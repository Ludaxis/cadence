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

        // Player profile setup
        private float _initialRating = 1500f;
        private float _initialDeviation = 350f;
        private int _initialSessions = 0;
        private float _initialWinRate = 0.5f;
        private LevelType _levelType = LevelType.Standard;
        private int _levelIndex = 0;

        // Level parameters
        private List<ParamEntry> _levelParams = new List<ParamEntry>
        {
            new ParamEntry { Key = "difficulty", Value = 100f },
            new ParamEntry { Key = "move_limit", Value = 30f }
        };

        // Signal injection
        private int _moveCount = 10;
        private float _optimalPercent = 0.5f;
        private float _interMoveInterval = 1.5f;
        private int _pauseCount = 0;

        // Batch simulation
        private int _simulateCount = 5;
        private float _simulateWinRate = 0.5f;
        private bool _autoApplyProposals = true;

        // Results
        private List<ProposalRecord> _proposalHistory = new List<ProposalRecord>();
        private SessionSummary _lastSessionSummary;
        private bool _hasSessionSummary;
        private Vector2 _leftScroll, _middleScroll, _rightScroll;

        // Compare mode
        private bool _compareMode;
        private DDAConfig _compareConfig;
        private DDAService _compareService;
        private List<ProposalRecord> _compareHistory = new List<ProposalRecord>();
        private Dictionary<string, float> _activeParams;
        private Dictionary<string, float> _compareActiveParams;

        // UI state
        private bool _showSessionSummary = true;
        private bool _showProposalHistory = true;

        [MenuItem("Cadence/Sandbox Dashboard")]
        public static void ShowWindow()
        {
            var window = GetWindow<SandboxDashboard>("Sandbox Dashboard");
            window.minSize = new Vector2(920, 620);
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawMiddlePanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        // ════════════════════════════════════════════════════════════
        //  TOOLBAR
        // ════════════════════════════════════════════════════════════

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Reset All", EditorStyles.toolbarButton, GUILayout.Width(65)))
            {
                ResetService();
                _proposalHistory.Clear();
                _compareHistory.Clear();
                _hasSessionSummary = false;
            }

            bool newCompareMode = GUILayout.Toggle(_compareMode, "Compare Mode",
                EditorStyles.toolbarButton, GUILayout.Width(100));
            if (newCompareMode != _compareMode)
            {
                _compareMode = newCompareMode;
                if (!_compareMode)
                {
                    _compareService = null;
                    _compareActiveParams = null;
                    _compareHistory.Clear();
                }
            }

            GUILayout.FlexibleSpace();

            if (_service != null)
            {
                string status;
                Color color;
                if (_service.IsSessionActive)
                {
                    status = "SESSION ACTIVE";
                    color = new Color(0.3f, 0.9f, 0.3f);
                }
                else if (_proposalHistory.Count > 0)
                {
                    status = $"IDLE — {_proposalHistory.Count} sessions completed";
                    color = new Color(0.7f, 0.85f, 1f);
                }
                else
                {
                    status = "READY — no sessions yet";
                    color = Color.gray;
                }

                var prev = GUI.color;
                GUI.color = color;
                GUILayout.Label(status, EditorStyles.toolbarButton);
                GUI.color = prev;
            }
            else
            {
                GUILayout.Label("NOT INITIALIZED", EditorStyles.toolbarButton);
            }

            EditorGUILayout.EndHorizontal();
        }

        // ════════════════════════════════════════════════════════════
        //  LEFT PANEL — Setup & Configuration
        // ════════════════════════════════════════════════════════════

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(270));
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

            // ── Step 1: Config ──
            DrawSectionHeader("1. DDA Config");
            EditorGUILayout.HelpBox(
                "Drag your DDAConfig asset here. This is the master configuration " +
                "that wires PlayerModel, FlowDetector, and AdjustmentEngine configs.",
                MessageType.None);

            EditorGUI.BeginChangeCheck();
            _ddaConfig = (DDAConfig)EditorGUILayout.ObjectField(
                "DDA Config", _ddaConfig, typeof(DDAConfig), false);
            if (EditorGUI.EndChangeCheck())
            {
                _service = null;
                _activeParams = null;
                _compareService = null;
                _compareActiveParams = null;
            }

            if (_compareMode)
            {
                EditorGUILayout.Space(2);
                EditorGUI.BeginChangeCheck();
                _compareConfig = (DDAConfig)EditorGUILayout.ObjectField(
                    "Compare Config", _compareConfig, typeof(DDAConfig), false);
                if (EditorGUI.EndChangeCheck())
                {
                    _compareService = null;
                    _compareActiveParams = null;
                    _compareHistory.Clear();
                }
            }

            // ── Step 2: Player Profile ──
            EditorGUILayout.Space(8);
            DrawSectionHeader("2. Player Profile");
            EditorGUILayout.HelpBox(
                "Simulated player starting state. Rating = skill estimate (1500 = average). " +
                "Deviation = uncertainty (350 = new player, <100 = confident). " +
                "Win Rate and Sessions seed the profile history.",
                MessageType.None);

            _initialRating = EditorGUILayout.FloatField("Rating", _initialRating);
            _initialDeviation = EditorGUILayout.Slider("Deviation", _initialDeviation, 30f, 350f);
            _initialWinRate = EditorGUILayout.Slider("Win Rate", _initialWinRate, 0f, 1f);
            _initialSessions = EditorGUILayout.IntSlider("Past Sessions", _initialSessions, 0, 50);

            // ── Step 3: Level Setup ──
            EditorGUILayout.Space(8);
            DrawSectionHeader("3. Level Setup");
            EditorGUILayout.HelpBox(
                "Level Type affects DDA strength (Boss = 0.3x, Tutorial = disabled). " +
                "Level Index feeds the sawtooth scheduler for periodic difficulty waves.",
                MessageType.None);

            _levelType = (LevelType)EditorGUILayout.EnumPopup("Level Type", _levelType);
            _levelIndex = EditorGUILayout.IntField("Level Index", _levelIndex);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Level Parameters", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Key-value difficulty parameters for the level. " +
                "The DDA proposes adjusted values after each session. " +
                "These are the parameters GetProposal() will modify.",
                MessageType.None);

            bool paramsChanged = false;
            for (int i = 0; i < _levelParams.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                var updated = new ParamEntry
                {
                    Key = EditorGUILayout.TextField(_levelParams[i].Key, GUILayout.Width(90)),
                    Value = EditorGUILayout.FloatField(_levelParams[i].Value)
                };
                if (_levelParams[i].Key != updated.Key ||
                    !Mathf.Approximately(_levelParams[i].Value, updated.Value))
                {
                    paramsChanged = true;
                }
                _levelParams[i] = updated;

                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    _levelParams.RemoveAt(i);
                    paramsChanged = true;
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Add Parameter"))
            {
                _levelParams.Add(new ParamEntry { Key = "param", Value = 1f });
                paramsChanged = true;
            }

            if (paramsChanged)
                ApplyParamEditsToActiveState();

            // ── Initialize Button ──
            EditorGUILayout.Space(8);
            var bgColor = GUI.backgroundColor;
            GUI.backgroundColor = _service == null ? new Color(0.5f, 1f, 0.5f) : Color.white;
            if (GUILayout.Button(_service == null ? "Initialize Service" : "Re-Initialize",
                GUILayout.Height(28)))
            {
                ResetService();
            }
            GUI.backgroundColor = bgColor;

            if (_service == null && _ddaConfig != null)
            {
                EditorGUILayout.HelpBox(
                    "Click 'Initialize Service' to create the DDA sandbox with your settings above.",
                    MessageType.Info);
            }
            else if (_ddaConfig == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a DDA Config asset first. Use Cadence > Setup Wizard to create one.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ════════════════════════════════════════════════════════════
        //  MIDDLE PANEL — Session Controls & Simulation
        // ════════════════════════════════════════════════════════════

        private void DrawMiddlePanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(310));
            _middleScroll = EditorGUILayout.BeginScrollView(_middleScroll);

            if (_service == null)
            {
                EditorGUILayout.HelpBox(
                    "Initialize the service first (left panel) to enable session controls.",
                    MessageType.Info);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            // ── Manual Session ──
            DrawSectionHeader("Manual Session");
            EditorGUILayout.HelpBox(
                "Run a single session step by step:\n" +
                "1) Begin Session → 2) Inject Moves → 3) End Win/Lose\n" +
                "After ending, a proposal is generated automatically.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_service.IsSessionActive;
            if (GUILayout.Button("Begin Session", GUILayout.Height(24)))
            {
                BeginSessionOnAllServices();
            }
            GUI.enabled = _service.IsSessionActive;
            var bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("End Win", GUILayout.Height(24)))
                EndAndPropose(SessionOutcome.Win);
            GUI.backgroundColor = new Color(1f, 0.6f, 0.5f);
            if (GUILayout.Button("End Lose", GUILayout.Height(24)))
                EndAndPropose(SessionOutcome.Lose);
            GUI.backgroundColor = bg;
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // ── Move Injection ──
            EditorGUILayout.Space(8);
            DrawSectionHeader("Inject Moves");
            EditorGUILayout.HelpBox(
                "Simulate a batch of player moves. Optimal % controls how many moves " +
                "are 'good' vs 'wasted'. Interval is seconds between moves. " +
                "Pauses add frustration signals.",
                MessageType.None);

            GUI.enabled = _service.IsSessionActive;
            _moveCount = EditorGUILayout.IntSlider("Move Count", _moveCount, 1, 100);
            _optimalPercent = EditorGUILayout.Slider("Optimal %", _optimalPercent, 0f, 1f);
            _interMoveInterval = EditorGUILayout.Slider("Move Interval (sec)", _interMoveInterval, 0.2f, 10f);
            _pauseCount = EditorGUILayout.IntSlider("Pauses", _pauseCount, 0, 10);
            if (GUILayout.Button("Inject Moves"))
                InjectMoves(_service, _moveCount, _optimalPercent);
            GUI.enabled = true;

            // ── Signal Buttons ──
            EditorGUILayout.Space(8);
            DrawSectionHeader("Quick Signals");
            EditorGUILayout.HelpBox(
                "Inject individual signals for precise testing. " +
                "Each button sends the correct signal with proper value and tier.",
                MessageType.None);

            GUI.enabled = _service.IsSessionActive;
            DrawSignalGrid();
            GUI.enabled = true;

            // ── Batch Simulation ──
            EditorGUILayout.Space(12);
            DrawSectionHeader("Batch Simulation");
            EditorGUILayout.HelpBox(
                "Simulate many sessions at once. Win Rate controls the fraction that " +
                "are wins vs losses. Auto-Apply feeds each proposal's adjusted values " +
                "into the next session (the real DDA feedback loop).",
                MessageType.None);

            GUI.enabled = !_service.IsSessionActive;
            _simulateCount = EditorGUILayout.IntSlider("Session Count", _simulateCount, 1, 50);
            _simulateWinRate = EditorGUILayout.Slider("Win Rate", _simulateWinRate, 0f, 1f);
            _autoApplyProposals = EditorGUILayout.Toggle("Auto-Apply Proposals", _autoApplyProposals);

            if (GUILayout.Button("Run Batch Simulation", GUILayout.Height(24)))
                RunBatchSimulation(_simulateCount, _simulateWinRate);
            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSignalGrid()
        {
            // Each entry: (label, key, value, tier)
            var signals = new[]
            {
                ("Move", SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality),
                ("Optimal", SignalKeys.MoveOptimal, 1f, SignalTier.DecisionQuality),
                ("Waste", SignalKeys.MoveWaste, 1f, SignalTier.DecisionQuality),
                ("Progress", SignalKeys.ProgressDelta, 0.1f, SignalTier.DecisionQuality),
                ("Interval", SignalKeys.InterMoveInterval, 1.5f, SignalTier.BehavioralTempo),
                ("Hesitation", SignalKeys.HesitationTime, 3f, SignalTier.BehavioralTempo),
                ("Pause", SignalKeys.PauseTriggered, 1f, SignalTier.BehavioralTempo),
                ("PowerUp", SignalKeys.PowerUpUsed, 1f, SignalTier.StrategicPattern),
                ("Sequence", SignalKeys.SequenceMatch, 1f, SignalTier.StrategicPattern),
            };

            int cols = 3;
            for (int i = 0; i < signals.Length; i++)
            {
                if (i % cols == 0) EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(signals[i].Item1, GUILayout.Height(28), GUILayout.Width(90)))
                {
                    _service.RecordSignal(signals[i].Item2, signals[i].Item3, signals[i].Item4);
                    _service.Tick(0.1f);
                    if (_compareMode && _compareService != null && _compareService.IsSessionActive)
                    {
                        _compareService.RecordSignal(signals[i].Item2, signals[i].Item3, signals[i].Item4);
                        _compareService.Tick(0.1f);
                    }
                    Repaint();
                }

                if (i % cols == cols - 1 || i == signals.Length - 1)
                    EditorGUILayout.EndHorizontal();
            }
        }

        // ════════════════════════════════════════════════════════════
        //  RIGHT PANEL — Live State & Results
        // ════════════════════════════════════════════════════════════

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

            if (_service == null)
            {
                EditorGUILayout.HelpBox(
                    "Initialize the service to see live DDA state here.\n\n" +
                    "This panel shows:\n" +
                    "• Real-time flow state (Flow / Boredom / Anxiety / Frustration)\n" +
                    "• Player archetype classification\n" +
                    "• Glicko-2 skill profile (rating, deviation, confidence)\n" +
                    "• Session analysis results\n" +
                    "• Adjustment proposals with parameter deltas",
                    MessageType.Info);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            // ── Flow State ──
            DrawSectionHeader("Flow State");
            var flow = _service.CurrentFlow;
            var flowColor = GetFlowColor(flow.State);
            EditorGUI.indentLevel++;
            var prev = GUI.color;
            GUI.color = flowColor;
            EditorGUILayout.LabelField("State", flow.State.ToString(), EditorStyles.boldLabel);
            GUI.color = prev;
            DrawProgressBar("Tempo", flow.TempoScore);
            DrawProgressBar("Efficiency", flow.EfficiencyScore);
            DrawProgressBar("Engagement", flow.EngagementScore);
            DrawProgressBar("Confidence", flow.Confidence);
            EditorGUI.indentLevel--;

            // ── Player Archetype ──
            EditorGUILayout.Space(4);
            DrawSectionHeader("Player Archetype");
            var archetype = _service.CurrentArchetype;
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Primary",
                $"{archetype.Primary} ({archetype.PrimaryConfidence:P0})");
            EditorGUILayout.LabelField("Secondary",
                $"{archetype.Secondary} ({archetype.SecondaryConfidence:P0})");
            EditorGUI.indentLevel--;

            // ── Player Profile ──
            EditorGUILayout.Space(4);
            DrawSectionHeader("Player Profile (Glicko-2)");
            var profile = _service.PlayerProfile;
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Rating",
                $"{profile.Rating:F0}  (1500 = average)");
            EditorGUILayout.LabelField("Deviation",
                $"{profile.Deviation:F1}  ({profile.Confidence01:P0} confident)");
            EditorGUILayout.LabelField("Sessions", profile.SessionsCompleted.ToString());
            DrawProgressBar("Win Rate", profile.AverageOutcome);
            DrawProgressBar("Avg Efficiency", profile.AverageEfficiency);
            EditorGUILayout.LabelField("Sufficient Data",
                profile.HasSufficientData ? "Yes (≥5 sessions)" : $"No ({profile.SessionsCompleted}/5)");
            EditorGUI.indentLevel--;

            // ── Session Summary ──
            if (_hasSessionSummary)
            {
                EditorGUILayout.Space(4);
                _showSessionSummary = EditorGUILayout.Foldout(_showSessionSummary,
                    "Last Session Summary", true, EditorStyles.foldoutHeader);
                if (_showSessionSummary)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("Outcome",
                        _lastSessionSummary.Outcome.ToString());
                    EditorGUILayout.LabelField("Moves / Duration",
                        $"{_lastSessionSummary.TotalMoves} moves, {_lastSessionSummary.Duration:F1}s");
                    DrawProgressBar("Move Efficiency", _lastSessionSummary.MoveEfficiency);
                    DrawProgressBar("Waste Ratio", _lastSessionSummary.WasteRatio);
                    DrawProgressBar("Progress Rate", _lastSessionSummary.ProgressRate);
                    EditorGUILayout.LabelField("Mean Interval",
                        $"{_lastSessionSummary.MeanInterMoveInterval:F2}s (var: {_lastSessionSummary.InterMoveVariance:F3})");
                    EditorGUILayout.LabelField("Pauses", _lastSessionSummary.PauseCount.ToString());
                    EditorGUILayout.LabelField("Power-Ups", _lastSessionSummary.PowerUpsUsed.ToString());

                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("Derived Scores", EditorStyles.miniLabel);
                    DrawProgressBar("Skill Score", _lastSessionSummary.SkillScore);
                    DrawProgressBar("Engagement", _lastSessionSummary.EngagementScore);
                    DrawColoredProgressBar("Frustration",
                        _lastSessionSummary.FrustrationScore,
                        _lastSessionSummary.FrustrationScore > 0.7f
                            ? new Color(1f, 0.3f, 0.2f) : Color.white);
                    EditorGUI.indentLevel--;
                }
            }

            // ── Last Proposal ──
            if (_proposalHistory.Count > 0)
            {
                EditorGUILayout.Space(4);
                DrawSectionHeader("Last Proposal");
                var last = _proposalHistory[_proposalHistory.Count - 1];
                EditorGUI.indentLevel++;

                if (!string.IsNullOrEmpty(last.Reason))
                    EditorGUILayout.HelpBox(last.Reason, MessageType.None);

                EditorGUILayout.LabelField("Flow State", last.FlowState.ToString());
                EditorGUILayout.LabelField("Confidence", last.Confidence.ToString("F2"));
                EditorGUILayout.LabelField("Timing", last.Timing.ToString());

                if (last.Deltas.Count > 0)
                {
                    EditorGUILayout.Space(2);
                    foreach (var d in last.Deltas)
                    {
                        string dir = d.Delta > 0 ? "harder" : "easier";
                        EditorGUILayout.LabelField(
                            $"  {d.Key}",
                            $"{d.From:F1} → {d.To:F1} ({d.Delta:+0.0;-0.0} {dir}) [{d.Rule}]");
                    }

                    // Apply button
                    if (!_service.IsSessionActive)
                    {
                        EditorGUILayout.Space(2);
                        if (GUILayout.Button("Apply Proposal to Level Params"))
                            ApplyLastProposal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("  (no parameter changes proposed)");
                }
                EditorGUI.indentLevel--;
            }

            // ── Proposal History ──
            if (_proposalHistory.Count > 1)
            {
                EditorGUILayout.Space(4);
                _showProposalHistory = EditorGUILayout.Foldout(_showProposalHistory,
                    $"Proposal History ({_proposalHistory.Count})", true, EditorStyles.foldoutHeader);
                if (_showProposalHistory)
                {
                    int start = Mathf.Max(0, _proposalHistory.Count - 15);
                    for (int i = _proposalHistory.Count - 1; i >= start; i--)
                    {
                        var rec = _proposalHistory[i];
                        string deltaStr = rec.Deltas.Count > 0
                            ? $"Δ={SumDeltas(rec):+0.0;-0.0}"
                            : "no change";
                        EditorGUILayout.LabelField(
                            $"  #{i + 1} [{rec.Outcome}]",
                            $"Flow={rec.FlowState}  {deltaStr}  Conf={rec.Confidence:F2}");
                    }
                }
            }

            // ── Compare Results ──
            if (_compareMode && _compareHistory.Count > 0 && _proposalHistory.Count > 0)
            {
                EditorGUILayout.Space(8);
                DrawSectionHeader("Compare: Config A vs Config B");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Config A", EditorStyles.miniLabel,
                    GUILayout.Width(120));
                EditorGUILayout.LabelField("Config B", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                int count = Mathf.Min(_proposalHistory.Count, _compareHistory.Count);
                int show = Mathf.Min(count, 15);
                for (int i = count - show; i < count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(
                        $"#{i + 1} Δ={SumDeltas(_proposalHistory[i]):+0.0;-0.0}",
                        GUILayout.Width(120));
                    EditorGUILayout.LabelField(
                        $"#{i + 1} Δ={SumDeltas(_compareHistory[i]):+0.0;-0.0}");
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ════════════════════════════════════════════════════════════
        //  LOGIC
        // ════════════════════════════════════════════════════════════

        private void ResetService()
        {
            if (_ddaConfig == null)
            {
                Debug.LogWarning("[Cadence Sandbox] Assign a DDAConfig first.");
                return;
            }

            _service = new DDAService(_ddaConfig);
            _proposalHistory.Clear();
            _hasSessionSummary = false;
            _activeParams = BuildParamDict();

            // Apply custom player profile
            InjectPlayerProfile(_service, _initialRating, _initialDeviation,
                _initialWinRate, _initialSessions);

            if (_compareMode && _compareConfig != null)
            {
                _compareService = new DDAService(_compareConfig);
                _compareHistory.Clear();
                _compareActiveParams = new Dictionary<string, float>(_activeParams);
                InjectPlayerProfile(_compareService, _initialRating, _initialDeviation,
                    _initialWinRate, _initialSessions);
            }
            else
            {
                _compareService = null;
                _compareActiveParams = null;
                _compareHistory.Clear();
            }

            Repaint();
        }

        private void InjectPlayerProfile(DDAService service, float rating, float deviation,
            float winRate, int sessions)
        {
            // Get the default profile, modify it, and load it back
            string json = service.SaveProfile();
            var profile = JsonUtility.FromJson<PlayerSkillProfile>(json);

            profile.Rating = rating;
            profile.Deviation = deviation;
            profile.AverageOutcome = winRate;
            profile.AverageEfficiency = winRate * 0.8f; // rough correlation
            profile.SessionsCompleted = sessions;

            // Seed history entries to match declared sessions/win rate
            profile.RecentHistory.Clear();
            long now = System.DateTime.UtcNow.Ticks;
            for (int i = 0; i < sessions && i < PlayerSkillProfile.MaxHistoryEntries; i++)
            {
                bool isWin = (float)i / Mathf.Max(1, sessions) < winRate;
                profile.RecentHistory.Add(new SessionHistoryEntry
                {
                    Efficiency = isWin ? 0.6f + Random.value * 0.3f : 0.2f + Random.value * 0.3f,
                    Outcome = isWin ? 1f : 0f,
                    Duration = 30f + Random.value * 60f,
                    Moves = 10 + (int)(Random.value * 20),
                    TimestampUtcTicks = now - (long)((sessions - i) * System.TimeSpan.TicksPerHour),
                    LevelTypeByte = (byte)_levelType
                });
            }

            if (sessions > 0)
                profile.LastSessionUtcTicks = now - System.TimeSpan.TicksPerHour;

            string modifiedJson = JsonUtility.ToJson(profile);
            service.LoadProfile(modifiedJson);
        }

        private void BeginSessionOnAllServices()
        {
            if (_service == null) return;
            if (_activeParams == null || _activeParams.Count == 0)
                _activeParams = BuildParamDict();

            EnsureCompareServiceReady();

            _service.BeginSession("sandbox_level",
                new Dictionary<string, float>(_activeParams), _levelType);

            if (_compareMode && _compareService != null)
            {
                if (_compareActiveParams == null || _compareActiveParams.Count == 0)
                    _compareActiveParams = new Dictionary<string, float>(_activeParams);

                _compareService.BeginSession("sandbox_level",
                    new Dictionary<string, float>(_compareActiveParams), _levelType);
            }

            Repaint();
        }

        private void EndAndPropose(SessionOutcome outcome)
        {
            if (_service == null || !_service.IsSessionActive) return;

            _service.EndSession(outcome);

            // Capture session summary from debug snapshot
            var debug = _service.GetDebugSnapshot();
            _lastSessionSummary = debug.LastSessionSummary;
            _hasSessionSummary = true;

            if (_activeParams == null || _activeParams.Count == 0)
                _activeParams = BuildParamDict();

            var proposal = _service.GetProposal(
                new Dictionary<string, float>(_activeParams), _levelType, _levelIndex);
            RecordProposal(proposal, outcome, _proposalHistory);

            if (_compareMode && _compareService != null)
            {
                if (_compareActiveParams == null || _compareActiveParams.Count == 0)
                    _compareActiveParams = new Dictionary<string, float>(_activeParams);

                _compareService.EndSession(outcome);
                var cProposal = _compareService.GetProposal(
                    new Dictionary<string, float>(_compareActiveParams),
                    _levelType,
                    _levelIndex);
                RecordProposal(cProposal, outcome, _compareHistory);
            }

            _levelIndex++;
            Repaint();
        }

        private void InjectMoves(DDAService service, int count, float optimalPct)
        {
            if (service == null || !service.IsSessionActive) return;

            int optimalCount = Mathf.RoundToInt(count * optimalPct);
            for (int i = 0; i < count; i++)
            {
                service.RecordSignal(SignalKeys.MoveExecuted, 1f,
                    SignalTier.DecisionQuality, i + 1);

                if (i < optimalCount)
                {
                    service.RecordSignal(SignalKeys.MoveOptimal, 1f,
                        SignalTier.DecisionQuality, i + 1);
                    service.RecordSignal(SignalKeys.ProgressDelta, 0.05f,
                        SignalTier.DecisionQuality, i + 1);
                }
                else
                {
                    service.RecordSignal(SignalKeys.MoveWaste, 1f,
                        SignalTier.DecisionQuality, i + 1);
                }

                service.RecordSignal(SignalKeys.InterMoveInterval, _interMoveInterval,
                    SignalTier.BehavioralTempo, i + 1);
                service.Tick(_interMoveInterval);
            }

            // Inject pauses
            for (int p = 0; p < _pauseCount; p++)
            {
                service.RecordSignal(SignalKeys.PauseTriggered, 1f,
                    SignalTier.BehavioralTempo);
                service.Tick(2f);
            }
        }

        private void RunBatchSimulation(int count, float winRate)
        {
            if (_service == null) return;
            if (_service.IsSessionActive)
            {
                Debug.LogWarning("[Cadence Sandbox] End the current session before running batch simulation.");
                return;
            }

            EnsureCompareServiceReady();

            var stateA = _activeParams != null && _activeParams.Count > 0
                ? new Dictionary<string, float>(_activeParams)
                : BuildParamDict();

            Dictionary<string, float> stateB = null;
            if (_compareMode && _compareService != null)
            {
                stateB = _compareActiveParams != null && _compareActiveParams.Count > 0
                    ? new Dictionary<string, float>(_compareActiveParams)
                    : new Dictionary<string, float>(stateA);
            }

            for (int i = 0; i < count; i++)
            {
                bool isWin = Random.value < winRate;
                float sessionOptimal = isWin ? 0.6f + Random.value * 0.3f : 0.1f + Random.value * 0.3f;

                // Begin session on all services
                _service.BeginSession($"sim_{i}",
                    new Dictionary<string, float>(stateA), _levelType);
                if (stateB != null)
                    _compareService.BeginSession($"sim_{i}",
                        new Dictionary<string, float>(stateB), _levelType);

                // Inject moves
                InjectMoves(_service, 15, sessionOptimal);
                if (_compareMode && _compareService != null && _compareService.IsSessionActive)
                    InjectMoves(_compareService, 15, sessionOptimal);

                // End and propose
                var outcome = isWin ? SessionOutcome.Win : SessionOutcome.Lose;
                _service.EndSession(outcome);

                var debug = _service.GetDebugSnapshot();
                _lastSessionSummary = debug.LastSessionSummary;
                _hasSessionSummary = true;

                var proposal = _service.GetProposal(
                    new Dictionary<string, float>(stateA), _levelType, _levelIndex);
                RecordProposal(proposal, outcome, _proposalHistory);

                if (stateB != null)
                {
                    _compareService.EndSession(outcome);
                    var cProposal = _compareService.GetProposal(
                        new Dictionary<string, float>(stateB), _levelType, _levelIndex);
                    RecordProposal(cProposal, outcome, _compareHistory);

                    if (_autoApplyProposals && cProposal != null && cProposal.Deltas != null)
                        ApplyProposalToDictionary(cProposal, stateB);
                }

                // Auto-apply proposals to feed back into next session
                if (_autoApplyProposals && proposal != null && proposal.Deltas != null)
                    ApplyProposalToDictionary(proposal, stateA);

                _levelIndex++;
            }

            _activeParams = stateA;
            if (stateB != null)
                _compareActiveParams = stateB;

            SyncParamEntriesFromDictionary(_activeParams);
            Repaint();
        }

        private void ApplyLastProposal()
        {
            if (_proposalHistory.Count == 0) return;
            var last = _proposalHistory[_proposalHistory.Count - 1];
            if (_activeParams == null || _activeParams.Count == 0)
                _activeParams = BuildParamDict();

            foreach (var d in last.Deltas)
                _activeParams[d.Key] = d.To;

            SyncParamEntriesFromDictionary(_activeParams);
        }

        private void ApplyParamEditsToActiveState()
        {
            var editedParams = BuildParamDict();
            _activeParams = new Dictionary<string, float>(editedParams);

            // Manual UI edits should become the shared baseline for compare mode.
            if (_compareMode)
                _compareActiveParams = new Dictionary<string, float>(editedParams);
        }

        private void EnsureCompareServiceReady()
        {
            if (!_compareMode || _compareConfig == null || _compareService != null)
                return;

            _compareService = new DDAService(_compareConfig);
            _compareHistory.Clear();

            // If the main service already has state, clone it so A/B start from the same profile.
            if (_service != null)
            {
                _compareService.LoadProfile(_service.SaveProfile());
            }
            else
            {
                InjectPlayerProfile(_compareService, _initialRating, _initialDeviation,
                    _initialWinRate, _initialSessions);
            }

            if (_compareActiveParams == null || _compareActiveParams.Count == 0)
            {
                if (_activeParams == null || _activeParams.Count == 0)
                    _activeParams = BuildParamDict();

                _compareActiveParams = new Dictionary<string, float>(_activeParams);
            }
        }

        private static void ApplyProposalToDictionary(AdjustmentProposal proposal,
            Dictionary<string, float> targetParams)
        {
            if (proposal?.Deltas == null || targetParams == null) return;
            foreach (var d in proposal.Deltas)
            {
                targetParams[d.ParameterKey] = d.ProposedValue;
            }
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

        private void SyncParamEntriesFromDictionary(Dictionary<string, float> source)
        {
            if (source == null) return;

            for (int i = 0; i < _levelParams.Count; i++)
            {
                if (source.TryGetValue(_levelParams[i].Key, out float value))
                {
                    _levelParams[i] = new ParamEntry
                    {
                        Key = _levelParams[i].Key,
                        Value = value
                    };
                }
            }

            foreach (var kvp in source)
            {
                bool found = false;
                for (int i = 0; i < _levelParams.Count; i++)
                {
                    if (_levelParams[i].Key == kvp.Key)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _levelParams.Add(new ParamEntry
                    {
                        Key = kvp.Key,
                        Value = kvp.Value
                    });
                }
            }
        }

        private static void RecordProposal(AdjustmentProposal proposal, SessionOutcome outcome,
            List<ProposalRecord> history)
        {
            var rec = new ProposalRecord { Outcome = outcome };
            if (proposal != null)
            {
                rec.Confidence = proposal.Confidence;
                rec.FlowState = proposal.DetectedState;
                rec.Reason = proposal.Reason;
                rec.Timing = proposal.Timing;
                if (proposal.Deltas != null)
                {
                    foreach (var d in proposal.Deltas)
                    {
                        rec.Deltas.Add(new DeltaRecord
                        {
                            Key = d.ParameterKey,
                            From = d.CurrentValue,
                            To = d.ProposedValue,
                            Delta = d.ProposedValue - d.CurrentValue,
                            Rule = d.RuleName
                        });
                    }
                }
            }
            history.Add(rec);
        }

        // ════════════════════════════════════════════════════════════
        //  UI HELPERS
        // ════════════════════════════════════════════════════════════

        private static void DrawSectionHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private static void DrawProgressBar(string label, float value)
        {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            float labelWidth = EditorGUIUtility.labelWidth;
            var labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            var barRect = new Rect(rect.x + labelWidth + 2, rect.y, rect.width - labelWidth - 2, rect.height);

            EditorGUI.LabelField(labelRect, label);
            EditorGUI.ProgressBar(barRect, Mathf.Clamp01(value), $"{value:F2}");
        }

        private static void DrawColoredProgressBar(string label, float value, Color color)
        {
            var prev = GUI.color;
            GUI.color = color;
            DrawProgressBar(label, value);
            GUI.color = prev;
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

        // ════════════════════════════════════════════════════════════
        //  DATA TYPES
        // ════════════════════════════════════════════════════════════

        private struct ParamEntry
        {
            public string Key;
            public float Value;
        }

        private class ProposalRecord
        {
            public float Confidence;
            public FlowState FlowState;
            public SessionOutcome Outcome;
            public string Reason;
            public AdjustmentTiming Timing;
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
