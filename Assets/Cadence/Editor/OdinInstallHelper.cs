using UnityEditor;
using UnityEngine;

namespace Cadence.Editor
{
    public static class OdinInstallHelper
    {
        private const string AssetStoreUrl =
            "https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041";

        [MenuItem("Cadence/Install Odin Inspector", false, 800)]
        private static void ShowOdinHelper()
        {
            if (IsOdinInstalled())
            {
                EditorUtility.DisplayDialog(
                    "Odin Inspector — Already Installed",
                    "Odin Inspector is already installed in this project.\n\n" +
                    "All Cadence inspector enhancements are active:\n" +
                    "  - Grouped, color-coded config sections\n" +
                    "  - Inline sub-config editing\n" +
                    "  - Conditional field visibility\n" +
                    "  - Required-field validation",
                    "OK");
                return;
            }

            bool openStore = EditorUtility.DisplayDialog(
                "Cadence — Install Odin Inspector",
                "Odin Inspector is not installed. Cadence works without it, " +
                "but with Odin you get:\n\n" +
                "  - Grouped, color-coded config sections\n" +
                "  - Inline sub-config editing\n" +
                "  - Conditional field visibility\n" +
                "  - Required-field validation\n\n" +
                "Would you like to open the Asset Store page?",
                "Open Asset Store", "Cancel");

            if (openStore)
                Application.OpenURL(AssetStoreUrl);
        }

        private static bool IsOdinInstalled()
        {
#if ODIN_INSPECTOR
            return true;
#else
            return false;
#endif
        }
    }
}
