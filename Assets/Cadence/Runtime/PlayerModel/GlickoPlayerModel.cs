using System;
using UnityEngine;

namespace Cadence
{
    /// <summary>
    /// Glicko-2 implementation of <see cref="IPlayerModel"/>.
    /// Maintains a player skill rating with uncertainty (deviation) and volatility,
    /// updated after each session using the Glicko-2 algorithm.
    /// </summary>
    public sealed class GlickoPlayerModel : IPlayerModel
    {
        private readonly PlayerModelConfig _config;
        private readonly PlayerSkillProfile _profile;

        // ───────────────────── Glicko-2 Algorithm Constants ─────────────────────

        /// <summary>Scaling factor between Glicko-1 (0-3000) and Glicko-2 (internal) rating scales.</summary>
        private const double GlickoScale = 173.7178;
        private const double Pi2 = Math.PI * Math.PI;

        /// <summary>Baseline rating on the Glicko-1 scale (average player).</summary>
        private const double GlickoBaselineRating = 1500.0;

        /// <summary>Logistic function divisor for win probability prediction.</summary>
        private const double WinPredictionDivisor = 400.0;

        // Effective opponent estimation
        private const float EfficiencyThreshold = 0.5f;
        private const float EasyLevelRatingOffset = -50f;
        private const float HardLevelRatingOffset = 50f;
        private const float DefaultOpponentDeviation = 60f;

        // Deviation clamping
        private const float MinDeviationClamp = 30f;

        public PlayerSkillProfile Profile => _profile;

        /// <summary>
        /// Initializes a new Glicko-2 player model.
        /// </summary>
        /// <param name="config">Configuration for initial values and system constants. If null, uses defaults.</param>
        public GlickoPlayerModel(PlayerModelConfig config)
        {
            _config = config;
            _profile = new PlayerSkillProfile
            {
                Rating = config != null ? config.InitialRating : PlayerSkillProfile.DefaultRating,
                Deviation = config != null ? config.InitialDeviation : PlayerSkillProfile.DefaultDeviation,
                Volatility = config != null ? config.InitialVolatility : PlayerSkillProfile.DefaultVolatility
            };
        }

        public void UpdateFromSession(SessionSummary summary)
        {
            float actualScore = summary.Outcome == SessionOutcome.Win ? 1f : 0f;

            // Use skill score as a proxy for level difficulty
            // Higher skill = higher effective opponent rating
            float effectiveLevelRating = _profile.Rating +
                (summary.MoveEfficiency > EfficiencyThreshold ? EasyLevelRatingOffset : HardLevelRatingOffset);

            UpdateGlicko2(actualScore, effectiveLevelRating, DefaultOpponentDeviation);

            // Update profile stats
            _profile.SessionsCompleted++;
            _profile.TotalMoves += summary.TotalMoves;

            // Running average of efficiency
            float n = _profile.SessionsCompleted;
            _profile.AverageEfficiency =
                _profile.AverageEfficiency * ((n - 1f) / n) + summary.MoveEfficiency / n;
            _profile.AverageOutcome =
                _profile.AverageOutcome * ((n - 1f) / n) + actualScore / n;

            _profile.LastSessionUtcTicks = DateTime.UtcNow.Ticks;

            // Record in history
            _profile.RecentHistory.Add(new SessionHistoryEntry
            {
                Efficiency = summary.MoveEfficiency,
                Outcome = actualScore,
                Duration = summary.Duration,
                Moves = summary.TotalMoves,
                TimestampUtcTicks = DateTime.UtcNow.Ticks,
                LevelTypeByte = (byte)summary.LevelType
            });

            while (_profile.RecentHistory.Count > (_config != null
                ? _config.MaxHistoryEntries : PlayerSkillProfile.MaxHistoryEntries))
            {
                _profile.RecentHistory.RemoveAt(0);
            }
        }

        public float PredictWinRate(float levelDifficulty)
        {
            // Logistic function based on rating difference
            double diff = (_profile.Rating - levelDifficulty) / WinPredictionDivisor;
            return (float)(1.0 / (1.0 + Math.Pow(10.0, -diff)));
        }

        public void ApplyTimeDecay(float daysSinceLastSession)
        {
            if (daysSinceLastSession <= 0f) return;
            float decayPerDay = _config != null ? _config.DeviationDecayPerDay : 5f;
            float maxDev = _config != null ? _config.MaxDeviation : PlayerSkillProfile.DefaultDeviation;
            _profile.Deviation = Mathf.Min(
                _profile.Deviation + decayPerDay * daysSinceLastSession,
                maxDev
            );
        }

        public string Serialize()
        {
            return JsonUtility.ToJson(_profile);
        }

