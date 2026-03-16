using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Cadence.Editor
{
    /// <summary>
    /// Checks for Cadence SDK updates by comparing the installed package commit
    /// against the latest commit on the remote repository via GitHub API.
    /// Falls back to release-based checking when releases exist.
    /// Accessible via Cadence > Check for Updates.
    /// </summary>
    public class CadenceUpdateChecker : EditorWindow
    {
        // ───────────────────── Constants ─────────────────────

        private const string RepoOwner = "Ludaxis";
        private const string RepoName = "cadence";
        private const string DefaultBranch = "main";

        private const string ReleasesApiUrl =
            "https://api.github.com/repos/" + RepoOwner + "/" + RepoName + "/releases/latest";

        private const string CommitsApiUrl =
            "https://api.github.com/repos/" + RepoOwner + "/" + RepoName + "/commits/" + DefaultBranch;

        private const string RepoPageUrl =
            "https://github.com/" + RepoOwner + "/" + RepoName;

        private const string LastCheckPrefKey = "Cadence_LastUpdateCheck";
        private const string DismissedHashPrefKey = "Cadence_DismissedHash";
        private const string AutoCheckPrefKey = "Cadence_AutoCheckEnabled";
        private const double AutoCheckIntervalHours = 24.0;

        // ───────────────────── State ─────────────────────

        private static UnityWebRequest _request;
        private static bool _checking;
        private static CheckResult _result;
        private static bool _autoCheckDone;
        private static bool _isSilent;

        private Vector2 _scrollPos;

        // ───────────────────── Menu ─────────────────────

        [MenuItem("Cadence/Check for Updates", false, 900)]
        public static void ShowWindow()
        {
            var window = GetWindow<CadenceUpdateChecker>("Cadence Updates");
            window.minSize = new Vector2(400, 300);
            if (_result == null)
                BeginCheck(silent: false);
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

        private static void BeginCheck(bool silent)
        {
            if (_checking) return;
            _checking = true;
            _isSilent = silent;
            _result = null;

            // Try releases first
            SendRequest(ReleasesApiUrl, OnReleasesResponse);
        }

        private static void SendRequest(string url, Action<string, long> onComplete)
        {
            _request = UnityWebRequest.Get(url);
            _request.SetRequestHeader("User-Agent", "Cadence-Unity-SDK");
            _request.SetRequestHeader("Accept", "application/vnd.github.v3+json");
            _request.timeout = 15;

            var op = _request.SendWebRequest();
            op.completed += _ =>
            {
                if (_request == null) return;

                long responseCode = _request.responseCode;
                string body = "";

#if UNITY_2020_1_OR_NEWER
                bool isError = _request.result == UnityWebRequest.Result.ConnectionError ||
                               _request.result == UnityWebRequest.Result.ProtocolError;
#else
                bool isError = _request.isNetworkError || _request.isHttpError;
#endif
                // 404 is not a fatal error — it means no releases exist
                if (!isError || responseCode == 404)
                    body = _request.downloadHandler?.text ?? "";

                string error = isError && responseCode != 404 ? _request.error : null;

                _request.Dispose();
                _request = null;

                onComplete(string.IsNullOrEmpty(error) ? body : null, responseCode);
            };
        }

        private static void OnReleasesResponse(string json, long statusCode)
        {
            // 404 = no releases exist, fall back to commit comparison
            if (statusCode == 404 || string.IsNullOrEmpty(json))
            {
                SendRequest(CommitsApiUrl, OnCommitsResponse);
                return;
            }

            // Network error
            if (json == null)
            {
                FinishWithError($"Network error (HTTP {statusCode})");
                return;
            }

            EditorPrefs.SetString(LastCheckPrefKey, DateTime.UtcNow.ToString("o"));

            try
            {
                var release = JsonUtility.FromJson<GitHubRelease>(json);
                string currentVersion = GetCurrentVersion();
                string latestVersion = CleanVersionTag(release.tag_name);
                bool isNewer = IsNewerVersion(latestVersion, currentVersion);

                _result = new CheckResult
                {
                    Mode = CheckMode.Release,
                    CurrentVersion = currentVersion,
                    LatestVersion = latestVersion,
                    IsUpdateAvailable = isNewer,
                    ReleaseName = release.name,
                    ReleaseBody = release.body,
                    ReleaseUrl = release.html_url,
                    IsPreRelease = release.prerelease,
                    CheckedAt = DateTime.UtcNow
                };

                NotifySilent();
            }
            catch (Exception e)
            {
                FinishWithError($"Failed to parse release: {e.Message}");
            }

            _checking = false;
        }

        private static void OnCommitsResponse(string json, long statusCode)
        {
            _checking = false;

            if (string.IsNullOrEmpty(json) || statusCode != 200)
            {
                FinishWithError($"Cannot reach repository (HTTP {statusCode}).\n" +
                                "This may be a private repo — GitHub API requires authentication for private repos.");
                return;
            }

            EditorPrefs.SetString(LastCheckPrefKey, DateTime.UtcNow.ToString("o"));

            try
            {
                var commit = JsonUtility.FromJson<GitHubCommit>(json);
                string remoteHash = commit.sha;
                string localHash = GetInstalledCommitHash();
                string currentVersion = GetCurrentVersion();

                bool isNewer = false;
                if (!string.IsNullOrEmpty(localHash) && !string.IsNullOrEmpty(remoteHash))
                    isNewer = !remoteHash.StartsWith(localHash) && !localHash.StartsWith(remoteHash);

                string commitMsg = "";
                if (commit.commit != null)
                    commitMsg = commit.commit.message ?? "";
                // Truncate to first line
                int nl = commitMsg.IndexOf('\n');
                if (nl > 0) commitMsg = commitMsg.Substring(0, nl);

                _result = new CheckResult
                {
                    Mode = CheckMode.Commit,
                    CurrentVersion = currentVersion,
                    LocalHash = string.IsNullOrEmpty(localHash) ? "unknown" : localHash,
                    RemoteHash = string.IsNullOrEmpty(remoteHash) ? "unknown" : remoteHash,
                    LatestCommitMessage = commitMsg,
                    LatestCommitDate = commit.commit?.author?.date ?? "",
                    IsUpdateAvailable = isNewer,
                    ReleaseUrl = RepoPageUrl + "/commits/" + DefaultBranch,
                    CheckedAt = DateTime.UtcNow
                };

                NotifySilent();
            }
            catch (Exception e)
            {
                FinishWithError($"Failed to parse commit: {e.Message}");
            }
        }

        private static void FinishWithError(string error)
        {
            _checking = false;
            _result = new CheckResult
            {
                Error = error,
                CheckedAt = DateTime.UtcNow
            };
        }

        private static void NotifySilent()
        {
            if (!_isSilent || _result == null || !_result.IsUpdateAvailable) return;

            string id = _result.Mode == CheckMode.Release
                ? _result.LatestVersion
                : _result.RemoteHash;

            string dismissed = EditorPrefs.GetString(DismissedHashPrefKey, "");
            if (dismissed == id) return;

            if (_result.Mode == CheckMode.Release)
            {
                Debug.Log($"[Cadence] Update available: v{_result.LatestVersion} " +
                          $"(current: v{_result.CurrentVersion}). " +
                          "Open Cadence > Check for Updates.");
            }
            else
            {
                Debug.Log($"[Cadence] New commits available on {DefaultBranch}. " +
                          $"Local: {Shorten(_result.LocalHash)}, " +
                          $"Remote: {Shorten(_result.RemoteHash)}. " +
                          "Open Cadence > Check for Updates.");
            }
        }

        // ───────────────────── GUI ─────────────────────

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("Cadence SDK — Update Checker", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Auto-check toggle
            bool autoCheck = EditorPrefs.GetBool(AutoCheckPrefKey, true);
            bool newAutoCheck = EditorGUILayout.Toggle("Auto-check on editor start", autoCheck);
            if (newAutoCheck != autoCheck)
                EditorPrefs.SetBool(AutoCheckPrefKey, newAutoCheck);

            EditorGUILayout.Space(8);

            if (_checking)
            {
                EditorGUILayout.HelpBox("Checking for updates...", MessageType.Info);
                Repaint();
                EditorGUILayout.EndScrollView();
                return;
            }

            if (_result == null)
            {
                if (GUILayout.Button("Check Now", GUILayout.Height(30)))
                    BeginCheck(silent: false);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (!string.IsNullOrEmpty(_result.Error))
            {
                EditorGUILayout.HelpBox(_result.Error, MessageType.Warning);
                DrawCheckAgainButton();
                EditorGUILayout.EndScrollView();
                return;
            }

            if (_result.Mode == CheckMode.Release)
                DrawReleaseComparison();
            else
                DrawCommitComparison();

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

        private void DrawReleaseComparison()
        {
            DrawRow("Installed Version:", $"v{_result.CurrentVersion}");

            var style = new GUIStyle(EditorStyles.boldLabel);
            if (_result.IsUpdateAvailable)
                style.normal.textColor = new Color(0.2f, 0.8f, 0.2f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Latest Release:", GUILayout.Width(140));
            EditorGUILayout.LabelField($"v{_result.LatestVersion}", style);
            if (_result.IsPreRelease)
                GUILayout.Label("PRE-RELEASE", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            DrawUpdateStatus();
        }

        private void DrawCommitComparison()
        {
            DrawRow("Package Version:", $"v{_result.CurrentVersion}");
            DrawRow("Installed Commit:", Shorten(_result.LocalHash));

            var style = new GUIStyle(EditorStyles.boldLabel);
            if (_result.IsUpdateAvailable)
                style.normal.textColor = new Color(0.2f, 0.8f, 0.2f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Latest Commit:", GUILayout.Width(140));
            EditorGUILayout.LabelField(Shorten(_result.RemoteHash), style);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_result.LatestCommitMessage))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("", GUILayout.Width(140));
                EditorGUILayout.LabelField(_result.LatestCommitMessage, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            if (!string.IsNullOrEmpty(_result.LatestCommitDate))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("", GUILayout.Width(140));
                EditorGUILayout.LabelField(FormatDate(_result.LatestCommitDate), EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);

            // Info about no releases
            EditorGUILayout.HelpBox(
                "No published releases found. Comparing git commits instead.\n" +
                "Create a GitHub Release to enable version-based update checking.",
                MessageType.None);

            EditorGUILayout.Space(4);
            DrawUpdateStatus();
        }

        private void DrawUpdateStatus()
        {
            if (_result.IsUpdateAvailable)
            {
                EditorGUILayout.HelpBox(
                    _result.Mode == CheckMode.Release
                        ? $"Update available: v{_result.LatestVersion} (you have v{_result.CurrentVersion})"
                        : $"New commits available on '{DefaultBranch}' branch.",
                    MessageType.Info);

                EditorGUILayout.Space(4);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("View on GitHub", GUILayout.Height(28)))
                    Application.OpenURL(_result.ReleaseUrl ?? RepoPageUrl);

                if (GUILayout.Button("Copy Package URL", GUILayout.Height(28)))
                {
                    string url = $"git+ssh://git@github.com/{RepoOwner}/{RepoName}.git?path=Assets/Cadence";
                    EditorGUIUtility.systemCopyBuffer = url;
                    Debug.Log($"[Cadence] Package URL copied: {url}");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Open Package Manager"))
                    UnityEditor.PackageManager.UI.Window.Open("");

                if (GUILayout.Button("Dismiss"))
                {
                    string id = _result.Mode == CheckMode.Release
                        ? _result.LatestVersion
                        : _result.RemoteHash;
                    EditorPrefs.SetString(DismissedHashPrefKey, id);
                    _result.IsUpdateAvailable = false;
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("You are up to date.", MessageType.Info);
            }

            if (_result.CheckedAt != default)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField(
                    $"Last checked: {_result.CheckedAt.ToLocalTime():yyyy-MM-dd HH:mm}",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawReleaseNotes()
        {
            EditorGUILayout.LabelField("Release Notes", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(_result.ReleaseName))
                EditorGUILayout.LabelField(_result.ReleaseName, EditorStyles.largeLabel);

            var bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                richText = false,
                fontSize = 11
            };

            string body = _result.ReleaseBody;
            if (body.Length > 2000)
                body = body.Substring(0, 2000) + "\n\n... (truncated)";

            EditorGUILayout.LabelField(body, bodyStyle);
        }

        private void DrawCheckAgainButton()
        {
            if (GUILayout.Button("Check Again", GUILayout.Height(24)))
            {
                _result = null;
                BeginCheck(silent: false);
            }
        }

        private static void DrawRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(140));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        // ───────────────────── Version / Hash Helpers ─────────────────────

        private static string GetCurrentVersion()
        {
            // Try PackageInfo API first (works for installed UPM packages)
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(DDAConfig).Assembly);
            if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.version))
                return packageInfo.version;

            // Fallback: read package.json directly
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

            return "unknown";
        }

        private static string GetInstalledCommitHash()
        {
            // PackageInfo.git.hash contains the resolved commit for git packages
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(DDAConfig).Assembly);

            if (packageInfo != null && packageInfo.git != null &&
                !string.IsNullOrEmpty(packageInfo.git.hash))
            {
                return packageInfo.git.hash;
            }

            // Fallback: try reading packages-lock.json
            try
            {
                string lockPath = System.IO.Path.Combine(
                    Application.dataPath, "..", "Packages", "packages-lock.json");

                if (System.IO.File.Exists(lockPath))
                {
                    string lockJson = System.IO.File.ReadAllText(lockPath);
                    // Simple parse: find "com.ludaxis.cadence" and its "hash" field
                    int pkgIdx = lockJson.IndexOf("com.ludaxis.cadence", StringComparison.Ordinal);
                    if (pkgIdx >= 0)
                    {
                        int hashIdx = lockJson.IndexOf("\"hash\"", pkgIdx, StringComparison.Ordinal);
                        if (hashIdx >= 0)
                        {
                            int colonIdx = lockJson.IndexOf(':', hashIdx);
                            int quoteStart = lockJson.IndexOf('"', colonIdx + 1);
                            int quoteEnd = lockJson.IndexOf('"', quoteStart + 1);
                            if (quoteStart >= 0 && quoteEnd > quoteStart)
                                return lockJson.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                        }
                    }
                }
            }
            catch
            {
                // Ignore parse failures
            }

            return "";
        }

        private static string CleanVersionTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return "0.0.0";
            return tag.TrimStart('v', 'V');
        }

        private static bool IsNewerVersion(string latest, string current)
        {
            if (string.IsNullOrEmpty(latest) || string.IsNullOrEmpty(current))
                return false;

            var latestParts = ParseVersion(latest);
            var currentParts = ParseVersion(current);

            for (int i = 0; i < 3; i++)
            {
                if (latestParts[i] > currentParts[i]) return true;
                if (latestParts[i] < currentParts[i]) return false;
            }

            return false;
        }

        private static int[] ParseVersion(string version)
        {
            var result = new int[3];
            int dashIdx = version.IndexOf('-');
            if (dashIdx >= 0)
                version = version.Substring(0, dashIdx);

            string[] parts = version.Split('.');
            for (int i = 0; i < Mathf.Min(parts.Length, 3); i++)
                int.TryParse(parts[i], out result[i]);

            return result;
        }

        private static string Shorten(string hash)
        {
            if (string.IsNullOrEmpty(hash) || hash == "unknown") return "unknown";
            return hash.Length > 8 ? hash.Substring(0, 8) : hash;
        }

        private static string FormatDate(string isoDate)
        {
            if (DateTime.TryParse(isoDate, out var dt))
                return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            return isoDate;
        }

        // ───────────────────── Data Models ─────────────────────

        private enum CheckMode { Release, Commit }

        [Serializable]
        private class GitHubRelease
        {
            public string tag_name;
            public string name;
            public string body;
            public bool prerelease;
            public string html_url;
        }

        [Serializable]
        private class GitHubCommit
        {
            public string sha;
            public GitHubCommitDetail commit;
        }

        [Serializable]
        private class GitHubCommitDetail
        {
            public string message;
            public GitHubAuthor author;
        }

        [Serializable]
        private class GitHubAuthor
        {
            public string date;
        }

        [Serializable]
        private class PackageManifest
        {
            public string version;
        }

        private class CheckResult
        {
            public CheckMode Mode;
            public string CurrentVersion;
            public string LatestVersion;
            public string LocalHash;
            public string RemoteHash;
            public string LatestCommitMessage;
            public string LatestCommitDate;
            public bool IsUpdateAvailable;
            public bool IsPreRelease;
            public string ReleaseName;
            public string ReleaseBody;
            public string ReleaseUrl;
            public string Error;
            public DateTime CheckedAt;
        }
    }
}
