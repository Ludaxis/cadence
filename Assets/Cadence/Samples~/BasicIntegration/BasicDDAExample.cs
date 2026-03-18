using System.Collections.Generic;
using UnityEngine;

namespace Cadence.Samples
{
    /// <summary>
    /// Minimal example showing how to wire Cadence into a game loop.
    /// Attach to any GameObject in your scene.
    /// </summary>
    public class BasicDDAExample : MonoBehaviour
    {
        [SerializeField] private DDAConfig _config;

        private IDDAService _dda;

        private void Start()
        {
            _dda = new DDAService(_config);
        }

        /// <summary>Call when a level begins. Include par_moves in levelParams for skill index tracking.</summary>
        public void OnLevelStart(string levelId, Dictionary<string, float> levelParams,
            LevelType levelType = LevelType.Standard)
        {
            // Example: levelParams = { "difficulty": 100, "move_limit": 30, "par_moves": 20 }
            // par_moves is a recognized key that enables SkillIndex computation.
            _dda.BeginSession(levelId, levelParams, levelType);
        }

        /// <summary>Call every frame during gameplay.</summary>
        private void Update()
        {
            if (_dda == null || !_dda.IsSessionActive) return;

            _dda.Tick(Time.deltaTime);

            // Read real-time flow state
            var flow = _dda.CurrentFlow;
            Debug.Log($"Flow: {flow.State} (confidence: {flow.Confidence:F2})");
        }

        /// <summary>Record signals as the player acts.</summary>
        public void OnPlayerMove(bool wasOptimal, float hesitationTime)
        {
            _dda.RecordSignal(SignalKeys.MoveExecuted);
            _dda.RecordSignal(SignalKeys.MoveOptimal, wasOptimal ? 1f : 0f,
                SignalTier.DecisionQuality);

            if (!wasOptimal)
                _dda.RecordSignal(SignalKeys.MoveWaste, 1f, SignalTier.DecisionQuality);

            _dda.RecordSignal(SignalKeys.HesitationTime, hesitationTime, SignalTier.BehavioralTempo);
        }

        /// <summary>Call when the level ends.</summary>
        public void OnLevelEnd(bool won, Dictionary<string, float> nextLevelParams,
            LevelType nextLevelType = LevelType.Standard, int nextLevelIndex = -1)
        {
            var outcome = won ? SessionOutcome.Win : SessionOutcome.Lose;
            _dda.EndSession(outcome);

            // Get difficulty adjustment proposal for the next level
            var proposal = _dda.GetProposal(nextLevelParams, nextLevelType, nextLevelIndex);
            Debug.Log($"Proposal: {proposal.Reason} (confidence: {proposal.Confidence:F2})");

            foreach (var delta in proposal.Deltas)
            {
                Debug.Log($"  {delta.ParameterKey}: {delta.CurrentValue:F2} -> {delta.ProposedValue:F2} ({delta.RuleName})");
            }

            // Log skill index if par_moves was provided
            var debug = ((DDAService)_dda).GetDebugSnapshot();
            if (debug.LastSessionSummary.HasSkillIndex)
                Debug.Log($"Skill Index: {debug.LastSessionSummary.SkillIndex:F2} (par={debug.LastSessionSummary.ParMoves})");
        }
    }
}
