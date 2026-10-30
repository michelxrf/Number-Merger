using System;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace NumberMerge.Ads
{
    // Initializes the LevelPlay (ironSource) mediation SDK. Ad formats (interstitial, rewarded, banner)
    // should be wired on top of this once IsInitialized is true or InitSucceeded has fired.
    // Setup: https://docs.unity.com/en-us/grow/levelplay/sdk/unity/package-integration
    public sealed class AdsManager : MonoBehaviour
    {
        [Tooltip("App Key from the LevelPlay platform.")]
        [SerializeField] private string _appKey = "";

        [Tooltip("Optional user ID. Used by Ad Quality User Journey and server-to-server rewards.")]
        [SerializeField] private string _userId = "";

        [SerializeField] private bool _initializeOnStart = true;

        [Tooltip("Enables the LevelPlay integration test suite. Only applies to development builds.")]
        [SerializeField] private bool _enableTestSuite = false;

        private bool _initRequested;

        public static AdsManager Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        public event Action InitSucceeded;
        public event Action<string> InitFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Listeners must be registered before Init so no callback is missed.
            LevelPlay.OnInitSuccess += HandleInitSuccess;
            LevelPlay.OnInitFailed += HandleInitFailed;
        }

        private void Start()
        {
            if (_initializeOnStart)
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            LevelPlay.OnInitSuccess -= HandleInitSuccess;
            LevelPlay.OnInitFailed -= HandleInitFailed;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // Safe to call again after a failure (for example, once the network is back).
        public void Initialize()
        {
            if (IsInitialized || _initRequested)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_appKey))
            {
                Debug.LogError("AdsManager has no LevelPlay App Key assigned; ads will not initialize.", this);
                return;
            }

            _initRequested = true;

            if (_enableTestSuite && Debug.isDebugBuild)
            {
                // Metadata must be set before Init.
                LevelPlay.SetMetaData("is_test_suite", "enable");
            }

            string userId = string.IsNullOrWhiteSpace(_userId) ? null : _userId;
            LevelPlay.Init(_appKey, userId);
        }

        private void HandleInitSuccess(LevelPlayConfiguration configuration)
        {
            _initRequested = false;
            IsInitialized = true;
            Debug.Log("LevelPlay initialized.", this);

            if (_enableTestSuite && Debug.isDebugBuild)
            {
                LevelPlay.LaunchTestSuite();
            }

            InitSucceeded?.Invoke();
        }

        private void HandleInitFailed(LevelPlayInitError error)
        {
            _initRequested = false;
            IsInitialized = false;

            string message = error != null ? error.ErrorMessage : "Unknown error";
            Debug.LogWarning("LevelPlay initialization failed: " + message, this);

            InitFailed?.Invoke(message);
        }
    }
}
