using System;
using UnityEngine;

namespace Cadence
{
    public sealed class GlickoPlayerModel : IPlayerModel
    {
        private readonly PlayerModelConfig _config;
        private readonly PlayerSkillProfile _profile;

        // Glicko-2 scaling constant: converts between Glicko-1 and Glicko-2 scale
        private const double GlickoScale = 173.7178;
        private const double Pi2 = Math.PI * Math.PI;

        public PlayerSkillProfile Profile => _profile;

        public GlickoPlayerModel(PlayerModelConfig config)
        {
            _config = config;
            _profile = new PlayerSkillProfile
            {
                Rating = config.InitialRating,
                Deviation = config.InitialDeviation,
                Volatility = config.InitialVolatility
            };
        }

        public void UpdateFromSession(SessionSummary summary)
        {
            float actualScore = summary.Outcome == SessionOutcome.Win ? 1f : 0f;

            // Use skill score as a proxy for level difficulty
            // Higher skill = higher effective opponent rating
            float effectiveLevelRating = _profile.Rating +
                (summary.MoveEfficiency > 0.5f ? -50f : 50f);

            UpdateGlicko2(actualScore, effectiveLevelRating, 60f);

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
            double diff = (_profile.Rating - levelDifficulty) / 400.0;
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
            JsonUtility.FromJsonOverwrite(json, _profile);
        }

        /// <summary>
        /// Core Glicko-2 update for a single game result.
        /// </summary>
        private void UpdateGlicko2(float actualScore, float opponentRating, float opponentDeviation)
        {
            double tau = _config != null ? _config.Tau : 0.5;
            double eps = _config != null ? _config.ConvergenceEpsilon : 0.000001;

            // Step 1: Convert to Glicko-2 scale
            double mu = (_profile.Rating - 1500.0) / GlickoScale;
            double phi = _profile.Deviation / GlickoScale;
            double sigma = _profile.Volatility;

            double muJ = (opponentRating - 1500.0) / GlickoScale;
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
                while (VolatilityF(B, delta2, phi2, v, a, tau2) < 0)
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
            _profile.Rating = (float)(newMu * GlickoScale + 1500.0);
            _profile.Deviation = (float)(newPhi * GlickoScale);
            _profile.Volatility = (float)newSigma;

            // Clamp deviation
            float maxDev = _config != null ? _config.MaxDeviation : PlayerSkillProfile.DefaultDeviation;
            _profile.Deviation = Mathf.Clamp(_profile.Deviation, 30f, maxDev);
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
