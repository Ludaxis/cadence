using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Cadence
{
    /// <summary>
    /// A single point on the difficulty curve, used for preview and visualization.
    /// Returned by <see cref="IDifficultyScheduler.GetCurvePreview"/>.
    /// </summary>
    [Serializable]
    public struct DifficultyPoint
    {
#if ODIN_INSPECTOR
        [PropertyTooltip("Level index in the global progression (0-based).")]
#endif
        public int LevelIndex;

#if ODIN_INSPECTOR
        [PropertyTooltip("Difficulty multiplier at this level.\n" +
                          "1.0 = baseline. >1.0 = harder. <1.0 = easier.\n" +
                          "Boss levels peak at baseline + amplitude.\n" +
                          "Breather levels dip to baseline - reliefDepth.")]
        [SuffixLabel("x", Overlay = true)]
#endif
        public float Multiplier;

#if ODIN_INSPECTOR
        [PropertyTooltip("Suggested level type based on curve position.\n" +
                          "Boss = at the peak, Breather = at the dip, Standard = ramp.")]
#endif
        public LevelType SuggestedType;
    }
}
