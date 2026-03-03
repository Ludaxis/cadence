using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Cadence.Editor
{
    /// <summary>
    /// Checks for Cadence SDK updates by querying the GitHub Releases API.
    /// Accessible via Cadence > Check for Updates.
    /// </summary>
    public class CadenceUpdateChecker : EditorWindow
    {
        // ───────────────────── Constants ─────────────────────

        private const string GitHubApiUrl =
            "https://api.github.com/repos/Ludaxis/cadence/releases/latest";

        private const string ReleasesPageUrl =
            "https://github.com/Ludaxis/cadence/releases";

        private const string PackageJsonGuid = ""; // not used; we find by path
        private const string LastCheckPrefKey = "Cadence_LastUpdateCheck";
        private const string DismissedVersionPrefKey = "Cadence_DismissedVersion";
        private const string AutoCheckPrefKey = "Cadence_AutoCheckEnabled";
        private const double AutoCheckIntervalHours = 24.0;

        // ───────────────────── State ─────────────────────

        private static UnityWebRequest _request;
        private static bool _checking;
        private static CheckResult _result;
        private static bool _autoCheckDone;

        private Vector2 _scrollPos;

        // ───────────────────── Menu ─────────────────────

        [MenuItem("Cadence/Check for Updates", false, 900)]
        public static void ShowWindow()
        {
            var window = GetWindow<CadenceUpdateChecker>("Cadence Updates");
            window.minSize = new Vector2(380, 260);
            if (_result == null)
                BeginCheck();
        }

        // ───────────────────── Auto-check on Editor Load ─────────────────────

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            if (!EditorPrefs.GetBool(AutoCheckPrefKey, true))
                return;

            string lastCheck = EditorPrefs.GetString(LastCheckPrefKey, "");
            if (!string.IsNullOrEmpty(lastCheck) &&
                DateTime.TryParse(lastCheck, out var lastTime) &&
                (DateTime.UtcNow - lastTime).TotalHours < AutoCheckIntervalHours)
            {
                return;
            }

            // Delay to avoid slowing editor startup
            EditorApplication.delayCall += () =>
            {
                if (!_autoCheckDone && !_checking)
                {
                    _autoCheckDone = true;
                    BeginCheck(silent: true);
                }
            };
        }

        // ───────────────────── Check Logic ─────────────────────

        private static void BeginCheck(bool silent = false)
        {
            if (_checking) return;
            _checking = true;
            _result = null;

            _request = UnityWebRequest.Get(GitHubApiUrl);
            _request.SetRequestHeader("User-Agent", "Cadence-Unity-SDK");
            _request.SetRequestHeader("Accept", "application/vnd.github.v3+json");
            _request.timeout = 10;

            var op = _request.SendWebRequest();
            op.completed += _ => OnRequestComplete(silent);
        }

        private static void OnRequestComplete(bool silent)
        {
            _checking = false;

            if (_request == null)
                return;

#if UNITY_2020_1_OR_NEWER
            bool isError = _request.result != UnityWebRequest.Result.Success;
#else
            bool isError = _request.isNetworkError || _request.isHttpError;
#endif

            if (isError)
            {
                _result = new CheckResult
                {
                    Error = $"Network error: {_request.error}",
                    CheckedAt = DateTime.UtcNow
                };
                _request.Dispose();
                _request = null;
                return;
            }

            string json = _request.downloadHandler.text;
            _request.Dispose();
            _request = null;

            EditorPrefs.SetString(LastCheckPrefKey, DateTime.UtcNow.ToString("o"));

            try
            {
                var release = JsonUtility.FromJson<GitHubRelease>(json);
                string currentVersion = GetCurrentVersion();
                string latestVersion = CleanVersionTag(release.tag_name);
                bool isNewer = IsNewerVersion(latestVersion, currentVersion);

                _result = new CheckResult
                {
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    IsUpdateAvailable = isNewer,
                    ReleaseName = release.name,
                    ReleaseBody = release.body,
                    ReleaseUrl = release.html_url,
                    IsPreRelease = release.prerelease,
                    PublishedAt = release.published_at,
                    CheckedAt = DateTime.UtcNow
                };

                // Silent auto-check: only show window if update available and not dismissed
                if (silent && isNewer)
                {
                    string dismissed = EditorPrefs.GetString(DismissedVersionPrefKey, "");
                    if (dismissed != latestVersion)
                    {
                        Debug.Log($"[Cadence] Update available: v{latestVersion} (current: v{currentVersion}). " +
                                  $"Open Cadence > Check for Updates for details.");
                    }
                }
            }
            catch (Exception e)
            {
                _result = new CheckResult
                {
                    Error = $"Failed to parse response: {e.Message}",
                    CheckedAt = DateTime.UtcNow
                };
            }
        }

        // ───────────────────── GUI ─────────────────────

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // Header
            EditorGUILayout.LabelField("Cadence SDK — Update Checker", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Auto-check toggle
            bool autoCheck = EditorPrefs.GetBool(AutoCheckPrefKey, true);
            bool newAutoCheck = EditorGUILayout.Toggle("Auto-check on editor start", autoCheck);
            if (newAutoCheck != autoCheck)
                EditorPrefs.SetBool(AutoCheckPrefKey, newAutoCheck);

            EditorGUILayout.Space(8);

            // Checking state
            if (_checking)
            {
                EditorGUILayout.HelpBox("Checking for updates...", MessageType.Info);
                Repaint();
                EditorGUILayout.EndScrollView();
                return;
            }

            // No result yet
            if (_result == null)
            {
                if (GUILayout.Button("Check Now", GUILayout.Height(30)))
                    BeginCheck();
                EditorGUILayout.EndScrollView();
                return;
            }

            // Error
            if (!string.IsNullOrEmpty(_result.Error))
            {
                EditorGUILayout.HelpBox(_result.Error, MessageType.Warning);
                DrawCheckAgainButton();
                EditorGUILayout.EndScrollView();
                return;
            }

            // Version comparison
            DrawVersionComparison();

            // Release notes
            if (_result.IsUpdateAvailable && !string.IsNullOrEmpty(_result.ReleaseBody))
            {
                EditorGUILayout.Space(8);
                DrawReleaseNotes();
            }

            EditorGUILayout.Space(8);
            DrawCheckAgainButton();

            EditorGUILayout.EndScrollView();
        }

        private void DrawVersionComparison()
        {
            // Current
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Installed Version:", GUILayout.Width(130));
            EditorGUILayout.LabelField($"v{_result.CurrentVersion}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            // Latest
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Latest Version:", GUILayout.Width(130));
            var style = new GUIStyle(EditorStyles.boldLabel);
            if (_result.IsUpdateAvailable)
                style.normal.textColor = new Color(0.2f, 0.8f, 0.2f);
            EditorGUILayout.LabelField($"v{_result.LatestVersion}", style);
            if (_result.IsPreRelease)
            {
                GUILayout.Label("PRE-RELEASE", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            if (_result.IsUpdateAvailable)
            {
                EditorGUILayout.HelpBox(
                    $"A new version of Cadence is available: v{_result.LatestVersion}\n" +
                    $"You are running v{_result.CurrentVersion}.",
                    MessageType.Info);

                EditorGUILayout.Space(4);

                // Update actions
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("View Release on GitHub", GUILayout.Height(28)))
                {
                    Application.OpenURL(_result.ReleaseUrl ?? ReleasesPageUrl);
                }

                if (GUILayout.Button("Copy Install URL", GUILayout.Height(28)))
                {
                    string url = "git+ssh://git@github.com/Ludaxis/cadence.git?path=Assets/Cadence";
                    EditorGUIUtility.systemCopyBuffer = url;
                    Debug.Log($"[Cadence] Package URL copied: {url}");
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Update via Package Manager"))
                {
                    EditorApplication.ExecuteMenuItem("Window/Package Manager");
                }

                if (GUILayout.Button("Dismiss this version"))
                {
                    EditorPrefs.SetString(DismissedVersionPrefKey, _result.LatestVersion);
                    Debug.Log($"[Cadence] Dismissed update notification for v{_result.LatestVersion}.");
                }

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("You are running the latest version.", MessageType.Info);
            }

            // Last checked
            if (_result.CheckedAt != default)
            {
                EditorGUILayout.Space(2);
                var local = _result.CheckedAt.ToLocalTime();
                EditorGUILayout.LabelField($"Last checked: {local:yyyy-MM-dd HH:mm}",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawReleaseNotes()
        {
            EditorGUILayout.LabelField("Release Notes", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(_result.ReleaseName))
                EditorGUILayout.LabelField(_result.ReleaseName, EditorStyles.largeLabel);

            // Render release body as wrapped text
            var bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                richText = false,
                fontSize = 11
            };

            string body = _result.ReleaseBody;
            // Truncate very long notes
            if (body.Length > 2000)
                body = body.Substring(0, 2000) + "\n\n... (truncated, see GitHub for full notes)";

            EditorGUILayout.LabelField(body, bodyStyle);
        }

        private void DrawCheckAgainButton()
        {
            if (GUILayout.Button("Check Again", GUILayout.Height(24)))
            {
                _result = null;
                BeginCheck();
            }
        }

        // ───────────────────── Version Parsing ─────────────────────

        private static string GetCurrentVersion()
        {
            // Find package.json relative to this script's assembly
            string[] guids = AssetDatabase.FindAssets("package t:TextAsset",
                new[] { "Packages/com.ludaxis.cadence" });

            // Fallback: search by known path patterns
            if (guids.Length == 0)
            {
                string[] searchPaths = new[]
                {
                    "Assets/Cadence/package.json",
                    "Packages/com.ludaxis.cadence/package.json"
                };

                foreach (var path in searchPaths)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (asset != null)
                    {
                        var manifest = JsonUtility.FromJson<PackageManifest>(asset.text);
                        if (!string.IsNullOrEmpty(manifest.version))
                            return manifest.version;
                    }
                }
            }

            // Try Unity PackageInfo API
#if UNITY_2019_4_OR_NEWER
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(DDAConfig).Assembly);
            if (packageInfo != null)
                return packageInfo.version;
#endif

            return "unknown";
        }

        private static string CleanVersionTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return "0.0.0";
            // Strip leading 'v' or 'V'
            tag = tag.TrimStart('v', 'V');
            return tag;
        }

        private static bool IsNewerVersion(string latest, string current)
        {
            if (string.IsNullOrEmpty(latest) || string.IsNullOrEmpty(current))
                return false;

            // Parse semver: major.minor.patch(-prerelease)?
            var latestParts = ParseVersion(latest);
            var currentParts = ParseVersion(current);

            for (int i = 0; i < 3; i++)
            {
                if (latestParts[i] > currentParts[i]) return true;
                if (latestParts[i] < currentParts[i]) return false;
            }

            return false; // Equal
        }

        private static int[] ParseVersion(string version)
        {
            var result = new int[3];
            // Strip prerelease suffix
            int dashIdx = version.IndexOf('-');
            if (dashIdx >= 0)
                version = version.Substring(0, dashIdx);

            string[] parts = version.Split('.');
            for (int i = 0; i < Mathf.Min(parts.Length, 3); i++)
            {
                int.TryParse(parts[i], out result[i]);
            }

            return result;
        }

        // ───────────────────── Data Models ─────────────────────

        [Serializable]
        private class GitHubRelease
        {
            public string tag_name;
            public string name;
            public string body;
            public bool prerelease;
            public string html_url;
            public string published_at;
        }

        [Serializable]
        private class PackageManifest
        {
            public string version;
        }

        private class CheckResult
        {
            public string CurrentVersion;
            public string LatestVersion;
            public bool IsUpdateAvailable;
            public string ReleaseName;
            public string ReleaseBody;
            public string ReleaseUrl;
            public bool IsPreRelease;
            public string PublishedAt;
            public string Error;
            public DateTime CheckedAt;
        }
    }
}
