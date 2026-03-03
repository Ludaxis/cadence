using System;

namespace Cadence
{
    [Serializable]
    public sealed class LevelTypeConfig
    {
        public LevelType Type;
        public string PrimaryParameterKey;
        public float AdjustmentScale;
        public bool AllowUpwardAdjustment;
        public bool DDAEnabled;

        public LevelTypeConfig()
        {
            Type = LevelType.Standard;
            PrimaryParameterKey = null;
            AdjustmentScale = 1f;
            AllowUpwardAdjustment = true;
            DDAEnabled = true;
        }

        public LevelTypeConfig(LevelType type, string primaryKey, float scale,
            bool allowUpward, bool enabled)
        {
            Type = type;
            PrimaryParameterKey = primaryKey;
            AdjustmentScale = scale;
            AllowUpwardAdjustment = allowUpward;
            DDAEnabled = enabled;
        }
    }
}
