using UnityEditor;
using UnityEngine;
using Cadence;

namespace Cadence.Editor
{
    public class DDADebugWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private IDDAService _service;
        private DDADebugData _snapshot;
        private float _refreshInterval = 0.25f;
        private double _lastRefresh;

        [MenuItem("Cadence/Debug Window")]
        public static void ShowWindow()
        {
            GetWindow<DDADebugWindow>("DDA Debug");
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            _service = null;
            _snapshot = null;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup - _lastRefresh < _refreshInterval) return;
            _lastRefresh = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see DDA data.", MessageType.Info);
                return;
            }

            // Try auto-discovery via CadenceManager
            if (_service == null && CadenceManager.Instance != null)
                _service = CadenceManager.Service;

            if (_service == null)
            {
                EditorGUILayout.HelpBox(
                    "No IDDAService found.\n\n" +
                    "Either add a CadenceManager to your scene (Cadence > Create Manager in Scene) " +
                    "or call DDADebugWindow.SetService() from your game code.",
                    MessageType.Warning);

                if (GUILayout.Button("Refresh"))
                    _service = null;
                return;
            }

            _snapshot = _service.GetDebugSnapshot();
            if (_snapshot == null) return;

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawSessionInfo();
            DrawFlowState();
            DrawPlayerProfile();
            DrawArchetype();
            DrawLastProposal();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Call this from your game code to provide the service reference.
        /// </summary>
        public static void SetService(IDDAService service)
        {
            var window = GetWindow<DDADebugWindow>(utility: false, title: "DDA Debug", focus: false);
            if (window != null)
                window._service = service;
        }

        private void DrawSessionInfo()
        {
            EditorGUILayout.LabelField("Session", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.Toggle("Active", _snapshot.SessionActive);
            EditorGUILayout.TextField("Level", _snapshot.CurrentLevelId ?? "-");
            EditorGUILayout.EnumPopup("Level Type", _snapshot.CurrentLevelType);
            EditorGUILayout.FloatField("Time", _snapshot.SessionTime);
            EditorGUILayout.IntField("Signals", _snapshot.TotalSignals);
            EditorGUILayout.IntField("Ring Buffer", _snapshot.RingBufferCount);
            EditorGUILayout.IntField("Levels This Session", _snapshot.LevelsThisSession);
            EditorGUILayout.Toggle("Fatigue Active", _snapshot.SessionFatigueActive);
            EditorGUILayout.Toggle("Abandon Pending", _snapshot.ExplicitAbandonPending);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawFlowState()
        {
            var flow = _snapshot.CurrentFlow;
            EditorGUILayout.LabelField("Flow State", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            Color stateColor = CadenceEditorStyles.GetFlowColor(flow.State);
            var prevColor = GUI.color;
            GUI.color = stateColor;
            EditorGUILayout.EnumPopup("State", flow.State);
            GUI.color = prevColor;

            EditorGUILayout.Slider("Confidence", flow.Confidence, 0f, 1f);
            EditorGUILayout.Slider("Tempo", flow.TempoScore, 0f, 1f);
            EditorGUILayout.Slider("Efficiency", flow.EfficiencyScore, 0f, 1f);
            EditorGUILayout.Slider("Engagement", flow.EngagementScore, 0f, 1f);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawPlayerProfile()
        {
            var p = _snapshot.Profile;
            if (p == null) return;

            EditorGUILayout.LabelField("Player Profile", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.FloatField("Rating", p.Rating);
            EditorGUILayout.FloatField("Deviation", p.Deviation);
            EditorGUILayout.Slider("Confidence", p.Confidence01, 0f, 1f);
            EditorGUILayout.IntField("Sessions", p.SessionsCompleted);
            EditorGUILayout.FloatField("Avg Efficiency", p.AverageEfficiency);
            EditorGUILayout.FloatField("Win Rate", p.AverageOutcome);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawArchetype()
        {
            var a = _snapshot.ArchetypeReading;
            EditorGUILayout.LabelField("Player Archetype", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Primary",
                $"{a.Primary} ({a.PrimaryConfidence:P0})");
            EditorGUILayout.LabelField("Secondary",
                $"{a.Secondary} ({a.SecondaryConfidence:P0})");
            EditorGUILayout.Slider("SpeedRunner", a.SpeedRunnerScore, 0f, 1f);
            EditorGUILayout.Slider("CarefulThinker", a.CarefulThinkerScore, 0f, 1f);
            EditorGUILayout.Slider("StrugglingLearner", a.StrugglingLearnerScore, 0f, 1f);
            EditorGUILayout.Slider("BoosterDependent", a.BoosterDependentScore, 0f, 1f);
            EditorGUILayout.Slider("ChurnRisk", a.ChurnRiskScore, 0f, 1f);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawLastProposal()
        {
            var p = _snapshot.LastProposal;
            if (p == null || p.Deltas == null || p.Deltas.Count == 0)
            {
            EditorGUILayout.LabelField("Last Proposal", "None");
            return;
        }

            EditorGUILayout.LabelField("Last Proposal", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.Slider("Confidence", p.Confidence, 0f, 1f);
            EditorGUILayout.EnumPopup("Flow State", p.DetectedState);
            EditorGUILayout.EnumPopup("Timing", p.Timing);
            if (!string.IsNullOrEmpty(p.Reason))
                EditorGUILayout.LabelField("Reason", p.Reason);

            for (int i = 0; i < p.Deltas.Count; i++)
            {
                var d = p.Deltas[i];
                EditorGUILayout.LabelField(
                    $"  {d.ParameterKey}",
                    $"{d.CurrentValue:F2} -> {d.ProposedValue:F2} ({d.Delta:+0.00;-0.00}) [{d.RuleName}]"
                );
            }
            EditorGUI.indentLevel--;
        }

    }
}
