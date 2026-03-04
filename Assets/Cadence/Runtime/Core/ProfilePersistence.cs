using UnityEngine;

namespace Cadence
{
    /// <summary>
    /// PlayerPrefs-based profile persistence.
    /// Cross-platform, small payload (&lt;2KB), no file I/O complexity.
    /// </summary>
    public static class ProfilePersistence
    {
        /// <summary>
        /// Loads a previously saved profile JSON string, or <c>null</c> if the key does not exist.
        /// </summary>
        /// <param name="key">PlayerPrefs key used when the profile was saved.</param>
        public static string Load(string key)
        {
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
        }

        /// <summary>
        /// Persists a profile JSON string to PlayerPrefs and flushes to disk immediately.
        /// </summary>
        /// <param name="key">PlayerPrefs key to store the profile under.</param>
        /// <param name="json">Serialized profile JSON produced by <see cref="IDDAService.SaveProfile"/>.</param>
        public static void Save(string key, string json)
        {
            PlayerPrefs.SetString(key, json);
        }

        /// <summary>
        /// Deletes a stored profile and flushes the change to disk.
        /// </summary>
        /// <param name="key">PlayerPrefs key to remove.</param>
        public static void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
        }

        /// <summary>
        /// Returns <c>true</c> if a profile exists under the given key.
        /// </summary>
        /// <param name="key">PlayerPrefs key to check.</param>
        public static bool HasProfile(string key)
        {
            return PlayerPrefs.HasKey(key);
        }
    }
}
