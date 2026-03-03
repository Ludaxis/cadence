using UnityEditor;
using Cadence;

namespace Cadence.Editor
{
    /// <summary>
    /// Bridges CadenceManager (Runtime) to editor windows without the
    /// Runtime assembly referencing the Editor assembly.
    /// </summary>
    [InitializeOnLoad]
    internal static class CadenceEditorBridge
    {
        static CadenceEditorBridge()
        {
            CadenceManager.OnServiceInitialized += OnServiceReady;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnServiceReady(IDDAService service)
        {
            DDADebugWindow.SetService(service);
            FlowStateVisualizer.SetService(service);
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                FlowStateVisualizer.SetService(null);
            }
        }
    }
}
