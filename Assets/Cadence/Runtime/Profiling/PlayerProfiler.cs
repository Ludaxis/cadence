using System.Collections.Generic;
using UnityEngine;

namespace Cadence
{
    public sealed class PlayerProfiler : IPlayerProfiler
    {
        public PlayerArchetypeReading Classify(PlayerSkillProfile profile, SessionSummary lastSession)
        {
            var reading = new PlayerArchetypeReading();

            if (profile == null || profile.RecentHistory == null || profile.RecentHistory.Count < 3)
            {
                reading.Primary = PlayerArchetype.Unknown;
                reading.PrimaryConfidence = 0f;
                return reading;
            }

            var history = profile.RecentHistory;

            reading.SpeedRunnerScore = ComputeSpeedRunnerScore(history, lastSession);
            reading.CarefulThinkerScore = ComputeCarefulThinkerScore(history, lastSession);
            reading.StrugglingLearnerScore = ComputeStrugglingLearnerScore(history, profile);
            reading.BoosterDependentScore = ComputeBoosterDependentScore(history, lastSession);
            reading.ChurnRiskScore = ComputeChurnRiskScore(history, profile);

            // Find primary and secondary
            float bestScore = 0f;
            float secondScore = 0f;
            PlayerArchetype bestType = PlayerArchetype.Unknown;
            PlayerArchetype secondType = PlayerArchetype.Unknown;

            CheckScore(PlayerArchetype.SpeedRunner, reading.SpeedRunnerScore,
                ref bestType, ref bestScore, ref secondType, ref secondScore);
            CheckScore(PlayerArchetype.CarefulThinker, reading.CarefulThinkerScore,
                ref bestType, ref bestScore, ref secondType, ref secondScore);
            CheckScore(PlayerArchetype.StrugglingLearner, reading.StrugglingLearnerScore,
                ref bestType, ref bestScore, ref secondType, ref secondScore);
            CheckScore(PlayerArchetype.BoosterDependent, reading.BoosterDependentScore,
                ref bestType, ref bestScore, ref secondType, ref secondScore);
            CheckScore(PlayerArchetype.ChurnRisk, reading.ChurnRiskScore,
                ref bestType, ref bestScore, ref secondType, ref secondScore);

            // Require minimum threshold for classification
            const float minThreshold = 0.3f;
            reading.Primary = bestScore >= minThreshold ? bestType : PlayerArchetype.Unknown;
            reading.PrimaryConfidence = bestScore;
            reading.Secondary = secondScore >= minThreshold ? secondType : PlayerArchetype.Unknown;
            reading.SecondaryConfidence = secondScore;

            return reading;
        }

        private static float ComputeSpeedRunnerScore(List<SessionHistoryEntry> history,
            SessionSummary lastSession)
        {
            // SpeedRunner: high efficiency, fast completion, high win rate
            float efficiencyTrend = ComputeTrend(history, e => e.Efficiency);
            float avgEfficiency = ComputeAverage(history, e => e.Efficiency);
            float avgDuration = ComputeAverage(history, e => e.Duration);
            float winRate = ComputeAverage(history, e => e.Outcome);

            // Short sessions + high efficiency + high win rate
            float speedScore = 0f;
            speedScore += Mathf.Clamp01(avgEfficiency) * 0.3f;
            speedScore += Mathf.Clamp01(winRate) * 0.25f;
            speedScore += Mathf.Clamp01(efficiencyTrend + 0.5f) * 0.15f;

            // Duration penalty: faster = higher score (assume typical session ~60-120s)
            if (avgDuration > 0f)
            {
                float durationScore = Mathf.Clamp01(1f - avgDuration / 180f);
                speedScore += durationScore * 0.3f;
            }

            return Mathf.Clamp01(speedScore);
        }

        private static float ComputeCarefulThinkerScore(List<SessionHistoryEntry> history,
            SessionSummary lastSession)
        {
            // CarefulThinker: moderate efficiency, long sessions, moderate win rate
            float avgEfficiency = ComputeAverage(history, e => e.Efficiency);
            float avgDuration = ComputeAverage(history, e => e.Duration);
            float winRate = ComputeAverage(history, e => e.Outcome);

            float score = 0f;
            // Moderate efficiency (0.4-0.7 is careful, not speed-running)
            float effPeak = 1f - Mathf.Abs(avgEfficiency - 0.55f) * 3f;
            score += Mathf.Clamp01(effPeak) * 0.3f;

            // Longer sessions indicate careful play
            if (avgDuration > 0f)
            {
                float durationScore = Mathf.Clamp01(avgDuration / 120f);
                score += durationScore * 0.3f;
            }

            // Moderate-high win rate (careful pays off)
            float wrPeak = 1f - Mathf.Abs(winRate - 0.6f) * 3f;
            score += Mathf.Clamp01(wrPeak) * 0.25f;

            // High moves per session (thinking each one through)
            float avgMoves = ComputeAverage(history, e => e.Moves);
            if (avgMoves > 0f)
                score += Mathf.Clamp01(avgMoves / 50f) * 0.15f;

            return Mathf.Clamp01(score);
        }

