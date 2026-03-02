using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Cadence
{
    /// <summary>
    /// Persists signal batches as JSON files in Application.persistentDataPath/Cadence/.
    /// Each session is saved as {levelId}_{timestamp}.json.
    /// </summary>
    public sealed class FileSignalStorage : ISignalStorage
    {
        private readonly string _basePath;

        public FileSignalStorage()
        {
            _basePath = Path.Combine(Application.persistentDataPath, "Cadence", "Signals");
        }

        public FileSignalStorage(string basePath)
        {
            _basePath = basePath;
        }

        public void Save(string levelId, SignalBatch batch)
        {
            try
            {
                EnsureDirectory();
                string safeLevelId = SanitizeFileName(levelId);
                long timestamp = DateTime.UtcNow.Ticks;
                string fileName = $"{safeLevelId}_{timestamp}.json";
                string filePath = Path.Combine(_basePath, fileName);
                string json = SignalLogSerializer.Serialize(batch);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Cadence] Failed to save signals: {ex.Message}");
            }
        }

        public SignalBatch Load(string levelId, int sessionIndex = -1)
        {
            try
            {
                var files = GetFilesForLevel(levelId);
                if (files.Count == 0) return new SignalBatch();

                int index = sessionIndex < 0 ? files.Count - 1 : sessionIndex;
                if (index < 0 || index >= files.Count) return new SignalBatch();

                string json = File.ReadAllText(files[index]);
                return SignalLogSerializer.Deserialize(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Cadence] Failed to load signals: {ex.Message}");
                return new SignalBatch();
            }
        }

        public int GetSessionCount(string levelId)
        {
            return GetFilesForLevel(levelId).Count;
        }

        public void Prune(int maxSessionsPerLevel)
        {
            try
            {
                if (!Directory.Exists(_basePath)) return;

                var allFiles = new List<string>(Directory.GetFiles(_basePath, "*.json"));
                allFiles.Sort(StringComparer.Ordinal);

                // Group by level prefix (everything before last _)
                var grouped = new Dictionary<string, List<string>>();
                for (int i = 0; i < allFiles.Count; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(allFiles[i]);
                    int lastUnderscore = name.LastIndexOf('_');
                    string prefix = lastUnderscore > 0 ? name.Substring(0, lastUnderscore) : name;
                    if (!grouped.TryGetValue(prefix, out var list))
                    {
                        list = new List<string>();
                        grouped[prefix] = list;
                    }
                    list.Add(allFiles[i]);
                }

                foreach (var kvp in grouped)
                {
                    var files = kvp.Value;
                    files.Sort(StringComparer.Ordinal);
                    while (files.Count > maxSessionsPerLevel)
                    {
                        File.Delete(files[0]);
                        files.RemoveAt(0);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Cadence] Failed to prune signals: {ex.Message}");
            }
        }

        private List<string> GetFilesForLevel(string levelId)
        {
            if (!Directory.Exists(_basePath)) return new List<string>();

            string safeLevelId = SanitizeFileName(levelId);
            string pattern = $"{safeLevelId}_*.json";
            var files = new List<string>(Directory.GetFiles(_basePath, pattern));
            files.Sort(StringComparer.Ordinal);
            return files;
        }

        private void EnsureDirectory()
        {
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unknown";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
