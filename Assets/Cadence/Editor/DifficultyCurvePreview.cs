using UnityEditor;
using UnityEngine;
using Cadence;

namespace Cadence.Editor
{
    public class DifficultyCurvePreview : EditorWindow
    {
        private SawtoothCurveConfig _sawtoothConfig;
        private PlayerModelConfig _playerModelConfig;
        private Vector2 _scrollPos;

        // Preview parameters
        private int _levelCount = 50;
        private int _startLevel = 0;
        private float _playerRating = 1500f;
        private float _playerDeviation = 200f;

        // Cached preview data
        private DifficultyPoint[] _points;
        private float[] _multipliers;
        private float[] _passRates;
        private Color[] _pointColors;
        private bool _dirty = true;

        [MenuItem("Cadence/Difficulty Curve Preview")]
        public static void ShowWindow()
        {
            GetWindow<DifficultyCurvePreview>("Difficulty Curve Preview");
        }

        private void OnEnable()
        {
            _dirty = true;
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("Difficulty Curve Preview", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Config slots
            EditorGUI.BeginChangeCheck();
            _sawtoothConfig = (SawtoothCurveConfig)EditorGUILayout.ObjectField(
                "Sawtooth Config", _sawtoothConfig, typeof(SawtoothCurveConfig), false);
            _playerModelConfig = (PlayerModelConfig)EditorGUILayout.ObjectField(
                "Player Model Config", _playerModelConfig, typeof(PlayerModelConfig), false);

            EditorGUILayout.Space();

            // Player profile sliders
            _playerRating = EditorGUILayout.Slider("Player Rating", _playerRating, 800f, 2200f);
            _playerDeviation = EditorGUILayout.Slider("Rating Deviation", _playerDeviation, 30f, 350f);

            EditorGUILayout.Space();

            // Range sliders
            _startLevel = EditorGUILayout.IntSlider("Start Level", _startLevel, 0, 200);
            _levelCount = EditorGUILayout.IntSlider("Level Count", _levelCount, 10, 100);

            if (EditorGUI.EndChangeCheck())
                _dirty = true;

            EditorGUILayout.Space();

            if (_sawtoothConfig == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a SawtoothCurveConfig to preview the difficulty curve.",
                    MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (_dirty)
            {
                RegeneratePreview();
                _dirty = false;
            }

            // Graph area
            EditorGUILayout.LabelField("Difficulty Multiplier", EditorStyles.boldLabel);
            Rect graphRect = GUILayoutUtility.GetRect(position.width - 40f, 200f);
            DrawMultiplierGraph(graphRect);

            EditorGUILayout.Space();

            // Pass rate overlay
            if (_passRates != null)
            {
                EditorGUILayout.LabelField("Predicted Pass Rate", EditorStyles.boldLabel);
                Rect passRect = GUILayoutUtility.GetRect(position.width - 40f, 150f);
                DrawPassRateGraph(passRect);
            }

            EditorGUILayout.Space();

            // Stats panel
            DrawStatsPanel();

            // Level type legend
            DrawLegend();

            EditorGUILayout.EndScrollView();
        }

        private void RegeneratePreview()
        {
            var scheduler = new DifficultyScheduler(_sawtoothConfig);
            _points = scheduler.GetCurvePreview(_startLevel, _levelCount);

            _multipliers = new float[_levelCount];
            _pointColors = new Color[_levelCount];
            _passRates = new float[_levelCount];

            for (int i = 0; i < _levelCount; i++)
            {
                _multipliers[i] = _points[i].Multiplier;
                _pointColors[i] = GetLevelTypeColor(_points[i].SuggestedType);

                // Predict pass rate based on player rating vs multiplied difficulty
                float effectiveDifficulty = _playerRating * _points[i].Multiplier;
                float diff = (_playerRating - effectiveDifficulty) / 400f;
                _passRates[i] = 1f / (1f + Mathf.Pow(10f, -diff));
            }
        }

        private void DrawMultiplierGraph(Rect rect)
        {
            if (_multipliers == null) return;

            // Background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

            float minY = 0.5f;
            float maxY = 1.8f;

            // Grid
            EditorGraphUtility.DrawGrid(rect, 5, _levelCount / 10,
                new Color(0.3f, 0.3f, 0.3f));

            // Baseline 1.0 line
            EditorGraphUtility.DrawHorizontalLine(rect, 1f, minY, maxY,
                new Color(1f, 1f, 1f, 0.3f), 1f);

            // Curve with per-point colors
            EditorGraphUtility.DrawLineGraph(rect, _multipliers, minY, maxY,
                _pointColors, 3f);

            // Boss and breather markers
            for (int i = 0; i < _points.Length; i++)
            {
                if (_points[i].SuggestedType == LevelType.Boss)
                {
                    EditorGraphUtility.DrawPoint(rect, i, _points.Length,
                        _multipliers[i], minY, maxY, new Color(1f, 0.3f, 0.2f), 5f);
                }
                else if (_points[i].SuggestedType == LevelType.Breather)
                {
                    EditorGraphUtility.DrawPoint(rect, i, _points.Length,
                        _multipliers[i], minY, maxY, new Color(0.3f, 0.8f, 1f), 5f);
                }
            }

            // Y-axis labels
            var labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
            GUI.Label(new Rect(rect.x - 35, rect.y - 8, 30, 16), maxY.ToString("F1"), labelStyle);
            GUI.Label(new Rect(rect.x - 35, rect.yMax - 8, 30, 16), minY.ToString("F1"), labelStyle);
            float midY = (minY + maxY) * 0.5f;
            float midYPos = rect.yMax - Mathf.InverseLerp(minY, maxY, midY) * rect.height;
            GUI.Label(new Rect(rect.x - 35, midYPos - 8, 30, 16), midY.ToString("F1"), labelStyle);
        }

        private void DrawPassRateGraph(Rect rect)
        {
            if (_passRates == null) return;

            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
            EditorGraphUtility.DrawGrid(rect, 4, _levelCount / 10,
                new Color(0.3f, 0.3f, 0.3f));

            EditorGraphUtility.DrawLineGraph(rect, _passRates, 0f, 1f,
                new Color(0.4f, 0.9f, 0.4f), 2f);

            // Target band lines
            EditorGraphUtility.DrawHorizontalLine(rect, 0.3f, 0f, 1f,
                new Color(1f, 0.5f, 0.2f, 0.5f), 1f);
            EditorGraphUtility.DrawHorizontalLine(rect, 0.7f, 0f, 1f,
                new Color(1f, 0.5f, 0.2f, 0.5f), 1f);

            var labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
            GUI.Label(new Rect(rect.x - 35, rect.y - 8, 30, 16), "100%", labelStyle);
            GUI.Label(new Rect(rect.x - 35, rect.yMax - 8, 30, 16), "0%", labelStyle);
        }

        private void DrawStatsPanel()
        {
            if (_points == null) return;

            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            float minMult = float.MaxValue, maxMult = float.MinValue, sumMult = 0f;
            int bossCount = 0, breatherCount = 0;
            for (int i = 0; i < _points.Length; i++)
            {
                float m = _points[i].Multiplier;
                if (m < minMult) minMult = m;
                if (m > maxMult) maxMult = m;
                sumMult += m;
                if (_points[i].SuggestedType == LevelType.Boss) bossCount++;
                if (_points[i].SuggestedType == LevelType.Breather) breatherCount++;
            }

            EditorGUILayout.LabelField("Multiplier Range",
                $"{minMult:F3} - {maxMult:F3}");
            EditorGUILayout.LabelField("Average Multiplier",
                $"{sumMult / _points.Length:F3}");
            EditorGUILayout.LabelField("Boss Levels", bossCount.ToString());
            EditorGUILayout.LabelField("Breather Levels", breatherCount.ToString());
            EditorGUILayout.LabelField("Cycles",
                $"{_levelCount / Mathf.Max(1, _sawtoothConfig.Period):F1}");

            EditorGUI.indentLevel--;
        }

        private void DrawLegend()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Legend", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            DrawLegendItem("Standard", GetLevelTypeColor(LevelType.Standard));
            DrawLegendItem("Boss", GetLevelTypeColor(LevelType.Boss));
            DrawLegendItem("Breather", GetLevelTypeColor(LevelType.Breather));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawLegendItem(string label, Color color)
        {
            var rect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
            EditorGUI.DrawRect(rect, color);
            EditorGUILayout.LabelField(label, GUILayout.Width(70));
        }

        private static Color GetLevelTypeColor(LevelType type)
        {
            switch (type)
            {
                case LevelType.Boss:     return new Color(1f, 0.3f, 0.2f);
                case LevelType.Breather: return new Color(0.3f, 0.8f, 1f);
                case LevelType.Tutorial: return new Color(0.7f, 0.7f, 0.7f);
                default:                 return new Color(0.4f, 0.9f, 0.4f);
            }
        }
    }
}
