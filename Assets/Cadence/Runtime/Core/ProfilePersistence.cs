using UnityEngine;

namespace Cadence
{
    /// <summary>
    /// PlayerPrefs-based profile persistence.
    /// Cross-platform, small payload (&lt;2KB), no file I/O complexity.
    /// </summary>
    public static class ProfilePersistence
    {
        public static string Load(string key)
        {
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
        }

        public static void Save(string key, string json)
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public static void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        public static bool HasProfile(string key)
        {
            return PlayerPrefs.HasKey(key);
        }
    }
}
