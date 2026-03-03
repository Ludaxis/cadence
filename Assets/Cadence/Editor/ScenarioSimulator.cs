using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Cadence;

namespace Cadence.Editor
{
    public class ScenarioSimulator : EditorWindow
    {
        // ════════════════════════════════════════════════════════════
        //  CONFIG STATE
        // ════════════════════════════════════════════════════════════

        private DDAConfig _ddaConfig;
        private int _levelCount = 100;
        private int _seed = 42;
        private List<ParamEntry> _paramEntries = new List<ParamEntry>
        {
            new ParamEntry { Key = "move_limit", Value = 30f }
        };
        private bool[] _personaEnabled;
        private PlayerPersona[] _personas;
        private bool _showBaseline = true;

        // ════════════════════════════════════════════════════════════
        //  SIMULATION RESULTS
        // ════════════════════════════════════════════════════════════

        private List<SimulationRun> _runs = new List<SimulationRun>();
        private List<SimulationRun> _baselineRuns = new List<SimulationRun>();
        private float[] _sawtoothMultipliers;
        private LevelType[] _sawtoothTypes;
        private bool _hasResults;

        // ════════════════════════════════════════════════════════════
        //  UI STATE
        // ════════════════════════════════════════════════════════════

        private Vector2 _leftScroll;
        private Vector2 _centerScroll;
        private Vector2 _bottomScroll;
        private int _sortColumn = -1;
        private bool _sortAscending = true;
        private int[] _sortedRunIndices;

        // Graph heights
        private const float SawtoothGraphHeight = 120f;
        private const float WinLoseGraphHeight = 80f;
        private const float WinRateGraphHeight = 120f;
        private const float RatingGraphHeight = 120f;
        private const float ParamGraphHeight = 120f;
        private const float HeatmapRowHeight = 16f;
        private const float HeatmapLabelWidth = 120f;

        private const float LeftPanelWidth = 250f;
        private const float BottomPanelHeight = 200f;

        [Serializable]
        private struct ParamEntry
        {
            public string Key;
            public float Value;
        }

        // ════════════════════════════════════════════════════════════
        //  MENU
        // ════════════════════════════════════════════════════════════

        [MenuItem("Cadence/Scenario Simulator", priority = 60)]
        public static void ShowWindow()
        {
            var window = GetWindow<ScenarioSimulator>("Scenario Simulator");
            window.minSize = new Vector2(960, 700);
        }

        private void OnEnable()
        {
            _personas = PlayerPersona.AllPresets();
            _personaEnabled = new bool[_personas.Length];
            for (int i = 0; i < _personaEnabled.Length; i++)
                _personaEnabled[i] = true;

            // Auto-discover DDAConfig
            if (_ddaConfig == null)
            {
                var guids = AssetDatabase.FindAssets("t:DDAConfig");
                if (guids.Length > 0)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _ddaConfig = AssetDatabase.LoadAssetAtPath<DDAConfig>(path);
                }
            }
        }

        // ════════════════════════════════════════════════════════════
        //  MAIN GUI
        // ════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            // Top region: left panel + center graphs
            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawCenterPanel();
            EditorGUILayout.EndHorizontal();

            // Bottom region: statistics table
            if (_hasResults)
                DrawBottomPanel();

