using UnityEngine;

namespace Cadence
{
    [CreateAssetMenu(menuName = "Cadence/Sawtooth Curve Config")]
    public class SawtoothCurveConfig : ScriptableObject
    {
        [Header("Cycle Shape")]
        [Tooltip("Number of levels per sawtooth cycle")]
        [Range(5, 50)] public int Period = 10;

        [Tooltip("Peak-to-trough multiplier range (0.3 = multiplier varies from 0.7 to 1.3)")]
        [Range(0.05f, 0.5f)] public float Amplitude = 0.3f;

        [Tooltip("How deep the relief dip goes after the boss spike")]
        [Range(0f, 0.3f)] public float ReliefDepth = 0.15f;

        [Tooltip("Ramp style for the ascending difficulty within each cycle")]
        public RampStyle RampStyle = RampStyle.SCurve;

        [Tooltip("Optional custom curve shape (overrides RampStyle if non-empty)")]
        public AnimationCurve CurveShape = new AnimationCurve();

        [Header("Boss / Breather Positions")]
        [Tooltip("Boss level offset from end of cycle (-1 = second-to-last)")]
        [Range(-5, 0)] public int BossLevelOffset = -1;

        [Tooltip("Breather level offset from start of next cycle (0 = first level)")]
        [Range(0, 3)] public int BreatherLevelOffset = 0;

        [Header("Long-Term Progression")]
        [Tooltip("Baseline difficulty increase per completed cycle")]
        [Range(0f, 0.1f)] public float BaselineDriftPerCycle = 0.02f;
    }

    public enum RampStyle : byte
    {
        Linear = 0,
        EaseIn = 1,
        EaseOut = 2,
        SCurve = 3
    }
}