        public void Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                JsonUtility.FromJsonOverwrite(json, _profile);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Cadence] Failed to deserialize player profile, resetting to defaults: {ex.Message}");
                _profile.Rating = _config != null ? _config.InitialRating : PlayerSkillProfile.DefaultRating;
                _profile.Deviation = _config != null ? _config.InitialDeviation : PlayerSkillProfile.DefaultDeviation;
                _profile.Volatility = _config != null ? _config.InitialVolatility : PlayerSkillProfile.DefaultVolatility;
                _profile.SessionsCompleted = 0;
                _profile.TotalMoves = 0;
                _profile.AverageEfficiency = 0f;
                _profile.AverageOutcome = 0f;
                _profile.LastSessionUtcTicks = 0;
                _profile.RecentHistory.Clear();
            }
        }

        /// <summary>
        /// Core Glicko-2 update for a single game result.
        /// </summary>
        private void UpdateGlicko2(float actualScore, float opponentRating, float opponentDeviation)
        {
            double tau = _config != null ? _config.Tau : 0.5;
            double eps = _config != null ? _config.ConvergenceEpsilon : 0.000001;

            // Step 1: Convert to Glicko-2 scale
            double mu = (_profile.Rating - GlickoBaselineRating) / GlickoScale;
            double phi = _profile.Deviation / GlickoScale;
            double sigma = _profile.Volatility;

            double muJ = (opponentRating - GlickoBaselineRating) / GlickoScale;
            double phiJ = opponentDeviation / GlickoScale;

            // Step 2: Compute variance (v) and delta
            double gPhiJ = G(phiJ);
            double eVal = E(mu, muJ, gPhiJ);
            double v = 1.0 / (gPhiJ * gPhiJ * eVal * (1.0 - eVal));
            double delta = v * gPhiJ * (actualScore - eVal);

            // Step 3: Determine new volatility (sigma')
            double a = Math.Log(sigma * sigma);
            double delta2 = delta * delta;
            double phi2 = phi * phi;
            double tau2 = tau * tau;

            // Illinois algorithm for finding sigma'
            double A = a;
            double B;
            if (delta2 > phi2 + v)
            {
                B = Math.Log(delta2 - phi2 - v);
            }
            else
            {
                int k = 1;
                B = a - k * tau;
                while (VolatilityF(B, delta2, phi2, v, a, tau2) < 0 && k < 50)
                {
                    k++;
                    B = a - k * tau;
                }
            }

            double fA = VolatilityF(A, delta2, phi2, v, a, tau2);
            double fB = VolatilityF(B, delta2, phi2, v, a, tau2);

            int iterations = 0;
            while (Math.Abs(B - A) > eps && iterations < 100)
            {
                double C = A + (A - B) * fA / (fB - fA);
                double fC = VolatilityF(C, delta2, phi2, v, a, tau2);

                if (fC * fB <= 0)
                {
                    A = B;
                    fA = fB;
                }
                else
                {
                    fA *= 0.5;
                }

                B = C;
                fB = fC;
                iterations++;
            }

            double newSigma = Math.Exp(B / 2.0);

            // Step 4: Update rating and deviation
            double phiStar = Math.Sqrt(phi2 + newSigma * newSigma);
            double newPhi = 1.0 / Math.Sqrt(1.0 / (phiStar * phiStar) + 1.0 / v);
            double newMu = mu + newPhi * newPhi * gPhiJ * (actualScore - eVal);

            // Step 5: Convert back to Glicko-1 scale
            float prevRating = _profile.Rating;
            float prevDeviation = _profile.Deviation;
            float prevVolatility = _profile.Volatility;

            _profile.Rating = (float)(newMu * GlickoScale + GlickoBaselineRating);
            _profile.Deviation = (float)(newPhi * GlickoScale);
            _profile.Volatility = (float)newSigma;

            // NaN/Infinity guard
            if (float.IsNaN(_profile.Rating) || float.IsInfinity(_profile.Rating) ||
                float.IsNaN(_profile.Deviation) || float.IsInfinity(_profile.Deviation) ||
                float.IsNaN(_profile.Volatility) || float.IsInfinity(_profile.Volatility))
            {
                Debug.LogWarning("[Cadence] Glicko-2 produced invalid values, reverting to previous state.");
                _profile.Rating = prevRating;
                _profile.Deviation = prevDeviation;
                _profile.Volatility = prevVolatility;
                return;
            }

            // Clamp deviation
            float maxDev = _config != null ? _config.MaxDeviation : PlayerSkillProfile.DefaultDeviation;
            _profile.Deviation = Mathf.Clamp(_profile.Deviation, MinDeviationClamp, maxDev);
        }

        private static double G(double phi)
        {
            return 1.0 / Math.Sqrt(1.0 + 3.0 * phi * phi / Pi2);
        }

        private static double E(double mu, double muJ, double gPhiJ)
        {
            return 1.0 / (1.0 + Math.Exp(-gPhiJ * (mu - muJ)));
        }

        private static double VolatilityF(double x, double delta2, double phi2,
            double v, double a, double tau2)
        {
            double ex = Math.Exp(x);
            double d = phi2 + v + ex;
            return (ex * (delta2 - d)) / (2.0 * d * d) - (x - a) / tau2;
        }
    }
}