            EditorGUILayout.EndVertical();
        }

        // ════════════════════════════════════════════════════════════
        //  LEFT PANEL — Configuration
        // ════════════════════════════════════════════════════════════

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LeftPanelWidth));
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

            // DDA Config
            EditorGUILayout.LabelField("DDA Config", EditorStyles.boldLabel);
            _ddaConfig = (DDAConfig)EditorGUILayout.ObjectField(
                _ddaConfig, typeof(DDAConfig), false);

            if (_ddaConfig == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a DDAConfig asset. Use Cadence > Setup Wizard to create one.",
                    MessageType.Warning);
            }

            // Level count
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);
            _levelCount = EditorGUILayout.IntSlider("Level Count", _levelCount, 10, 500);
            _seed = EditorGUILayout.IntField("Random Seed", _seed);

            // Parameters
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Base Parameters", EditorStyles.boldLabel);
            for (int i = 0; i < _paramEntries.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _paramEntries[i] = new ParamEntry
                {
                    Key = EditorGUILayout.TextField(_paramEntries[i].Key, GUILayout.Width(80)),
                    Value = EditorGUILayout.FloatField(_paramEntries[i].Value)
                };
                if (GUILayout.Button("\u00d7", GUILayout.Width(20)))
                {
                    _paramEntries.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Add Parameter"))
                _paramEntries.Add(new ParamEntry { Key = "param", Value = 1f });

            // Personas
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Personas", EditorStyles.boldLabel);
            for (int i = 0; i < _personas.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = _personas[i].Color;
                _personaEnabled[i] = EditorGUILayout.Toggle(_personaEnabled[i], GUILayout.Width(16));
                GUI.backgroundColor = prevColor;
                EditorGUILayout.LabelField(_personas[i].Name);
                EditorGUILayout.EndHorizontal();
            }

            // Baseline toggle
            EditorGUILayout.Space(8);
            _showBaseline = EditorGUILayout.Toggle("Show Baseline (No DDA)", _showBaseline);

            // Buttons
            EditorGUILayout.Space(12);
            GUI.enabled = _ddaConfig != null;
            var bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
            if (GUILayout.Button("Run Simulation", GUILayout.Height(30)))
                RunSimulation();
            GUI.backgroundColor = bg;
            GUI.enabled = true;

            GUI.enabled = _hasResults;
            if (GUILayout.Button("Export CSV", GUILayout.Height(24)))
                ExportCSV();
            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ════════════════════════════════════════════════════════════
        //  CENTER PANEL — Graphs
        // ════════════════════════════════════════════════════════════

        private void DrawCenterPanel()
        {
            EditorGUILayout.BeginVertical();
            _centerScroll = EditorGUILayout.BeginScrollView(_centerScroll);

            if (!_hasResults)
            {
                EditorGUILayout.HelpBox(
                    "Configure settings in the left panel and click 'Run Simulation' to see results.\n\n" +
                    "The simulator runs each persona through the full DDA loop:\n" +
                    "  BeginSession \u2192 signals \u2192 EndSession \u2192 GetProposal \u2192 apply\n\n" +
                    "Graphs show:\n" +
                    "  1. Sawtooth multiplier curve\n" +
                    "  2. Win/Lose outcomes per persona\n" +
                    "  3. Rolling win rate convergence\n" +
                    "  4. Glicko-2 rating evolution\n" +
                    "  5. Adjusted parameter values\n" +
                    "  6. Flow state heatmap",
                    MessageType.Info);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            float graphWidth = Mathf.Max(400f, position.width - LeftPanelWidth - 30f);

            DrawSawtoothGraph(graphWidth);
            EditorGUILayout.Space(4);
            DrawWinLoseGraph(graphWidth);
            EditorGUILayout.Space(4);
            DrawRollingWinRateGraph(graphWidth);
            EditorGUILayout.Space(4);
            DrawRatingGraph(graphWidth);
            EditorGUILayout.Space(4);
            DrawParamGraph(graphWidth);
            EditorGUILayout.Space(4);
            DrawFlowHeatmap(graphWidth);

            EditorGUILayout.Space(8);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ── Graph 1: Sawtooth Multiplier ──

        private void DrawSawtoothGraph(float width)
        {
            EditorGUILayout.LabelField("Sawtooth Multiplier", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(width, SawtoothGraphHeight);
            if (Event.current.type != EventType.Repaint) return;

            var gridColor = new Color(1f, 1f, 1f, 0.05f);
            EditorGraphUtility.DrawGrid(rect, 4, 10, gridColor);

            float minY = 0.5f, maxY = 2.0f;

            // Horizontal reference line at 1.0
            EditorGraphUtility.DrawHorizontalLine(rect, 1f, minY, maxY,
                new Color(1f, 1f, 1f, 0.2f), 1f);

            // Main curve
            EditorGraphUtility.DrawLineGraph(rect, _sawtoothMultipliers, minY, maxY,
                new Color(0.5f, 0.8f, 1f), 2f);

            // Boss/breather dots
            for (int i = 0; i < _levelCount; i++)
            {
                if (_sawtoothTypes[i] == LevelType.Boss)
                    EditorGraphUtility.DrawPoint(rect, i, _levelCount, _sawtoothMultipliers[i],
                        minY, maxY, new Color(1f, 0.3f, 0.2f), 4f);
                else if (_sawtoothTypes[i] == LevelType.Breather)
                    EditorGraphUtility.DrawPoint(rect, i, _levelCount, _sawtoothMultipliers[i],
                        minY, maxY, new Color(0.2f, 0.9f, 0.9f), 4f);
            }

            // Y-axis labels
            DrawYLabel(rect, "0.5", 0.5f, minY, maxY);
            DrawYLabel(rect, "1.0", 1.0f, minY, maxY);
            DrawYLabel(rect, "1.5", 1.5f, minY, maxY);
            DrawYLabel(rect, "2.0", 2.0f, minY, maxY);
        }

        // ── Graph 2: Win/Lose Outcomes ──

        private void DrawWinLoseGraph(float width)
        {
            EditorGUILayout.LabelField("Win/Lose Outcomes", EditorStyles.boldLabel);

            // Combine all runs for row layout: DDA runs first, then baseline
            var allRuns = new List<SimulationRun>(_runs);
            allRuns.AddRange(_baselineRuns);
            int totalRows = allRuns.Count;
            float graphHeight = Mathf.Max(WinLoseGraphHeight,
                totalRows > 0 ? totalRows * 12f : WinLoseGraphHeight);

            var rect = GUILayoutUtility.GetRect(width, graphHeight);
            if (Event.current.type != EventType.Repaint) return;

            float barWidth = rect.width / _levelCount;
            for (int r = 0; r < allRuns.Count; r++)
            {
                var run = allRuns[r];
                var persona = run.Persona;
                float h = rect.height / totalRows;
                float y = rect.y + r * h;

                for (int i = 0; i < run.Snapshots.Count; i++)
                {
                    var snap = run.Snapshots[i];
                    float x = rect.x + i * barWidth;

                    var color = snap.Won
                        ? new Color(persona.Color.r, persona.Color.g, persona.Color.b, 0.7f)
                        : new Color(0.4f, 0.1f, 0.1f, 0.5f);
                    var barRect = new Rect(x, y, Mathf.Max(1f, barWidth - 0.5f), h - 1f);
                    EditorGUI.DrawRect(barRect, color);
                }
            }
        }

        // ── Graph 3: Rolling Win Rate ──

        private void DrawRollingWinRateGraph(float width)
        {
            EditorGUILayout.LabelField("Rolling Win Rate (last 10 levels)", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(width, WinRateGraphHeight);
            if (Event.current.type != EventType.Repaint) return;

            var gridColor = new Color(1f, 1f, 1f, 0.05f);
            EditorGraphUtility.DrawGrid(rect, 4, 10, gridColor);

            float minY = 0f, maxY = 1f;

            // Flow channel band (0.3 - 0.7)
            float t03 = Mathf.InverseLerp(minY, maxY, 0.3f);
            float t07 = Mathf.InverseLerp(minY, maxY, 0.7f);
            float bandTop = rect.yMax - t07 * rect.height;
            float bandBot = rect.yMax - t03 * rect.height;
            EditorGUI.DrawRect(new Rect(rect.x, bandTop, rect.width, bandBot - bandTop),
                new Color(0.2f, 0.8f, 0.3f, 0.08f));

            EditorGraphUtility.DrawHorizontalLine(rect, 0.5f, minY, maxY,
                new Color(1f, 1f, 1f, 0.15f), 1f);

            // Baseline runs (thin, dim)
            foreach (var run in _baselineRuns)
            {
                var values = new float[run.Snapshots.Count];
                for (int i = 0; i < values.Length; i++)
                    values[i] = run.Snapshots[i].RollingWinRate;
                EditorGraphUtility.DrawLineGraph(rect, values, minY, maxY,
                    run.Persona.Color, 1f);
            }

            // DDA runs (thick, full color)
            foreach (var run in _runs)
            {
                var values = new float[run.Snapshots.Count];
                for (int i = 0; i < values.Length; i++)
                    values[i] = run.Snapshots[i].RollingWinRate;
                EditorGraphUtility.DrawLineGraph(rect, values, minY, maxY,
                    run.Persona.Color, 2f);
            }

            DrawYLabel(rect, "0.0", 0f, minY, maxY);
            DrawYLabel(rect, "0.5", 0.5f, minY, maxY);
            DrawYLabel(rect, "1.0", 1f, minY, maxY);
        }

        // ── Graph 4: Player Rating ──

        private void DrawRatingGraph(float width)
        {
            EditorGUILayout.LabelField("Player Rating (Glicko-2)", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(width, RatingGraphHeight);
            if (Event.current.type != EventType.Repaint) return;

            var gridColor = new Color(1f, 1f, 1f, 0.05f);
            EditorGraphUtility.DrawGrid(rect, 4, 10, gridColor);

            // Compute dynamic Y range across DDA + baseline runs
            float minRating = float.MaxValue;
            float maxRating = float.MinValue;
            var allRuns = new List<SimulationRun>(_runs);
            allRuns.AddRange(_baselineRuns);
            foreach (var run in allRuns)
            {
                for (int i = 0; i < run.Snapshots.Count; i++)
                {
                    float r = run.Snapshots[i].PlayerRating;
                    if (r < minRating) minRating = r;
                    if (r > maxRating) maxRating = r;
                }
            }
            if (minRating == float.MaxValue || maxRating == float.MinValue)
            {
                minRating = 1500f;
                maxRating = 1500f;
            }
            float padding = (maxRating - minRating) * 0.1f + 50f;
            float minY = minRating - padding;
            float maxY = maxRating + padding;

            EditorGraphUtility.DrawHorizontalLine(rect, 1500f, minY, maxY,
                new Color(1f, 1f, 1f, 0.15f), 1f);

            // Baseline runs (thin, dim)
            foreach (var run in _baselineRuns)
            {
                var values = new float[run.Snapshots.Count];
                for (int i = 0; i < values.Length; i++)
                    values[i] = run.Snapshots[i].PlayerRating;
                EditorGraphUtility.DrawLineGraph(rect, values, minY, maxY,
                    run.Persona.Color, 1f);
            }

            // DDA runs (thick, full color)
            foreach (var run in _runs)
            {
                var values = new float[run.Snapshots.Count];
                for (int i = 0; i < values.Length; i++)
                    values[i] = run.Snapshots[i].PlayerRating;
                EditorGraphUtility.DrawLineGraph(rect, values, minY, maxY,
                    run.Persona.Color, 2f);
            }

            DrawYLabel(rect, $"{minY:F0}", minY, minY, maxY);
            DrawYLabel(rect, "1500", 1500f, minY, maxY);
            DrawYLabel(rect, $"{maxY:F0}", maxY, minY, maxY);
        }

        // ── Graph 5: Adjusted Parameter Values ──

        private void DrawParamGraph(float width)
        {
            EditorGUILayout.LabelField("Adjusted Parameter Values", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(width, ParamGraphHeight);
            if (Event.current.type != EventType.Repaint) return;

            var gridColor = new Color(1f, 1f, 1f, 0.05f);
            EditorGraphUtility.DrawGrid(rect, 4, 10, gridColor);

            if (_paramEntries.Count == 0 || _runs.Count == 0) return;

            // Use first param key for the graph
            string paramKey = _paramEntries[0].Key;

            // Compute dynamic Y range
            float minVal = float.MaxValue, maxVal = float.MinValue;
            var allRuns = new List<SimulationRun>(_runs);
            allRuns.AddRange(_baselineRuns);
            foreach (var run in allRuns)
            {
                for (int i = 0; i < run.Snapshots.Count; i++)
                {
                    if (run.Snapshots[i].AdjustedParams != null &&
                        run.Snapshots[i].AdjustedParams.TryGetValue(paramKey, out float v))
                    {
                        if (v < minVal) minVal = v;
                        if (v > maxVal) maxVal = v;
                    }
                }
            }

            if (minVal > maxVal) return; // No data

            float pad = (maxVal - minVal) * 0.1f + 1f;
            float minY = minVal - pad;
            float maxY = maxVal + pad;

            // Baseline runs (thin, dim — flat since params never change)
            foreach (var run in _baselineRuns)
            {
                var values = new float[run.Snapshots.Count];
                for (int i = 0; i < values.Length; i++)
                {
                    if (run.Snapshots[i].AdjustedParams != null &&
                        run.Snapshots[i].AdjustedParams.TryGetValue(paramKey, out float v))
                        values[i] = v;
                    else
                        values[i] = i > 0 ? values[i - 1] : _paramEntries[0].Value;
                }
                EditorGraphUtility.DrawLineGraph(rect, values, minY, maxY,
                    run.Persona.Color, 1f);
            }

            // DDA runs (thick, full color)
            foreach (var run in _runs)
            {
                var values = new float[run.Snapshots.Count];
                for (int i = 0; i < values.Length; i++)
                {
                    if (run.Snapshots[i].AdjustedParams != null &&
                        run.Snapshots[i].AdjustedParams.TryGetValue(paramKey, out float v))
                        values[i] = v;
                    else
                        values[i] = i > 0 ? values[i - 1] : _paramEntries[0].Value;
                }
                EditorGraphUtility.DrawLineGraph(rect, values, minY, maxY,
                    run.Persona.Color, 2f);
            }

            DrawYLabel(rect, $"{minY:F0}", minY, minY, maxY);
            DrawYLabel(rect, $"{maxY:F0}", maxY, minY, maxY);
        }

        // ── Graph 6: Flow State Heatmap ──

        private void DrawFlowHeatmap(float width)
        {
            EditorGUILayout.LabelField("Flow State Heatmap", EditorStyles.boldLabel);

            // Legend
            EditorGUILayout.BeginHorizontal();
            DrawLegendSwatch("Flow", new Color(0.2f, 0.8f, 0.3f));
            DrawLegendSwatch("Boredom", new Color(0.9f, 0.9f, 0.2f));
            DrawLegendSwatch("Anxiety", new Color(1f, 0.6f, 0.1f));
            DrawLegendSwatch("Frustration", new Color(0.9f, 0.2f, 0.15f));
            DrawLegendSwatch("Unknown", new Color(0.3f, 0.3f, 0.3f));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            var heatmapRuns = new List<SimulationRun>(_runs);
            heatmapRuns.AddRange(_baselineRuns);

            foreach (var run in heatmapRuns)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(run.Persona.Name, GUILayout.Width(HeatmapLabelWidth));
                var rowRect = GUILayoutUtility.GetRect(width - HeatmapLabelWidth - 10f, HeatmapRowHeight);
                EditorGUILayout.EndHorizontal();

                if (Event.current.type == EventType.Repaint)
                {
                    var colors = new Color[run.Snapshots.Count];
                    for (int i = 0; i < colors.Length; i++)
                        colors[i] = GetFlowColor(run.Snapshots[i].FlowState);
                    EditorGraphUtility.DrawColorStrip(rowRect, colors);
                }
            }
        }

        // ════════════════════════════════════════════════════════════
        //  BOTTOM PANEL — Statistics Table
        // ════════════════════════════════════════════════════════════

        private void DrawBottomPanel()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

            _bottomScroll = EditorGUILayout.BeginScrollView(_bottomScroll,
                GUILayout.Height(BottomPanelHeight));

            // Merge DDA + baseline runs for the table
            var allRuns = new List<SimulationRun>(_runs);
            allRuns.AddRange(_baselineRuns);

            // Header row
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawSortableHeader("Persona", 0, 140);
            DrawSortableHeader("Win Rate", 1, 70);
            DrawSortableHeader("Final Rating", 2, 80);
            DrawSortableHeader("Win Streak", 3, 70);
            DrawSortableHeader("Lose Streak", 4, 75);
            DrawSortableHeader("Flow %", 5, 60);
            DrawSortableHeader("Frustration %", 6, 85);
            DrawSortableHeader("Avg Delta", 7, 70);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Data rows
            if (_sortedRunIndices == null || _sortedRunIndices.Length != allRuns.Count)
                RebuildSortIndices();

            for (int si = 0; si < _sortedRunIndices.Length; si++)
            {
                int idx = _sortedRunIndices[si];
                if (idx >= allRuns.Count) continue;
                var run = allRuns[idx];
                int total = run.Snapshots.Count;

                EditorGUILayout.BeginHorizontal();
                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = run.Persona.Color;
                EditorGUILayout.LabelField(run.Persona.Name, GUILayout.Width(140));
                GUI.backgroundColor = prevBg;

                EditorGUILayout.LabelField($"{run.OverallWinRate:P0}", GUILayout.Width(70));
                EditorGUILayout.LabelField($"{run.FinalRating:F0}", GUILayout.Width(80));
                EditorGUILayout.LabelField($"{run.LongestWinStreak}", GUILayout.Width(70));
                EditorGUILayout.LabelField($"{run.LongestLoseStreak}", GUILayout.Width(75));
                EditorGUILayout.LabelField(
                    total > 0 ? $"{(float)run.FlowCount / total:P0}" : "-",
                    GUILayout.Width(60));
                EditorGUILayout.LabelField(
                    total > 0 ? $"{(float)run.FrustrationCount / total:P0}" : "-",
                    GUILayout.Width(85));
                EditorGUILayout.LabelField($"{run.AverageAdjustmentDelta:F2}",
                    GUILayout.Width(70));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSortableHeader(string label, int colIndex, float width)
        {
            string arrow = _sortColumn == colIndex
                ? (_sortAscending ? " \u25b2" : " \u25bc")
                : "";
            if (GUILayout.Button(label + arrow, EditorStyles.toolbarButton,
                GUILayout.Width(width)))
            {
                if (_sortColumn == colIndex)
                    _sortAscending = !_sortAscending;
                else
                {
                    _sortColumn = colIndex;
                    _sortAscending = true;
                }
                RebuildSortIndices();
            }
        }

        private void RebuildSortIndices()
        {
            var allRuns = new List<SimulationRun>(_runs);
            allRuns.AddRange(_baselineRuns);

            _sortedRunIndices = new int[allRuns.Count];
            for (int i = 0; i < allRuns.Count; i++)
                _sortedRunIndices[i] = i;

            if (_sortColumn < 0) return;

            Array.Sort(_sortedRunIndices, (a, b) =>
            {
                float va = GetSortValue(allRuns[a], _sortColumn);
                float vb = GetSortValue(allRuns[b], _sortColumn);
                int cmp = va.CompareTo(vb);
                return _sortAscending ? cmp : -cmp;
            });
        }

        private float GetSortValue(SimulationRun run, int col)
        {
            int total = Mathf.Max(1, run.Snapshots.Count);
            switch (col)
            {
                case 0: return 0; // Name — no numeric sort
                case 1: return run.OverallWinRate;
                case 2: return run.FinalRating;
                case 3: return run.LongestWinStreak;
                case 4: return run.LongestLoseStreak;
                case 5: return (float)run.FlowCount / total;
                case 6: return (float)run.FrustrationCount / total;
                case 7: return run.AverageAdjustmentDelta;
                default: return 0;
            }
        }

        // ════════════════════════════════════════════════════════════
        //  SIMULATION LOOP
        // ════════════════════════════════════════════════════════════

        private void RunSimulation()
        {
            if (_ddaConfig == null) return;
            if (_ddaConfig.SawtoothCurveConfig == null)
            {
                Debug.LogWarning("[Scenario Simulator] DDAConfig has no SawtoothCurveConfig assigned.");
                return;
            }

            _runs.Clear();
            _baselineRuns.Clear();
            _hasResults = false;

            // Pre-compute sawtooth curve
            var scheduler = new DifficultyScheduler(_ddaConfig.SawtoothCurveConfig);
            _sawtoothMultipliers = new float[_levelCount];
            _sawtoothTypes = new LevelType[_levelCount];
            for (int i = 0; i < _levelCount; i++)
            {
                _sawtoothMultipliers[i] = scheduler.GetTargetMultiplier(i);
                _sawtoothTypes[i] = scheduler.GetSuggestedLevelType(i);
            }

            // Run each enabled persona — with DDA and optionally without
            for (int p = 0; p < _personas.Length; p++)
            {
                if (!_personaEnabled[p]) continue;

                var run = SimulatePersona(_personas[p], scheduler, useDDA: true);
                run.ComputeAggregates();
                _runs.Add(run);

                if (_showBaseline)
                {
                    var baseline = SimulatePersona(_personas[p], scheduler, useDDA: false);
                    baseline.ComputeAggregates();
                    _baselineRuns.Add(baseline);
                }
            }

            _hasResults = _runs.Count > 0;
            _sortedRunIndices = null;
            Repaint();
        }

        private SimulationRun SimulatePersona(PlayerPersona persona,
            DifficultyScheduler scheduler, bool useDDA)
        {
            // Build a persona copy for baseline labeling
            var runPersona = useDDA
                ? persona
                : new PlayerPersona
                {
                    Name = persona.Name + " (No DDA)",
                    Color = new Color(persona.Color.r, persona.Color.g, persona.Color.b, 0.4f),
                    BaseWinRate = persona.BaseWinRate,
                    MeanMoveCount = persona.MeanMoveCount,
                    MoveCountVariance = persona.MoveCountVariance,
                    OptimalMoveRatio = persona.OptimalMoveRatio,
                    MeanSessionTime = persona.MeanSessionTime,
                    MeanInterMoveTime = persona.MeanInterMoveTime,
                    BoosterUseRate = persona.BoosterUseRate,
                    PauseRate = persona.PauseRate,
                    SkillGrowthRate = persona.SkillGrowthRate,
                };

            var run = new SimulationRun { Persona = runPersona };

            // Keep runtime path identical for DDA and baseline.
            // Baseline mode simply skips proposal application.
            var service = new DDAService(_ddaConfig);

            // Same RNG seed for both DDA and baseline so win/lose sequence is comparable
            var rng = new System.Random(persona.Name.GetHashCode() ^ _seed);

            // Build initial parameter dictionary
            var baseParams = BuildParamDict();
            var currentParams = new Dictionary<string, float>(baseParams);

            // Rolling win rate window
            var recentOutcomes = new Queue<bool>();
            const int rollingWindow = 10;

            for (int i = 0; i < _levelCount; i++)
            {
                var levelType = scheduler.GetSuggestedLevelType(i);
                float multiplier = scheduler.GetTargetMultiplier(i);

                // Base effective win rate: persona (+ growth) modulated by sawtooth
                float effectiveWinRate = persona.EffectiveWinRate(i) * (2f - multiplier);

                // DDA parameter shift: the DDA convention is "lower param = easier difficulty."
                // When DDA DECREASES a param, it intends to help the player → boost win rate.
                // So paramShift is (base - current) / base: positive when DDA reduced the param.
                // For baseline runs, currentParams == baseParams so shift is always 0.
                float paramShift = 0f;
                foreach (var kvp in currentParams)
                {
                    if (baseParams.TryGetValue(kvp.Key, out float baseVal) &&
                        Mathf.Abs(baseVal) > 0.001f)
                    {
                        paramShift += (baseVal - kvp.Value) / baseVal;
                    }
                }
                if (currentParams.Count > 0)
                    paramShift /= currentParams.Count;

                // Diminishing returns: tanh(shift) squashes large adjustments.
                // DDA reduces move_limit 30→27 (−10%): tanh(0.1)=0.100 → +3.0% win rate boost
                // DDA reduces move_limit 30→15 (−50%): tanh(0.5)=0.462 → +13.9% win rate boost
                // DDA reduces move_limit 30→6  (−80%): tanh(0.8)=0.664 → +19.9% win rate boost
                const float sensitivity = 0.3f;
                float dampedShift = (float)System.Math.Tanh(paramShift) * sensitivity;
                effectiveWinRate *= 1f + dampedShift;

                effectiveWinRate = Mathf.Clamp(effectiveWinRate, 0.05f, 0.95f);

                bool won = rng.NextDouble() < effectiveWinRate;

                FlowState flowState = FlowState.Unknown;
                float adjustmentDelta = 0f;
                float playerRating = 1500f;
                float playerDeviation = 350f;

                // Full runtime loop (both DDA and baseline)
                service.BeginSession("level_" + i,
                    new Dictionary<string, float>(currentParams), levelType);

                InjectSignals(service, persona, won, rng, i);

                float elapsedTime = 0.016f * persona.MeanMoveCount;
                service.Tick(elapsedTime);

                service.EndSession(won ? SessionOutcome.Win : SessionOutcome.Lose);

                var debug = service.GetDebugSnapshot();
                flowState = debug.CurrentFlow.State;

                if (useDDA)
                {
                    var nextLevelType = (i + 1 < _levelCount)
                        ? scheduler.GetSuggestedLevelType(i + 1)
                        : LevelType.Standard;
                    var proposal = service.GetProposal(
                        new Dictionary<string, float>(currentParams), nextLevelType, i + 1);

                    if (proposal?.Deltas != null)
                    {
                        foreach (var d in proposal.Deltas)
                            adjustmentDelta += Mathf.Abs(d.ProposedValue - d.CurrentValue);
                    }

                    ApplyProposal(proposal, currentParams);
                }

                var profile = service.PlayerProfile;
                playerRating = profile.Rating;
                playerDeviation = profile.Deviation;

                // Rolling win rate
                recentOutcomes.Enqueue(won);
                while (recentOutcomes.Count > rollingWindow)
                    recentOutcomes.Dequeue();
                int wins = 0;
                foreach (bool w in recentOutcomes)
                    if (w) wins++;
                float rollingWinRate = (float)wins / recentOutcomes.Count;

                run.Snapshots.Add(new LevelSnapshot
                {
                    LevelIndex = i,
                    LevelType = levelType,
                    SawtoothMultiplier = multiplier,
                    Won = won,
                    PlayerRating = playerRating,
                    PlayerDeviation = playerDeviation,
                    FlowState = flowState,
                    RollingWinRate = rollingWinRate,
                    AdjustedParams = new Dictionary<string, float>(currentParams),
                    AdjustmentDelta = adjustmentDelta
                });
            }

            return run;
        }

        private void InjectSignals(DDAService service, PlayerPersona persona,
            bool won, System.Random rng, int levelIndex)
        {
            // Determine move count with variance
            int moveCount = Mathf.Max(3, Mathf.RoundToInt(
                persona.MeanMoveCount + (float)(rng.NextDouble() * 2 - 1) * persona.MoveCountVariance));

            float optimalPct = won
                ? Mathf.Clamp01(persona.OptimalMoveRatio + 0.15f)
                : Mathf.Clamp01(persona.OptimalMoveRatio - 0.15f);

            int optimalCount = Mathf.RoundToInt(moveCount * optimalPct);

            for (int m = 0; m < moveCount; m++)
            {
                service.RecordSignal(SignalKeys.MoveExecuted, 1f,
                    SignalTier.DecisionQuality, m + 1);

                if (m < optimalCount)
                {
                    service.RecordSignal(SignalKeys.MoveOptimal, 1f,
                        SignalTier.DecisionQuality, m + 1);
                    service.RecordSignal(SignalKeys.ProgressDelta, 0.05f,
                        SignalTier.DecisionQuality, m + 1);
                }
                else
                {
                    service.RecordSignal(SignalKeys.MoveWaste, 1f,
                        SignalTier.DecisionQuality, m + 1);
                }

                service.RecordSignal(SignalKeys.InterMoveInterval, persona.MeanInterMoveTime,
                    SignalTier.BehavioralTempo, m + 1);
                service.Tick(persona.MeanInterMoveTime);
            }

            // Boosters
            if (rng.NextDouble() < persona.BoosterUseRate)
            {
                service.RecordSignal(SignalKeys.PowerUpUsed, 1f,
                    SignalTier.StrategicPattern);
                service.Tick(0.5f);
            }

            // Pauses
            if (rng.NextDouble() < persona.PauseRate)
            {
                service.RecordSignal(SignalKeys.PauseTriggered, 1f,
                    SignalTier.BehavioralTempo);
                service.Tick(2f);
            }
        }

        private static void ApplyProposal(AdjustmentProposal proposal,
            Dictionary<string, float> currentParams)
        {
            if (proposal?.Deltas == null) return;
            foreach (var d in proposal.Deltas)
            {
                if (currentParams.ContainsKey(d.ParameterKey))
                    currentParams[d.ParameterKey] = d.ProposedValue;
            }
        }

        private Dictionary<string, float> BuildParamDict()
        {
            var dict = new Dictionary<string, float>();
            for (int i = 0; i < _paramEntries.Count; i++)
            {
                if (!string.IsNullOrEmpty(_paramEntries[i].Key))
                    dict[_paramEntries[i].Key] = _paramEntries[i].Value;
            }
            return dict;
        }

        // ════════════════════════════════════════════════════════════
        //  CSV EXPORT
        // ════════════════════════════════════════════════════════════

        private void ExportCSV()
        {
            var path = EditorUtility.SaveFilePanel(
                "Export Simulation Results", "", "scenario_simulation.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();

            // Header
            sb.Append("Persona,DDA,LevelIndex,LevelType,SawtoothMultiplier,Won,");
            sb.Append("PlayerRating,PlayerDeviation,FlowState,RollingWinRate,AdjustmentDelta");
            foreach (var p in _paramEntries)
                sb.Append("," + p.Key);
            sb.AppendLine();

            // Data — DDA runs + baseline runs
            var allExportRuns = new List<SimulationRun>(_runs);
            allExportRuns.AddRange(_baselineRuns);
            foreach (var run in allExportRuns)
            {
                foreach (var snap in run.Snapshots)
                {
                    bool isDDA = !run.Persona.Name.EndsWith("(No DDA)");
                    string baseName = isDDA ? run.Persona.Name : run.Persona.Name.Replace(" (No DDA)", "");
                    sb.Append($"{baseName},{(isDDA ? "Yes" : "No")},{snap.LevelIndex},{snap.LevelType},");
                    sb.Append($"{snap.SawtoothMultiplier:F4},{(snap.Won ? 1 : 0)},");
                    sb.Append($"{snap.PlayerRating:F1},{snap.PlayerDeviation:F1},");
                    sb.Append($"{snap.FlowState},{snap.RollingWinRate:F3},{snap.AdjustmentDelta:F4}");
                    foreach (var p in _paramEntries)
                    {
                        float val = 0f;
                        snap.AdjustedParams?.TryGetValue(p.Key, out val);
                        sb.Append($",{val:F2}");
                    }
                    sb.AppendLine();
                }
            }

            File.WriteAllText(path, sb.ToString());
            Debug.Log($"[Scenario Simulator] Exported {allExportRuns.Count} persona runs to: {path}");
        }

        // ════════════════════════════════════════════════════════════
        //  UI HELPERS
        // ════════════════════════════════════════════════════════════

        private static void DrawYLabel(Rect graphRect, string text, float value,
            float minY, float maxY)
        {
            float t = Mathf.InverseLerp(minY, maxY, value);
            float y = graphRect.yMax - t * graphRect.height;
            var labelRect = new Rect(graphRect.x + 2, y - 7, 50, 14);
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 1f, 1f, 0.5f) }
            };
            GUI.Label(labelRect, text, style);
        }

        private static void DrawLegendSwatch(string label, Color color)
        {
            var rect = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rect, color);
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel, GUILayout.Width(65));
        }

        private static Color GetFlowColor(FlowState state)
        {
            switch (state)
            {
                case FlowState.Flow:        return new Color(0.2f, 0.8f, 0.3f);
                case FlowState.Boredom:     return new Color(0.9f, 0.9f, 0.2f);
                case FlowState.Anxiety:     return new Color(1f, 0.6f, 0.1f);
                case FlowState.Frustration: return new Color(0.9f, 0.2f, 0.15f);
                default:                    return new Color(0.3f, 0.3f, 0.3f);
            }
        }
    }
}
