using System;

namespace Cadence
{
    [Serializable]
    public struct PlayerArchetypeReading
    {
        public PlayerArchetype Primary;
        public float PrimaryConfidence;
        public PlayerArchetype Secondary;
        public float SecondaryConfidence;

        // Raw scores per archetype (0-1)
        public float SpeedRunnerScore;
        public float CarefulThinkerScore;
        public float StrugglingLearnerScore;
        public float BoosterDependentScore;
        public float ChurnRiskScore;
    }
}
