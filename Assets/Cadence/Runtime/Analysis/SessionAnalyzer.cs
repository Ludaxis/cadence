using System.Collections.Generic;
using UnityEngine;

namespace Cadence
{
    public sealed class SessionAnalyzer : ISessionAnalyzer
    {
        // ───────────────────── Named Constants ─────────────────────

        // Engagement score
        private const float TempoNormalizationDivisor = 5f;
        private const float PausePenaltyFactor = 0.15f;
        private const float EngagementTempoWeight = 0.6f;
        private const float EngagementPauseWeight = 0.4f;

        // Skill score
        private const float SkillEfficiencyWeight = 0.7f;
        private const float SkillSequenceWeight = 0.3f;

        // Frustration score
        private const float FrustrationWasteWeight = 0.30f;
        private const float FrustrationVarianceWeight = 0.25f;
        private const float FrustrationPauseWeight = 0.20f;
        private const float FrustrationEfficiencyWeight = 0.25f;
        private const float InputAccuracySkillEfficiencyWeight = 0.75f;
        private const float InputAccuracySkillWeight = 0.25f;
        private const float FrustrationWasteWithInputWeight = 0.27f;
        private const float FrustrationVarianceWithInputWeight = 0.23f;
        private const float FrustrationPauseWithInputWeight = 0.18f;
        private const float FrustrationEfficiencyWithInputWeight = 0.22f;
        private const float FrustrationInputAccuracyWeight = 0.10f;

        public SessionSummary Analyze(SignalBatch batch)
        {
            if (batch?.Entries == null || batch.Entries.Count == 0)
                return new SessionSummary();

            var summary = new SessionSummary();
            var entries = batch.Entries;
            summary.TotalSignals = entries.Count;

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
            bool useExplicitIntervals = false;
            int pauseCount = 0;
            int powerUpsUsed = 0;
            int attemptNumber = 0;
            float sessionGapDays = 0f;
            float outcomeValue = 0f;
            bool hasOutcome = false;
            bool explicitAbandoned = false;
            float inputAccuracySum = 0f;
            int inputAccuracyCount = 0;
            float resourceEfficiencySum = 0f;
            int resourceEfficiencyCount = 0;

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
                        if (!useExplicitIntervals && lastMoveTime >= 0f)
                            intervalStats.Add(moveTime - lastMoveTime);
                        if (!useExplicitIntervals)
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
                        if (!useExplicitIntervals)
                        {
                            useExplicitIntervals = true;
                            intervalStats.Reset();
                            lastMoveTime = -1f;
                        }

                        if (e.Value > 0f)
                            intervalStats.Add(e.Value);
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

                    case SignalKeys.LevelAbandoned:
                        explicitAbandoned = true;
                        break;

                    case SignalKeys.InputAccuracy:
                        inputAccuracySum += Mathf.Clamp01(e.Value);
                        inputAccuracyCount++;
                        break;

                    case SignalKeys.ResourceEfficiency:
                        resourceEfficiencySum += Mathf.Clamp01(e.Value);
                        resourceEfficiencyCount++;
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
            if (explicitAbandoned)
            {
                summary.Outcome = SessionOutcome.Abandoned;
            }
            else if (hasOutcome)
            {
                if (outcomeValue > 0.5f) summary.Outcome = SessionOutcome.Win;
                else if (outcomeValue < -0.5f) summary.Outcome = SessionOutcome.Abandoned;
                else summary.Outcome = SessionOutcome.Lose;
            }

            // Tier 0
            summary.MoveEfficiency = moveCount > 0
                ? Mathf.Clamp01((float)optimalMoves / moveCount)
                : 0f;
            summary.HasInputAccuracy = inputAccuracyCount > 0;
            summary.InputAccuracy01 = summary.HasInputAccuracy
                ? inputAccuracySum / inputAccuracyCount
                : 0f;
            summary.HasResourceEfficiency = resourceEfficiencyCount > 0;
            summary.ResourceEfficiency01 = summary.HasResourceEfficiency
                ? resourceEfficiencySum / resourceEfficiencyCount
                : 0f;
            summary.EffectiveEfficiency01 = summary.HasResourceEfficiency
                ? Mathf.Clamp01((summary.MoveEfficiency + summary.ResourceEfficiency01) * 0.5f)
                : summary.MoveEfficiency;
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
            float efficiency = s.EffectiveEfficiency01;
            if (s.HasInputAccuracy)
            {
                efficiency = Mathf.Clamp01(
                    efficiency * InputAccuracySkillEfficiencyWeight +
                    s.InputAccuracy01 * InputAccuracySkillWeight
                );
            }

            float sequence = s.SequenceMatchRate;
            return Mathf.Clamp01(efficiency * SkillEfficiencyWeight + sequence * SkillSequenceWeight);
        }

        private static float ComputeEngagementScore(SessionSummary s)
        {
            // Low inter-move variance + few pauses = high engagement
            float tempoConsistency = s.InterMoveVariance > 0f
                ? Mathf.Clamp01(1f - Mathf.Sqrt(s.InterMoveVariance) / TempoNormalizationDivisor)
                : 0.5f;
            float pausePenalty = Mathf.Clamp01(1f - s.PauseCount * PausePenaltyFactor);
            return Mathf.Clamp01(tempoConsistency * EngagementTempoWeight + pausePenalty * EngagementPauseWeight);
        }

        private static float ComputeFrustrationScore(SessionSummary s)
        {
            // High waste + high variance + many pauses = frustration
            float wasteSignal = Mathf.Clamp01(s.WasteRatio);
            float varianceSignal = s.InterMoveVariance > 0f
                ? Mathf.Clamp01(Mathf.Sqrt(s.InterMoveVariance) / TempoNormalizationDivisor)
                : 0f;
            float pauseSignal = Mathf.Clamp01(s.PauseCount * PausePenaltyFactor);
            float efficiencyInverse = 1f - s.MoveEfficiency;

            if (s.HasInputAccuracy)
            {
                float inputAccuracyInverse = 1f - s.InputAccuracy01;
                return Mathf.Clamp01(
                    wasteSignal * FrustrationWasteWithInputWeight +
                    varianceSignal * FrustrationVarianceWithInputWeight +
                    pauseSignal * FrustrationPauseWithInputWeight +
                    efficiencyInverse * FrustrationEfficiencyWithInputWeight +
                    inputAccuracyInverse * FrustrationInputAccuracyWeight
                );
            }

            return Mathf.Clamp01(
                wasteSignal * FrustrationWasteWeight +
                varianceSignal * FrustrationVarianceWeight +
                pauseSignal * FrustrationPauseWeight +
                efficiencyInverse * FrustrationEfficiencyWeight
            );
        }
    }
}
