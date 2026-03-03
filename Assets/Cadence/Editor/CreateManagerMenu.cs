using UnityEditor;
using UnityEngine;
using Cadence;

namespace Cadence.Editor
{
    public static class CreateManagerMenu
    {
        [MenuItem("Cadence/Create Manager in Scene", false, 50)]
        private static void CreateManager()
        {
            // Check for existing instance in the scene
            var existing = Object.FindObjectOfType<CadenceManager>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log("[Cadence] CadenceManager already exists in the scene. Selected it.");
                return;
            }

            var go = new GameObject("CadenceManager");
            var manager = go.AddComponent<CadenceManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create CadenceManager");

            // Auto-find DDAConfig
            string[] guids = AssetDatabase.FindAssets("t:DDAConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var config = AssetDatabase.LoadAssetAtPath<DDAConfig>(path);
                if (config != null)
                {
                    var so = new SerializedObject(manager);
                    var configProp = so.FindProperty("_config");
                    configProp.objectReferenceValue = config;
                    so.ApplyModifiedProperties();
                    Debug.Log($"[Cadence] Auto-assigned DDAConfig from {path}");
                }
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Cadence — No DDAConfig Found",
                    "No DDAConfig asset was found in the project.\n\n" +
                    "Use Cadence > Setup Wizard to create and configure one.",
                    "OK");
            }

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }

        [MenuItem("Cadence/Create Manager in Scene", true)]
        private static bool ValidateCreateManager()
        {
            return !Application.isPlaying;
        }
    }
}
