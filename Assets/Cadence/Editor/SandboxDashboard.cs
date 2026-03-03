using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Cadence;

namespace Cadence.Editor
{
    public class SandboxDashboard : EditorWindow
    {
        // Config
        private DDAConfig _ddaConfig;
        private DDAService _service;

        // Left Panel: Player profile
        private float _initialRating = 1500f;
        private float _initialDeviation = 350f;
        private float _initialWinRate = 0.5f;
        private int _initialSessions = 0;
        private LevelType _levelType = LevelType.Standard;
        private int _levelIndex = 0;

        // Level parameters
        private List<ParamEntry> _levelParams = new List<ParamEntry>
        {
            new ParamEntry { Key = "difficulty", Value = 100f },
            new ParamEntry { Key = "move_limit", Value = 30f }
        };

        // Middle Panel: Signal injection
        private int _moveCount = 10;
        private float _optimalPercent = 0.5f;
        private int _simulateCount = 5;
        private bool _simulateWins = false;

        // Right Panel: History
        private List<ProposalRecord> _proposalHistory = new List<ProposalRecord>();
        private Vector2 _leftScroll, _middleScroll, _rightScroll, _bottomScroll;

        // Compare mode
        private bool _compareMode;
        private DDAConfig _compareConfig;
        private DDAService _compareService;
        private List<ProposalRecord> _compareHistory = new List<ProposalRecord>();

        [MenuItem("Cadence/Sandbox Dashboard")]
        public static void ShowWindow()
        {
            var window = GetWindow<SandboxDashboard>("Sandbox Dashboard");
            window.minSize = new Vector2(900, 600);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(60)))
                ResetService();
            _compareMode = GUILayout.Toggle(_compareMode, "Compare Mode",
                EditorStyles.toolbarButton, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            if (_service != null)
            {
                var color = _service.IsSessionActive ? Color.green : Color.gray;
                var prev = GUI.color;
                GUI.color = color;
                GUILayout.Label(_service.IsSessionActive ? "SESSION ACTIVE" : "NO SESSION",
                    EditorStyles.toolbarButton);
                GUI.color = prev;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawMiddlePanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            DrawBottomPanel();
        }

        // ---- LEFT PANEL: Config + Player Setup ----

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(260));
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _ddaConfig = (DDAConfig)EditorGUILayout.ObjectField(
                "DDA Config", _ddaConfig, typeof(DDAConfig), false);
            if (EditorGUI.EndChangeCheck())
                _service = null;