        private static float ComputeStrugglingLearnerScore(List<SessionHistoryEntry> history,
            PlayerSkillProfile profile)
        {
            // StrugglingLearner: low efficiency, low win rate, but improving over time
            float avgEfficiency = ComputeAverage(history, e => e.Efficiency);
            float winRate = ComputeAverage(history, e => e.Outcome);
            float efficiencyTrend = ComputeTrend(history, e => e.Efficiency);

            float score = 0f;
            // Low efficiency
            score += Mathf.Clamp01(1f - avgEfficiency) * 0.3f;
            // Low win rate
            score += Mathf.Clamp01(1f - winRate) * 0.3f;
            // But improving (positive efficiency trend)
            score += Mathf.Clamp01(efficiencyTrend + 0.5f) * 0.2f;
            // Fewer sessions completed (still learning)
            float sessionPenalty = Mathf.Clamp01(1f - profile.SessionsCompleted / 30f);
            score += sessionPenalty * 0.2f;

            return Mathf.Clamp01(score);
        }

        private static float ComputeBoosterDependentScore(List<SessionHistoryEntry> history,
            SessionSummary lastSession)
        {
            // BoosterDependent: wins correlate with power-up usage
            // We use the last session's PowerUpsUsed as signal since history doesn't track it
            float score = 0f;

            if (lastSession.PowerUpsUsed > 0)
            {
                float boosterRate = Mathf.Clamp01(lastSession.PowerUpsUsed / 5f);
                score += boosterRate * 0.5f;

                // Low efficiency without boosters suggests dependence
                float wasteSignal = Mathf.Clamp01(lastSession.WasteRatio);
                score += wasteSignal * 0.25f;
            }

            // If win rate is decent but efficiency is low, boosters are carrying
            float winRate = ComputeAverage(history, e => e.Outcome);
            float avgEff = ComputeAverage(history, e => e.Efficiency);
            if (winRate > 0.5f && avgEff < 0.4f)
                score += 0.25f;

            return Mathf.Clamp01(score);
        }

        private static float ComputeChurnRiskScore(List<SessionHistoryEntry> history,
            PlayerSkillProfile profile)
        {
            // ChurnRisk: declining session length, declining efficiency, low recent win rate
            float durationTrend = ComputeTrend(history, e => e.Duration);
            float efficiencyTrend = ComputeTrend(history, e => e.Efficiency);
            float recentWinRate = ComputeRecentAverage(history, 5, e => e.Outcome);

            float score = 0f;
            // Declining session duration (losing interest)
            score += Mathf.Clamp01(-durationTrend) * 0.3f;
            // Declining efficiency (giving up, not trying)
            score += Mathf.Clamp01(-efficiencyTrend) * 0.3f;
            // Low recent win rate
            score += Mathf.Clamp01(1f - recentWinRate) * 0.25f;
            // Session gap increasing (from LastSessionUtcTicks)
            if (profile.LastSessionUtcTicks > 0)
            {
                float daysSince = (float)(System.DateTime.UtcNow.Ticks -
                    profile.LastSessionUtcTicks) / System.TimeSpan.TicksPerDay;
                score += Mathf.Clamp01(daysSince / 7f) * 0.15f;
            }

            return Mathf.Clamp01(score);
        }

        // --- Utility Methods ---

        private static void CheckScore(PlayerArchetype type, float score,
            ref PlayerArchetype bestType, ref float bestScore,
            ref PlayerArchetype secondType, ref float secondScore)
        {
            if (score > bestScore)
            {
                secondType = bestType;
                secondScore = bestScore;
                bestType = type;
                bestScore = score;
            }
            else if (score > secondScore)
            {
                secondType = type;
                secondScore = score;
            }
        }

        private delegate float EntrySelector(SessionHistoryEntry entry);

        private static float ComputeAverage(List<SessionHistoryEntry> history, EntrySelector selector)
        {
            if (history.Count == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < history.Count; i++)
                sum += selector(history[i]);
            return sum / history.Count;
        }

        private static float ComputeRecentAverage(List<SessionHistoryEntry> history,
            int count, EntrySelector selector)
        {
            if (history.Count == 0) return 0f;
            int start = Mathf.Max(0, history.Count - count);
            float sum = 0f;
            int n = 0;
            for (int i = start; i < history.Count; i++)
            {
                sum += selector(history[i]);
                n++;
            }
            return n > 0 ? sum / n : 0f;
        }

        private static float ComputeTrend(List<SessionHistoryEntry> history, EntrySelector selector)
        {
            // Simple linear regression slope over the history
            if (history.Count < 3) return 0f;

            float n = history.Count;
            float sumX = 0f, sumY = 0f, sumXY = 0f, sumX2 = 0f;
            for (int i = 0; i < history.Count; i++)
            {
                float x = i;
                float y = selector(history[i]);
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }

            float denom = n * sumX2 - sumX * sumX;
            if (Mathf.Abs(denom) < 0.0001f) return 0f;

            float slope = (n * sumXY - sumX * sumY) / denom;
            // Normalize to roughly -1 to 1 range
            return Mathf.Clamp(slope * n, -1f, 1f);
        }
    }
}
