using System.IO;
using UnityEditor;
using UnityEngine;
using Cadence;

namespace Cadence.Editor
{
    public class SignalReplayWindow : EditorWindow
    {
        private SignalReplay _replay;
        private SignalBatch _loadedBatch;
        private Vector2 _scrollPos;
        private string _selectedFile;
        private string[] _availableFiles;
        private int _selectedFileIndex;
        private float _playbackSpeed = 1f;

        // Replay analysis
        private ISessionAnalyzer _analyzer;
        private SessionSummary _summary;
        private IFlowDetector _flowDetector;
        private SignalRingBuffer _replayBuffer;

        [MenuItem("Cadence/Signal Replay")]
        public static void ShowWindow()
        {
            GetWindow<SignalReplayWindow>("Signal Replay");
        }

        private void OnEnable()
        {
            _replay = new SignalReplay();
            _analyzer = new SessionAnalyzer();
            RefreshFileList();
        }

        private void Update()
        {
            if (_replay != null && _replay.IsPlaying)
            {
                _replay.Tick(0.016f); // Simulate ~60fps
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Signal Replay", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawFileSelector();
            EditorGUILayout.Space();

            if (_loadedBatch != null)
            {
                DrawPlaybackControls();
                EditorGUILayout.Space();
                DrawBatchInfo();
                EditorGUILayout.Space();
                DrawSignalList();
            }
        }

        private void DrawFileSelector()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Files", GUILayout.Width(100)))
                RefreshFileList();

            if (_availableFiles != null && _availableFiles.Length > 0)
            {
                int newIndex = EditorGUILayout.Popup(_selectedFileIndex, _availableFiles);
                if (newIndex != _selectedFileIndex)
                {
                    _selectedFileIndex = newIndex;
                    LoadFile(_availableFiles[newIndex]);
                }
            }
            else
            {
                EditorGUILayout.LabelField("No signal files found");
            }
            EditorGUILayout.EndHorizontal();

            // Manual file load
            if (GUILayout.Button("Load From File..."))
            {
                string path = EditorUtility.OpenFilePanel("Load Signal Batch", "", "json");
                if (!string.IsNullOrEmpty(path))
                    LoadFromPath(path);
            }
        }

        private void DrawPlaybackControls()
        {
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (!_replay.IsPlaying)
            {
                if (GUILayout.Button("Play")) _replay.Play(_playbackSpeed);
            }
            else
            {
                if (GUILayout.Button("Pause")) _replay.Pause();
            }
            if (GUILayout.Button("Stop")) _replay.Stop();
            EditorGUILayout.EndHorizontal();

            _playbackSpeed = EditorGUILayout.Slider("Speed", _playbackSpeed, 0.1f, 10f);

            float progress = _replay.TotalDuration > 0
                ? _replay.PlaybackTime / _replay.TotalDuration
                : 0f;
            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(false, 20),
                progress,
                $"{_replay.PlaybackTime:F1}s / {_replay.TotalDuration:F1}s  " +
                $"({_replay.CurrentIndex}/{_replay.TotalEntries})"
            );
        }

        private void DrawBatchInfo()
        {
            EditorGUILayout.LabelField("Session Summary", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Level", _loadedBatch.LevelId ?? "-");
            EditorGUILayout.IntField("Signals", _loadedBatch.Count);
            EditorGUILayout.EnumPopup("Outcome", _summary.Outcome);
            EditorGUILayout.FloatField("Duration", _summary.Duration);
            EditorGUILayout.IntField("Moves", _summary.TotalMoves);
            EditorGUILayout.Slider("Efficiency", _summary.MoveEfficiency, 0f, 1f);
            EditorGUILayout.Slider("Skill", _summary.SkillScore, 0f, 1f);
            EditorGUILayout.Slider("Engagement", _summary.EngagementScore, 0f, 1f);
            EditorGUILayout.Slider("Frustration", _summary.FrustrationScore, 0f, 1f);
            EditorGUI.indentLevel--;
        }

        private void DrawSignalList()
        {
            EditorGUILayout.LabelField("Signals", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(300));
            var entries = _loadedBatch.Entries;

            // Show around current index for context
            int start = Mathf.Max(0, _replay.CurrentIndex - 5);
            int end = Mathf.Min(entries.Count, start + 30);

            for (int i = start; i < end; i++)
            {
                var e = entries[i];
                bool isCurrent = i == _replay.CurrentIndex;

                if (isCurrent)
                {
                    var prev = GUI.color;
                    GUI.color = Color.yellow;
                    EditorGUILayout.LabelField(
                        $"[{e.Timestamp.SessionTime:F2}s] {e.Key} = {e.Value:F2} (T{(int)e.Tier} M{e.MoveIndex})");
                    GUI.color = prev;
                }
                else
                {
                    EditorGUILayout.LabelField(
                        $"[{e.Timestamp.SessionTime:F2}s] {e.Key} = {e.Value:F2} (T{(int)e.Tier} M{e.MoveIndex})");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void RefreshFileList()
        {
            string dir = Path.Combine(Application.persistentDataPath, "Cadence", "Signals");
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir, "*.json");
                _availableFiles = new string[files.Length];
                for (int i = 0; i < files.Length; i++)
                    _availableFiles[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
            else
            {
                _availableFiles = new string[0];
            }
        }

        private void LoadFile(string fileName)
        {
            string dir = Path.Combine(Application.persistentDataPath, "Cadence", "Signals");
            string path = Path.Combine(dir, fileName + ".json");
            LoadFromPath(path);
        }

        private void LoadFromPath(string path)
        {
            if (!File.Exists(path)) return;
            string json = File.ReadAllText(path);
            _loadedBatch = SignalLogSerializer.Deserialize(json);
            _replay.Load(_loadedBatch);
            _summary = _analyzer.Analyze(_loadedBatch);
        }
    }
}