            if (_compareMode)
            {
                _compareConfig = (DDAConfig)EditorGUILayout.ObjectField(
                    "Compare Config", _compareConfig, typeof(DDAConfig), false);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Player Profile", EditorStyles.boldLabel);
            _initialRating = EditorGUILayout.FloatField("Rating", _initialRating);
            _initialDeviation = EditorGUILayout.FloatField("Deviation", _initialDeviation);
            _initialWinRate = EditorGUILayout.Slider("Win Rate", _initialWinRate, 0f, 1f);
            _initialSessions = EditorGUILayout.IntField("Sessions", _initialSessions);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Level Setup", EditorStyles.boldLabel);
            _levelType = (LevelType)EditorGUILayout.EnumPopup("Level Type", _levelType);
            _levelIndex = EditorGUILayout.IntField("Level Index", _levelIndex);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Level Parameters", EditorStyles.boldLabel);
            for (int i = 0; i < _levelParams.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _levelParams[i] = new ParamEntry
                {
                    Key = EditorGUILayout.TextField(_levelParams[i].Key, GUILayout.Width(90)),
                    Value = EditorGUILayout.FloatField(_levelParams[i].Value)
                };
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    _levelParams.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Add Parameter"))
                _levelParams.Add(new ParamEntry { Key = "param", Value = 1f });

            EditorGUILayout.Space();
            if (GUILayout.Button("Initialize / Reset", GUILayout.Height(25)))
                ResetService();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ---- MIDDLE PANEL: Signal Injection ----

        private void DrawMiddlePanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            _middleScroll = EditorGUILayout.BeginScrollView(_middleScroll);

            EditorGUILayout.LabelField("Session Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _service != null && !_service.IsSessionActive;
            if (GUILayout.Button("Begin Session"))
            {
                var dict = BuildParamDict();
                _service.BeginSession("sandbox_level", dict, _levelType);
                if (_compareMode && _compareService != null)
                    _compareService.BeginSession("sandbox_level", dict, _levelType);
            }
            GUI.enabled = _service != null && _service.IsSessionActive;
            if (GUILayout.Button("End Win"))
                EndAndPropose(SessionOutcome.Win);
            if (GUILayout.Button("End Lose"))
                EndAndPropose(SessionOutcome.Lose);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Signal Injection", EditorStyles.boldLabel);

            // Signal grid
            GUI.enabled = _service != null && _service.IsSessionActive;
            DrawSignalGrid();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Batch Inject Moves", EditorStyles.boldLabel);
            _moveCount = EditorGUILayout.IntSlider("Move Count", _moveCount, 1, 100);
            _optimalPercent = EditorGUILayout.Slider("Optimal %", _optimalPercent, 0f, 1f);
            if (GUILayout.Button("Inject Moves"))
                InjectMoves(_moveCount, _optimalPercent);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tick", EditorStyles.boldLabel);
            if (GUILayout.Button("Tick (0.5s)"))
            {
                _service?.Tick(0.5f);
                if (_compareMode) _compareService?.Tick(0.5f);
                Repaint();
            }
            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSignalGrid()
        {
            string[] signals = new[]
            {
                SignalKeys.MoveExecuted, SignalKeys.MoveOptimal, SignalKeys.MoveWaste,
                SignalKeys.ProgressDelta, SignalKeys.InterMoveInterval, SignalKeys.HesitationTime,
                SignalKeys.PauseTriggered, SignalKeys.PowerUpUsed, SignalKeys.SequenceMatch
            };

            int cols = 3;
            for (int i = 0; i < signals.Length; i++)
            {
                if (i % cols == 0) EditorGUILayout.BeginHorizontal();

                string label = signals[i].Replace(".", "\n");
                if (GUILayout.Button(label, GUILayout.Height(35), GUILayout.Width(90)))
                {
                    _service?.RecordSignal(signals[i]);
                    if (_compareMode) _compareService?.RecordSignal(signals[i]);
                }

                if (i % cols == cols - 1 || i == signals.Length - 1)
                    EditorGUILayout.EndHorizontal();
            }
        }

        // ---- RIGHT PANEL: Live State ----

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

            if (_service == null)
            {
                EditorGUILayout.HelpBox("Click 'Initialize' to create a sandbox service.",
                    MessageType.Info);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            // Flow state
            var flow = _service.CurrentFlow;
            EditorGUILayout.LabelField("Flow State", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            var flowColor = GetFlowColor(flow.State);
            var prev = GUI.color;
            GUI.color = flowColor;
            EditorGUILayout.EnumPopup("State", flow.State);
            GUI.color = prev;
            EditorGUILayout.Slider("Tempo", flow.TempoScore, 0f, 1f);
            EditorGUILayout.Slider("Efficiency", flow.EfficiencyScore, 0f, 1f);
            EditorGUILayout.Slider("Engagement", flow.EngagementScore, 0f, 1f);
            EditorGUILayout.Slider("Confidence", flow.Confidence, 0f, 1f);
            EditorGUI.indentLevel--;

            // Archetype
            EditorGUILayout.Space();
            var archetype = _service.CurrentArchetype;
            EditorGUILayout.LabelField("Player Archetype", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Primary",
                $"{archetype.Primary} ({archetype.PrimaryConfidence:P0})");
            EditorGUILayout.LabelField("Secondary",
                $"{archetype.Secondary} ({archetype.SecondaryConfidence:P0})");
            EditorGUI.indentLevel--;

            // Player profile
            EditorGUILayout.Space();
            var profile = _service.PlayerProfile;
            EditorGUILayout.LabelField("Player Profile", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.FloatField("Rating", profile.Rating);
            EditorGUILayout.FloatField("Deviation", profile.Deviation);
            EditorGUILayout.IntField("Sessions", profile.SessionsCompleted);
            EditorGUILayout.FloatField("Win Rate", profile.AverageOutcome);
            EditorGUILayout.Slider("Confidence", profile.Confidence01, 0f, 1f);
            EditorGUI.indentLevel--;

            // Current proposal
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Proposal", EditorStyles.boldLabel);
            if (_proposalHistory.Count > 0)
            {
                var last = _proposalHistory[_proposalHistory.Count - 1];
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Confidence", last.Confidence.ToString("F2"));
                EditorGUILayout.LabelField("Flow", last.FlowState.ToString());
                foreach (var d in last.Deltas)
                {
                    EditorGUILayout.LabelField($"  {d.Key}",
                        $"{d.From:F2} -> {d.To:F2} ({d.Delta:+0.00;-0.00}) [{d.Rule}]");
                }
                EditorGUI.indentLevel--;
            }

            // Proposal history
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                $"Proposal History ({_proposalHistory.Count})", EditorStyles.boldLabel);
            int start = Mathf.Max(0, _proposalHistory.Count - 10);
            for (int i = _proposalHistory.Count - 1; i >= start; i--)
            {
                var rec = _proposalHistory[i];
                EditorGUILayout.LabelField($"#{i + 1}",
                    $"Flow={rec.FlowState} Deltas={rec.Deltas.Count} Conf={rec.Confidence:F2}");
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ---- BOTTOM PANEL: Batch Simulation ----

        private void DrawBottomPanel()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Batch Simulation", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            _simulateCount = EditorGUILayout.IntSlider("Count", _simulateCount, 1, 50);
            _simulateWins = EditorGUILayout.Toggle("Wins", _simulateWins);

            GUI.enabled = _service != null;
            if (GUILayout.Button("Simulate", GUILayout.Width(80)))
                RunBatchSimulation(_simulateCount, _simulateWins);
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            // Compare display
            if (_compareMode && _compareHistory.Count > 0 && _proposalHistory.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Compare Results", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Config A", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Config B", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                int count = Mathf.Min(_proposalHistory.Count, _compareHistory.Count);
                int show = Mathf.Min(count, 10);
                for (int i = count - show; i < count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"#{i + 1} d={SumDeltas(_proposalHistory[i]):+0.00;-0.00}");
                    EditorGUILayout.LabelField($"#{i + 1} d={SumDeltas(_compareHistory[i]):+0.00;-0.00}");
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        // ---- Logic ----

        private void ResetService()
        {
            if (_ddaConfig == null)
            {
                Debug.LogWarning("[Cadence Sandbox] Assign a DDAConfig first.");
                return;
            }

            _service = new DDAService(_ddaConfig);
            _proposalHistory.Clear();

            if (_compareMode && _compareConfig != null)
            {
                _compareService = new DDAService(_compareConfig);
                _compareHistory.Clear();
            }

            Repaint();
        }

        private void EndAndPropose(SessionOutcome outcome)
        {
            if (_service == null) return;

            _service.EndSession(outcome);
            var dict = BuildParamDict();
            var proposal = _service.GetProposal(dict, _levelType, _levelIndex);
            RecordProposal(proposal, _proposalHistory);

            if (_compareMode && _compareService != null)
            {
                _compareService.EndSession(outcome);
                var cProposal = _compareService.GetProposal(dict, _levelType, _levelIndex);
                RecordProposal(cProposal, _compareHistory);
            }

            Repaint();
        }

        private void InjectMoves(int count, float optimalPct)
        {
            if (_service == null || !_service.IsSessionActive) return;

            int optimalCount = Mathf.RoundToInt(count * optimalPct);
            for (int i = 0; i < count; i++)
            {
                _service.RecordSignal(SignalKeys.MoveExecuted, 1f,
                    SignalTier.DecisionQuality, i);
                if (i < optimalCount)
                    _service.RecordSignal(SignalKeys.MoveOptimal, 1f,
                        SignalTier.DecisionQuality, i);
                else
                    _service.RecordSignal(SignalKeys.MoveWaste, 1f,
                        SignalTier.DecisionQuality, i);
                _service.RecordSignal(SignalKeys.InterMoveInterval, 1.5f,
                    SignalTier.BehavioralTempo, i);
                _service.Tick(0.5f);

                if (_compareMode && _compareService != null && _compareService.IsSessionActive)
                {
                    _compareService.RecordSignal(SignalKeys.MoveExecuted, 1f,
                        SignalTier.DecisionQuality, i);
                    if (i < optimalCount)
                        _compareService.RecordSignal(SignalKeys.MoveOptimal, 1f,
                            SignalTier.DecisionQuality, i);
                    else
                        _compareService.RecordSignal(SignalKeys.MoveWaste, 1f,
                            SignalTier.DecisionQuality, i);
                    _compareService.RecordSignal(SignalKeys.InterMoveInterval, 1.5f,
                        SignalTier.BehavioralTempo, i);
                    _compareService.Tick(0.5f);
                }
            }

            Repaint();
        }

        private void RunBatchSimulation(int count, bool wins)
        {
            if (_service == null) return;

            for (int i = 0; i < count; i++)
            {
                var dict = BuildParamDict();
                _service.BeginSession($"sim_{i}", dict, _levelType);
                InjectMoves(15, wins ? 0.8f : 0.2f);
                EndAndPropose(wins ? SessionOutcome.Win : SessionOutcome.Lose);
            }

            Repaint();
        }

        private Dictionary<string, float> BuildParamDict()
        {
            var dict = new Dictionary<string, float>();
            for (int i = 0; i < _levelParams.Count; i++)
            {
                if (!string.IsNullOrEmpty(_levelParams[i].Key))
                    dict[_levelParams[i].Key] = _levelParams[i].Value;
            }
            return dict;
        }

        private static void RecordProposal(AdjustmentProposal proposal,
            List<ProposalRecord> history)
        {
            var rec = new ProposalRecord();
            if (proposal != null)
            {
                rec.Confidence = proposal.Confidence;
                rec.FlowState = proposal.DetectedState;
                if (proposal.Deltas != null)
                {
                    foreach (var d in proposal.Deltas)
                    {
                        rec.Deltas.Add(new DeltaRecord
                        {
                            Key = d.ParameterKey,
                            From = d.CurrentValue,
                            To = d.ProposedValue,
                            Delta = d.Delta,
                            Rule = d.RuleName
                        });
                    }
                }
            }
            history.Add(rec);
        }

        private static float SumDeltas(ProposalRecord rec)
        {
            float sum = 0f;
            foreach (var d in rec.Deltas) sum += d.Delta;
            return sum;
        }

        private static Color GetFlowColor(FlowState state)
        {
            switch (state)
            {
                case FlowState.Boredom:     return new Color(0.3f, 0.6f, 1f);
                case FlowState.Flow:        return new Color(0.2f, 0.9f, 0.3f);
                case FlowState.Anxiety:     return new Color(1f, 0.8f, 0.2f);
                case FlowState.Frustration: return new Color(1f, 0.3f, 0.2f);
                default:                    return Color.gray;
            }
        }

        // ---- Data Types ----

        private struct ParamEntry
        {
            public string Key;
            public float Value;
        }

        private class ProposalRecord
        {
            public float Confidence;
            public FlowState FlowState;
            public List<DeltaRecord> Deltas = new List<DeltaRecord>();
        }

        private struct DeltaRecord
        {
            public string Key;
            public float From;
            public float To;
            public float Delta;
            public string Rule;
        }
    }
}
