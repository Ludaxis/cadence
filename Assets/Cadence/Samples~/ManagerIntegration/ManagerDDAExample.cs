using System.Collections.Generic;
using UnityEngine;

namespace Cadence.Samples
{
    /// <summary>
    /// Simplified integration using CadenceManager.
    /// No manual DDAService construction, no Tick() calls, no profile save/load.
    ///
    /// Setup:
    /// 1. Add CadenceManager to your scene (Cadence > Create Manager in Scene)
    /// 2. Assign a DDAConfig in the Inspector
    /// 3. Call BeginLevel / RecordMove / EndLevel from your game code
    /// </summary>
    public class ManagerDDAExample : MonoBehaviour
    {
        private IDDAService _dda;
        private int _currentLevelIndex;

        private void Start()
        {
            // CadenceManager initializes the service automatically.
            // If it's already initialized, grab it now; otherwise wait for the event.
            if (CadenceManager.Service != null)
            {
                _dda = CadenceManager.Service;
            }
            else
            {
                CadenceManager.OnServiceInitialized += OnServiceReady;
            }
        }

        private void OnDestroy()
        {
            CadenceManager.OnServiceInitialized -= OnServiceReady;
        }

        private void OnServiceReady(IDDAService service)
        {
            _dda = service;
            CadenceManager.OnServiceInitialized -= OnServiceReady;
        }

        /// <summary>Call when a level begins.</summary>
        public void BeginLevel(string levelId, Dictionary<string, float> levelParams,
            LevelType levelType = LevelType.Standard)
        {
            if (_dda == null) return;
            _dda.BeginSession(levelId, levelParams, levelType);
        }

        /// <summary>Record signals as the player acts.</summary>
        public void RecordMove(bool wasOptimal, float hesitationTime)
        {
            if (_dda == null) return;

            _dda.RecordSignal(SignalKeys.MoveExecuted);
            _dda.RecordSignal(SignalKeys.MoveOptimal, wasOptimal ? 1f : 0f,
                SignalTier.DecisionQuality);

            if (!wasOptimal)
                _dda.RecordSignal(SignalKeys.MoveWaste, 1f, SignalTier.DecisionQuality);

            _dda.RecordSignal(SignalKeys.HesitationTime, hesitationTime,
                SignalTier.BehavioralTempo);
        }

        /// <summary>Call when the level ends.</summary>
        public void EndLevel(bool won, Dictionary<string, float> nextLevelParams)
        {
            if (_dda == null) return;

            var outcome = won ? SessionOutcome.Win : SessionOutcome.Lose;
            _dda.EndSession(outcome);

            _currentLevelIndex++;

            // Get a proposal with full context (level type + sawtooth index)
            var proposal = _dda.GetProposal(nextLevelParams, LevelType.Standard,
                _currentLevelIndex);

            if (proposal != null && proposal.Deltas != null && proposal.Deltas.Count > 0)
            {
                Debug.Log($"[Cadence] Proposal: {proposal.Reason}");
                foreach (var delta in proposal.Deltas)
                {
                    Debug.Log($"  {delta.ParameterKey}: " +
                              $"{delta.CurrentValue:F2} -> {delta.ProposedValue:F2} " +
                              $"[{delta.RuleName}]");

                    // Apply proposed values to your next level parameters
                    nextLevelParams[delta.ParameterKey] = delta.ProposedValue;
                }
            }
        }
    }
}
