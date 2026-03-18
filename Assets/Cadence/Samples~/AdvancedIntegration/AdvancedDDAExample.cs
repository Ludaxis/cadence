using System.Collections.Generic;
using UnityEngine;

namespace Cadence.Samples
{
    /// <summary>
    /// Advanced example showing level types, sawtooth scheduling,
    /// and archetype-aware difficulty adjustment.
    /// </summary>
    public class AdvancedDDAExample : MonoBehaviour
    {
        [SerializeField] private DDAConfig _config;

        private IDDAService _dda;
        private int _currentLevelIndex;

        private void Start()
        {
            _dda = new DDAService(_config);
        }

        /// <summary>
        /// Call when starting a new level. The sawtooth scheduler will
        /// suggest appropriate difficulty scaling and level type.
        /// </summary>
        public void OnLevelStart(string levelId, Dictionary<string, float> baseParams)
        {
            // Use the sawtooth scheduler to get the target multiplier
            float multiplier = _dda.GetTargetMultiplier(_currentLevelIndex);
            Debug.Log($"Level {_currentLevelIndex}: sawtooth multiplier = {multiplier:F3}");

            // Determine level type — could come from your level data
            // or from the scheduler's suggestion
            LevelType levelType = DetermineLevelType(_currentLevelIndex);

            _dda.BeginSession(levelId, baseParams, levelType);
        }

        private void Update()
        {
            if (_dda == null || !_dda.IsSessionActive) return;
            _dda.Tick(Time.deltaTime);
        }

        public void OnPlayerMove(bool wasOptimal, float hesitationTime)
        {
            _dda.RecordSignal(SignalKeys.MoveExecuted);
            _dda.RecordSignal(SignalKeys.MoveOptimal, wasOptimal ? 1f : 0f,
                SignalTier.DecisionQuality);

            if (!wasOptimal)
                _dda.RecordSignal(SignalKeys.MoveWaste, 1f, SignalTier.DecisionQuality);
            _dda.RecordSignal(SignalKeys.HesitationTime, hesitationTime,
                SignalTier.BehavioralTempo);
        }

        public void OnPowerUpUsed()
        {
            _dda.RecordSignal(SignalKeys.PowerUpUsed, 1f, SignalTier.StrategicPattern);
        }

        /// <summary>Call when the player undoes an action.</summary>
        public void OnUndo()
        {
            _dda.RecordSignal(SignalKeys.Undo, 1f, SignalTier.StrategicPattern);
        }

        /// <summary>Call when a frustration trigger is shown (e.g. revive prompt).</summary>
        public void OnFrustrationTriggerShown()
        {
            _dda.RecordSignal(SignalKeys.FrustrationTrigger, 1f, SignalTier.RetryMeta);
        }

        /// <summary>
        /// Call when beginning a replay of a previously completed level.
        /// Replay sessions skip Glicko-2 updates so the player's rating isn't distorted.
        /// </summary>
        public void OnReplayLevelStart(string levelId, Dictionary<string, float> baseParams)
        {
            _dda.BeginSession(levelId, baseParams, LevelType.Standard);
            _dda.RecordSignal(SignalKeys.PlayType, SignalKeys.PlayTypeReplay, SignalTier.RetryMeta);
        }

        /// <summary>
        /// Call when the level ends. Gets a proposal that accounts for
        /// level type, player archetype, and sawtooth position.
        /// </summary>
        public void OnLevelEnd(bool won, Dictionary<string, float> nextLevelParams)
        {
            var outcome = won ? SessionOutcome.Win : SessionOutcome.Lose;
            _dda.EndSession(outcome);

            _currentLevelIndex++;

            // Get archetype-aware proposal
            var archetype = _dda.CurrentArchetype;
            Debug.Log($"Player archetype: {archetype.Primary} ({archetype.PrimaryConfidence:P0})");

            // Determine next level type
            LevelType nextType = DetermineLevelType(_currentLevelIndex);

            // Get proposal with full context
            var proposal = _dda.GetProposal(nextLevelParams, nextType, _currentLevelIndex);
            if (proposal != null && proposal.Deltas.Count > 0)
            {
                Debug.Log($"Proposal ({proposal.Reason}):");
                foreach (var delta in proposal.Deltas)
                {
                    Debug.Log($"  {delta.ParameterKey}: {delta.CurrentValue:F2} -> " +
                              $"{delta.ProposedValue:F2} [{delta.RuleName}]");

                    // Apply the proposed values to your next level parameters
                    nextLevelParams[delta.ParameterKey] = delta.ProposedValue;
                }
            }
        }

        /// <summary>
        /// Example level type assignment — in production, this would come
        /// from your level database or the sawtooth scheduler.
        /// </summary>
        private LevelType DetermineLevelType(int levelIndex)
        {
            // First level is always tutorial
            if (levelIndex == 0) return LevelType.Tutorial;

            // Use the sawtooth scheduler's suggestion if available
            // (requires SawtoothCurveConfig assigned in DDAConfig)
            float multiplier = _dda.GetTargetMultiplier(levelIndex);
            if (multiplier > 1.2f) return LevelType.Boss;
            if (multiplier < 0.9f) return LevelType.Breather;

            // Default based on pattern
            if (levelIndex % 10 == 0) return LevelType.MoveLimited;
            if (levelIndex % 7 == 0) return LevelType.TimeLimited;
            return LevelType.Standard;
        }
    }
}
