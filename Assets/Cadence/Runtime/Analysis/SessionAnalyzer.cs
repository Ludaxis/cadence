using System.Collections.Generic;
using UnityEngine;

namespace Cadence
{
    public sealed class SessionAnalyzer : ISessionAnalyzer
    {
        public SessionSummary Analyze(SignalBatch batch)
        {
            var summary = new SessionSummary();
            var entries = batch.Entries;
            summary.TotalSignals = entries.Count;

            if (entries.Count == 0) return summary;

            // Accumulators
            int moveCount = 0;
            int optimalMoves = 0;
            float totalWaste = 0f;
            float totalProgress = 0f;
            int sequenceMatches = 0;
            int sequenceCheckCount = 0;
            var intervalStats = new RunningAverage();
            float lastMoveTime = -1f;
            float firstMoveTime = -1f;
            float lastSignalTime = 0f;
            int pauseCount = 0;
            int powerUpsUsed = 0;
            int attemptNumber = 0;
            float sessionGapDays = 0f;
            float outcomeValue = 0f;
            bool hasOutcome = false;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                lastSignalTime = e.Timestamp.SessionTime;

                switch (e.Key)
                {
                    case SignalKeys.MoveExecuted:
                        moveCount++;
                        float moveTime = e.Timestamp.SessionTime;
                        if (firstMoveTime < 0f) firstMoveTime = moveTime;
                        if (lastMoveTime >= 0f)
                            intervalStats.Add(moveTime - lastMoveTime);
                        lastMoveTime = moveTime;
                        break;

                    case SignalKeys.MoveOptimal:
                        if (e.Value > 0.5f) optimalMoves++;
                        break;

                    case SignalKeys.MoveWaste:
                        totalWaste += e.Value;
                        break;

                    case SignalKeys.ProgressDelta:
                        totalProgress += e.Value;
                        break;

                    case SignalKeys.InterMoveInterval:
                        // If the game sends explicit intervals, use those instead
                        break;

                    case SignalKeys.HesitationTime:
                        summary.HesitationTime = e.Value;
                        break;

                    case SignalKeys.PauseTriggered:
                        pauseCount++;
                        break;

                    case SignalKeys.PowerUpUsed:
                        powerUpsUsed++;
                        break;

                    case SignalKeys.SequenceMatch:
                        sequenceCheckCount++;
                        if (e.Value > 0.5f) sequenceMatches++;
                        break;

                    case SignalKeys.AttemptNumber:
                        attemptNumber = (int)e.Value;
                        break;

                    case SignalKeys.SessionGapDays:
                        sessionGapDays = e.Value;
                        break;

                    case SignalKeys.SessionOutcome:
                        outcomeValue = e.Value;
                        hasOutcome = true;
                        break;
                }
            }

            // Fill summary
            summary.TotalMoves = moveCount;
            summary.Duration = lastSignalTime;

            // Outcome
            if (hasOutcome)
            {
                if (outcomeValue > 0.5f) summary.Outcome = SessionOutcome.Win;
                else if (outcomeValue < -0.5f) summary.Outcome = SessionOutcome.Abandoned;
                else summary.Outcome = SessionOutcome.Lose;
            }

            // Tier 0
            summary.MoveEfficiency = moveCount > 0 ? (float)optimalMoves / moveCount : 0f;
            float totalResources = moveCount > 0 ? moveCount : 1f;
            summary.WasteRatio = totalWaste / totalResources;
            summary.ProgressRate = moveCount > 0 ? totalProgress / moveCount : 0f;

            // Tier 1
            summary.MeanInterMoveInterval = intervalStats.Mean;
            summary.InterMoveVariance = intervalStats.Variance;
            if (summary.HesitationTime <= 0f && firstMoveTime > 0f)
                summary.HesitationTime = firstMoveTime;
            summary.PauseCount = pauseCount;

            // Tier 2
            summary.PowerUpsUsed = powerUpsUsed;
            summary.SequenceMatchRate = sequenceCheckCount > 0
                ? (float)sequenceMatches / sequenceCheckCount
                : 0f;

            // Tier 3
            summary.AttemptNumber = attemptNumber;
            summary.SessionGapDays = sessionGapDays;

            // Derived scores
            summary.SkillScore = ComputeSkillScore(summary);
            summary.EngagementScore = ComputeEngagementScore(summary);
            summary.FrustrationScore = ComputeFrustrationScore(summary);

            return summary;
        }

        private static float ComputeSkillScore(SessionSummary s)
        {
            // Weighted blend of efficiency and sequence matching
            float efficiency = s.MoveEfficiency;
            float sequence = s.SequenceMatchRate;
            return Mathf.Clamp01(efficiency * 0.7f + sequence * 0.3f);
        }

        private static float ComputeEngagementScore(SessionSummary s)
        {
            // Low inter-move variance + few pauses = high engagement
            float tempoConsistency = s.InterMoveVariance > 0f
                ? Mathf.Clamp01(1f - Mathf.Sqrt(s.InterMoveVariance) / 5f)
                : 0.5f;
            float pausePenalty = Mathf.Clamp01(1f - s.PauseCount * 0.15f);
            return Mathf.Clamp01(tempoConsistency * 0.6f + pausePenalty * 0.4f);
        }

        private static float ComputeFrustrationScore(SessionSummary s)
        {
            // High waste + high variance + many pauses = frustration
            float wasteSignal = Mathf.Clamp01(s.WasteRatio);
            float varianceSignal = s.InterMoveVariance > 0f
                ? Mathf.Clamp01(Mathf.Sqrt(s.InterMoveVariance) / 5f)
                : 0f;
            float pauseSignal = Mathf.Clamp01(s.PauseCount * 0.15f);
            float efficiencyInverse = 1f - s.MoveEfficiency;
            return Mathf.Clamp01(
                wasteSignal * 0.3f +
                varianceSignal * 0.25f +
                pauseSignal * 0.2f +
                efficiencyInverse * 0.25f
            );
        }
    }
}
