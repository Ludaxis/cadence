using UnityEditor;
using UnityEngine;
using Cadence;

namespace Cadence.Editor
{
    /// <summary>
    /// Scene overlay that shows the current flow state as a colored badge
    /// in the top-left corner of the Scene view during Play mode.
    /// </summary>
    [InitializeOnLoad]
    public static class FlowStateVisualizer
    {
        private static bool _enabled = true;
        private static IDDAService _service;

        static FlowStateVisualizer()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public static void SetService(IDDAService service)
        {
            _service = service;
        }

        public static bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            // Auto-discovery via CadenceManager
            if (_service == null && CadenceManager.Instance != null)
                _service = CadenceManager.Service;

            if (!_enabled || !Application.isPlaying || _service == null) return;

            var flow = _service.CurrentFlow;

            Handles.BeginGUI();

            float x = 10f;
            float y = 10f;
            float width = 200f;
            float height = 80f;

            // Background
            Color bgColor = GetFlowColor(flow.State);
            bgColor.a = 0.8f;
            EditorGUI.DrawRect(new Rect(x, y, width, height), bgColor);

            // Border
            EditorGUI.DrawRect(new Rect(x, y, width, 2), Color.black);
            EditorGUI.DrawRect(new Rect(x, y + height - 2, width, 2), Color.black);
            EditorGUI.DrawRect(new Rect(x, y, 2, height), Color.black);
            EditorGUI.DrawRect(new Rect(x + width - 2, y, 2, height), Color.black);

            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            var smallStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(x, y + 5, width, 25), $"Flow: {flow.State}", style);
            GUI.Label(new Rect(x, y + 30, width, 18),
                $"Tempo: {flow.TempoScore:F2}  Eff: {flow.EfficiencyScore:F2}", smallStyle);
            GUI.Label(new Rect(x, y + 48, width, 18),
                $"Engage: {flow.EngagementScore:F2}  Conf: {flow.Confidence:F2}", smallStyle);

            Handles.EndGUI();
        }

        private static Color GetFlowColor(FlowState state)
        {
            switch (state)
            {
                case FlowState.Boredom:     return new Color(0.2f, 0.4f, 0.8f);
                case FlowState.Flow:        return new Color(0.1f, 0.7f, 0.2f);
                case FlowState.Anxiety:     return new Color(0.9f, 0.7f, 0.1f);
                case FlowState.Frustration: return new Color(0.9f, 0.2f, 0.1f);
                default:                    return new Color(0.4f, 0.4f, 0.4f);
            }
        }
    }
}
