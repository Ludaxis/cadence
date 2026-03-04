using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Cadence
{
    /// <summary>
    /// Drop-in singleton MonoBehaviour that wraps DDAService with auto-tick,
    /// auto-persistence, and static access. Optional — the code-only
    /// <c>new DDAService(config)</c> workflow remains fully supported.
    /// </summary>
    [AddComponentMenu("Cadence/Cadence Manager")]
    [DefaultExecutionOrder(-100)]
    public class CadenceManager : MonoBehaviour
    {
        // ───────────────────── Domain Reload Fix ─────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticState()
        {
            _instance = null;
            OnServiceInitialized = null;
        }

        // ───────────────────── Singleton ─────────────────────

        private static CadenceManager _instance;

        /// <summary>The singleton instance, or null if none exists.</summary>
        public static CadenceManager Instance => _instance;

        /// <summary>Shortcut to the managed IDDAService, or null if not initialized.</summary>
        public static IDDAService Service => _instance != null ? _instance._service : null;

        /// <summary>Fired once after the service is created and the profile is loaded.</summary>
        public static event Action<IDDAService> OnServiceInitialized;

        // ───────────────────── Inspector ─────────────────────

#if ODIN_INSPECTOR
        [TitleGroup("Configuration")]
        [Required("A DDAConfig asset is required. Use Cadence > Setup Wizard to create one.")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
#else
        [Header("Configuration")]
#endif
        [SerializeField] private DDAConfig _config;

#if ODIN_INSPECTOR
        [TitleGroup("Persistence")]
        [InfoBox("When enabled, the player profile is automatically saved to PlayerPrefs " +
                 "and restored on next launch.")]
        [ToggleLeft]
#else
        [Header("Persistence")]
#endif
        [SerializeField] private bool _autoSaveProfile = true;

#if ODIN_INSPECTOR
        [TitleGroup("Persistence")]
        [ShowIf("_autoSaveProfile")]
#endif
        [SerializeField] private string _profileKey = "Cadence_PlayerProfile";

#if ODIN_INSPECTOR
        [TitleGroup("Signal Storage")]
        [InfoBox("Creates a FileSignalStorage for signal persistence to disk.")]
        [ToggleLeft]
#else
        [Header("Signal Storage")]
#endif
        [SerializeField] private bool _enableFileStorage = true;

#if ODIN_INSPECTOR
        [TitleGroup("Debug")]
        [ToggleLeft]
#else
        [Header("Debug")]
#endif
        [SerializeField] private bool _verboseLogging;

        // ───────────────────── Runtime ─────────────────────

        private IDDAService _service;
        private bool _initialized;

        /// <summary>Whether the service has been created and is ready to use.</summary>
        public bool IsInitialized => _initialized;

        // ───────────────────── Lifecycle ─────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (_verboseLogging)
                    Debug.Log("[Cadence] Duplicate CadenceManager destroyed.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Update()
        {
            if (!_initialized) return;
            _service.Tick(Time.unscaledDeltaTime);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveProfileIfNeeded();
        }

        private void OnApplicationQuit()
        {
            SaveProfileIfNeeded();
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (_instance != this) return;

            SaveProfileIfNeeded();
            _instance = null;
        }

        // ───────────────────── Init ─────────────────────

        private void Initialize()
        {
            if (_config == null)
            {
                Debug.LogError("[Cadence] CadenceManager requires a DDAConfig asset. " +
                               "Assign one in the Inspector or use Cadence > Setup Wizard.");
                return;
            }

            ISignalStorage storage = _enableFileStorage ? new FileSignalStorage() : null;
            var service = new DDAService(_config, storage);

            if (_autoSaveProfile)
            {
                string json = ProfilePersistence.Load(_profileKey);
                if (json != null)
                {
                    try
                    {
                        service.LoadProfile(json);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[Cadence] Failed to load profile, starting fresh: {ex.Message}");
                    }
                    if (_verboseLogging)
                        Debug.Log("[Cadence] Player profile loaded from PlayerPrefs.");
                }
            }

            _service = service;
            _initialized = true;

            if (_verboseLogging)
                Debug.Log("[Cadence] CadenceManager initialized.");

            OnServiceInitialized?.Invoke(_service);
        }

        // ───────────────────── Persistence ─────────────────────

        private void SaveProfileIfNeeded()
        {
            if (!_initialized || !_autoSaveProfile) return;

            string json = _service.SaveProfile();
            ProfilePersistence.Save(_profileKey, json);

            if (_verboseLogging)
                Debug.Log("[Cadence] Player profile saved to PlayerPrefs.");
        }

        /// <summary>
        /// Deletes the persisted profile and reinitializes the service with a fresh profile.
        /// </summary>
        public void ResetProfile()
        {
            if (_autoSaveProfile)
                ProfilePersistence.Delete(_profileKey);

            if (_config != null)
            {
                ISignalStorage storage = _enableFileStorage ? new FileSignalStorage() : null;
                _service = new DDAService(_config, storage);
                _initialized = true;

                if (_verboseLogging)
                    Debug.Log("[Cadence] Player profile reset.");
            }
        }
    }
}
