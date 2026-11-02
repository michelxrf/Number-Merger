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
        private const string LogPrefix = "[Ads] ";

        [Tooltip("App Key from the LevelPlay platform.")]
        [SerializeField] private string _appKey = "";

        [Tooltip("Optional user ID. Used by Ad Quality User Journey and server-to-server rewards.")]
        [SerializeField] private string _userId = "";

        [Tooltip("Rewarded Video Ad Unit ID from the LevelPlay dashboard.")]
        [SerializeField] private string _rewardedAdUnitId = "287zkhtwhpg6cgee";

        [SerializeField] private bool _initializeOnStart = true;

        [Tooltip("Enables the LevelPlay integration test suite. Only applies to development builds.")]
        [SerializeField] private bool _enableTestSuite = false;

        private bool _initRequested;

        public static AdsManager Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        // Null until the SDK reports OnInitSuccess.
        public RewardedAdService Rewarded { get; private set; }

        public event Action InitSucceeded;
        public event Action<string> InitFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Log("Duplicate AdsManager destroyed.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Listeners must be registered before Init so no callback is missed.
            LevelPlay.OnInitSuccess += HandleInitSuccess;
            LevelPlay.OnInitFailed += HandleInitFailed;
            Log("AdsManager awake. Init on start: " + _initializeOnStart + ", test suite: " + _enableTestSuite + ".");
        }

        private void Start()
        {

            if (_initializeOnStart)
            {
                Initialize();
            }
            else
            {
                Log("Init skipped: _initializeOnStart is off. Call Initialize() manually.");
            }
        }

        private void OnDestroy()
        {
            LevelPlay.OnInitSuccess -= HandleInitSuccess;
            LevelPlay.OnInitFailed -= HandleInitFailed;

            Rewarded?.Dispose();
            Rewarded = null;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // Safe to call again after a failure (for example, once the network is back).
        public void Initialize()
        {
            if (IsInitialized)
            {
                Log("Init skipped: already initialized.");
                return;
            }

            if (_initRequested)
            {
                Log("Init skipped: an init request is already in progress.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_appKey))
            {
                LogError("No LevelPlay App Key assigned; ads will not initialize.");
                return;
            }

            _initRequested = true;
            Log("Init requested. Internet: " + Application.internetReachability + ".");

            // The SDK can report success synchronously (the Editor does), so the listeners must be
            // registered right before Init. The SDK ignores duplicate registrations.
            LevelPlay.OnInitSuccess += HandleInitSuccess;
            LevelPlay.OnInitFailed += HandleInitFailed;

            if (_enableTestSuite && Debug.isDebugBuild)
            {
                // Metadata must be set before Init.
                LevelPlay.SetMetaData("is_test_suite", "enable");
                Log("Test suite metadata set (debug build).");
            }

            string userId = string.IsNullOrWhiteSpace(_userId) ? null : _userId;
            LevelPlay.Init(_appKey, userId);
            Log("LevelPlay.Init called. Waiting for SDK callback.");
        }

        private void HandleInitSuccess(LevelPlayConfiguration configuration)
        {
            _initRequested = false;
            IsInitialized = true;
            Log("Init succeeded.");

            if (_enableTestSuite && Debug.isDebugBuild)
            {
                LevelPlay.LaunchTestSuite();
                Log("Test suite launched.");
            }

            // Rewarded ad objects must be created after OnInitSuccess.
            if (Rewarded == null)
            {
                Rewarded = new RewardedAdService(this, _rewardedAdUnitId);
                Log("Rewarded service created for ad unit " + _rewardedAdUnitId + ".");
            }

            InitSucceeded?.Invoke();
        }

        private void HandleInitFailed(LevelPlayInitError error)
        {
            _initRequested = false;
            IsInitialized = false;

            string message = error != null ? error.ErrorMessage : "Unknown error";
            LogWarning("Init failed: " + message + ". Ads are unavailable until Initialize() succeeds.");

            InitFailed?.Invoke(message);
        }

        private static void Log(string message)
        {
            Debug.Log(LogPrefix + message);
        }

        private static void LogWarning(string message)
        {
            Debug.LogWarning(LogPrefix + message);
        }

        private static void LogError(string message)
        {
            Debug.LogError(LogPrefix + message);
        }
    }
}
