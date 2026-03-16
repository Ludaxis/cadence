#if !ODIN_INSPECTOR
using UnityEditor;
using UnityEngine;
using Cadence;

namespace Cadence.Editor
{
    [CustomEditor(typeof(CadenceManager))]
    public class CadenceManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty _config;
        private SerializedProperty _autoSaveProfile;
        private SerializedProperty _profileKey;
        private SerializedProperty _enableFileStorage;
        private SerializedProperty _verboseLogging;

        private void OnEnable()
        {
            _config = serializedObject.FindProperty("_config");
            _autoSaveProfile = serializedObject.FindProperty("_autoSaveProfile");
            _profileKey = serializedObject.FindProperty("_profileKey");
            _enableFileStorage = serializedObject.FindProperty("_enableFileStorage");
            _verboseLogging = serializedObject.FindProperty("_verboseLogging");
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Configuration
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            if (_config.objectReferenceValue == null)
                EditorGUILayout.HelpBox(
                    "A DDAConfig asset is required. Use Cadence > Setup Wizard to create one.",
                    MessageType.Error);
            EditorGUILayout.PropertyField(_config);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // Persistence
            EditorGUILayout.LabelField("Persistence", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_autoSaveProfile, new GUIContent("Auto-Save Profile"));
            if (_autoSaveProfile.boolValue)
                EditorGUILayout.PropertyField(_profileKey, new GUIContent("Profile Key"));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // Signal Storage
            EditorGUILayout.LabelField("Signal Storage", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_enableFileStorage, new GUIContent("Enable File Storage"));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // Debug
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_verboseLogging, new GUIContent("Verbose Logging"));
            EditorGUI.indentLevel--;

            // Runtime info (Play mode only)
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                var manager = (CadenceManager)target;
                EditorGUILayout.Toggle("Initialized", manager.IsInitialized);

                var service = CadenceManager.Service;
                if (service != null)
                {
                    EditorGUILayout.Toggle("Session Active", service.IsSessionActive);
                    var profile = service.PlayerProfile;
                    if (profile != null)
                    {
                        EditorGUILayout.FloatField("Player Rating", profile.Rating);
                        EditorGUILayout.IntField("Sessions", profile.SessionsCompleted);
                    }
                }

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
