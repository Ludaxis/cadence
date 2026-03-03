using UnityEditor;
using UnityEngine;
using System.IO;
using Cadence;

namespace Cadence.Editor
{
    public class SetupWizard : EditorWindow
    {
        private string _targetFolder = "Assets/Config/Cadence";
        private Vector2 _scrollPos;

        // Cached status
        private bool _hasDDAConfig;
        private bool _hasPlayerModelConfig;
        private bool _hasFlowDetectorConfig;
        private bool _hasAdjustmentEngineConfig;
        private bool _hasSawtoothConfig;

        private DDAConfig _ddaConfig;
        private PlayerModelConfig _playerModelConfig;
        private FlowDetectorConfig _flowDetectorConfig;
        private AdjustmentEngineConfig _adjustmentEngineConfig;
        private SawtoothCurveConfig _sawtoothConfig;

        [MenuItem("Cadence/Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<SetupWizard>("Cadence Setup Wizard");
        }

        private void OnEnable()
        {
            RefreshStatus();
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("Cadence DDA — Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Folder picker
            EditorGUILayout.BeginHorizontal();
            _targetFolder = EditorGUILayout.TextField("Config Folder", _targetFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Config Folder",
                    "Assets", "Config");
                if (!string.IsNullOrEmpty(selected))
                {
                    // Convert absolute path to project-relative
                    if (selected.StartsWith(Application.dataPath))
                        selected = "Assets" + selected.Substring(Application.dataPath.Length);
                    _targetFolder = selected;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // Status panel
            EditorGUILayout.LabelField("Configuration Status", EditorStyles.boldLabel);
            DrawStatusRow("DDA Config", _hasDDAConfig, _ddaConfig);
            DrawStatusRow("Player Model Config", _hasPlayerModelConfig, _playerModelConfig);
            DrawStatusRow("Flow Detector Config", _hasFlowDetectorConfig, _flowDetectorConfig);
            DrawStatusRow("Adjustment Engine Config", _hasAdjustmentEngineConfig,
                _adjustmentEngineConfig);
            DrawStatusRow("Sawtooth Curve Config (Optional)", _hasSawtoothConfig,
                _sawtoothConfig);
            EditorGUILayout.Space();

            // Validation
            if (_hasDDAConfig && _ddaConfig != null)
            {
                bool wired = _ddaConfig.PlayerModelConfig != null &&
                             _ddaConfig.FlowDetectorConfig != null &&
                             _ddaConfig.AdjustmentEngineConfig != null;
                if (wired)
                {
                    EditorGUILayout.HelpBox("All configs created and wired.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "DDA Config exists but sub-configs are not fully wired. Click 'Wire References' to fix.",
                        MessageType.Warning);
                    if (GUILayout.Button("Wire References"))
                    {
                        WireReferences();
                    }
                }
            }

            EditorGUILayout.Space();

            // Actions
            bool allExist = _hasDDAConfig && _hasPlayerModelConfig &&
                            _hasFlowDetectorConfig && _hasAdjustmentEngineConfig;

            GUI.enabled = !allExist;
            if (GUILayout.Button("Create All", GUILayout.Height(30)))
            {
                CreateAll();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Refresh Status"))
            {
                RefreshStatus();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawStatusRow(string label, bool exists, Object asset)
        {
            EditorGUILayout.BeginHorizontal();
            var icon = exists
                ? EditorGUIUtility.IconContent("TestPassed")
                : EditorGUIUtility.IconContent("TestFailed");
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(18));
            EditorGUILayout.LabelField(label, exists ? "Found" : "Missing");
            if (exists && asset != null)
            {
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                    Selection.activeObject = asset;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshStatus()
        {
            // Search project for existing configs
            _playerModelConfig = FindAsset<PlayerModelConfig>();
            _flowDetectorConfig = FindAsset<FlowDetectorConfig>();
            _adjustmentEngineConfig = FindAsset<AdjustmentEngineConfig>();
            _ddaConfig = FindAsset<DDAConfig>();
            _sawtoothConfig = FindAsset<SawtoothCurveConfig>();

            _hasPlayerModelConfig = _playerModelConfig != null;
            _hasFlowDetectorConfig = _flowDetectorConfig != null;
            _hasAdjustmentEngineConfig = _adjustmentEngineConfig != null;
            _hasDDAConfig = _ddaConfig != null;
            _hasSawtoothConfig = _sawtoothConfig != null;
        }

        private void CreateAll()
        {
            EnsureDirectory(_targetFolder);

            if (!_hasPlayerModelConfig)
            {
                _playerModelConfig = CreateAssetIfMissing<PlayerModelConfig>(
                    "PlayerModelConfig");
                _hasPlayerModelConfig = true;
            }

            if (!_hasFlowDetectorConfig)
            {
                _flowDetectorConfig = CreateAssetIfMissing<FlowDetectorConfig>(
                    "FlowDetectorConfig");
                _hasFlowDetectorConfig = true;
            }

            if (!_hasAdjustmentEngineConfig)
            {
                _adjustmentEngineConfig = CreateAssetIfMissing<AdjustmentEngineConfig>(
                    "AdjustmentEngineConfig");
                _hasAdjustmentEngineConfig = true;
            }

            if (!_hasSawtoothConfig)
            {
                _sawtoothConfig = CreateAssetIfMissing<SawtoothCurveConfig>(
                    "SawtoothCurveConfig");
                _hasSawtoothConfig = true;
            }

            if (!_hasDDAConfig)
            {
                _ddaConfig = CreateAssetIfMissing<DDAConfig>("DDAConfig");
                _hasDDAConfig = true;
            }

            WireReferences();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Cadence] Setup Wizard: All configs created and wired.");
        }

        private void WireReferences()
        {
            if (_ddaConfig == null) return;

            var so = new SerializedObject(_ddaConfig);
            SetReference(so, "PlayerModelConfig", _playerModelConfig);
            SetReference(so, "FlowDetectorConfig", _flowDetectorConfig);
            SetReference(so, "AdjustmentEngineConfig", _adjustmentEngineConfig);
            SetReference(so, "SawtoothCurveConfig", _sawtoothConfig);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_ddaConfig);
        }

        private T CreateAssetIfMissing<T>(string name) where T : ScriptableObject
        {
            var existing = FindAsset<T>();
            if (existing != null) return existing;

            var instance = CreateInstance<T>();
            string path = $"{_targetFolder}/{name}.asset";
            AssetDatabase.CreateAsset(instance, path);
            return instance;
        }

        private static T FindAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0) return null;
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static void SetReference(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop != null)
                prop.objectReferenceValue = value;
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path);
            string folderName = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureDirectory(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
