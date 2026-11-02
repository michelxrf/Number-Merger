using System;
using System.Collections;
using Unity.Services.LevelPlay;
using UnityEngine;

namespace NumberMerge.Ads
{
    // Wraps one LevelPlayRewardedAd. Created by AdsManager after the SDK reports OnInitSuccess.
    // Loads the ad, retries failed loads with backoff, and reports the outcome of each TryShow.
    public sealed class RewardedAdService
    {
        private const string LogPrefix = "[Ads] Rewarded: ";
        private const float MinRetryDelay = 2f;
        private const float MaxRetryDelay = 60f;

        // OnAdRewarded can arrive after OnAdClosed. A close without a reward waits this long before failing.
        private const float RewardGraceSeconds = 2f;

        private readonly MonoBehaviour _coroutineOwner;
        private readonly LevelPlayRewardedAd _ad;

        private bool _isLoading;
        private bool _isShowing;
        private bool _rewardGranted;
        private float _retryDelay = MinRetryDelay;
        private Coroutine _retryRoutine;
        private Coroutine _graceRoutine;
        private Action _onRewarded;
        private Action _onFailed;

        public RewardedAdService(MonoBehaviour coroutineOwner, string adUnitId)
        {
            _coroutineOwner = coroutineOwner;
            _ad = new LevelPlayRewardedAd(adUnitId);

            _ad.OnAdLoaded += HandleLoaded;
            _ad.OnAdLoadFailed += HandleLoadFailed;
            _ad.OnAdDisplayed += HandleDisplayed;
            _ad.OnAdDisplayFailed += HandleDisplayFailed;
            _ad.OnAdRewarded += HandleRewarded;
            _ad.OnAdClosed += HandleClosed;

            Load();
        }

        // True while an ad is on screen or waiting for its reward to resolve.
        public bool IsShowing => _isShowing;

        // True when the SDK reports a loaded ad that can be shown now.
        public bool IsAdLoaded => _ad.IsAdReady();

        // True only when the device is online, an ad is loaded, and no ad is already showing.
        public bool CanShow =>
            !_isShowing &&
            Application.internetReachability != NetworkReachability.NotReachable &&
            IsAdLoaded;

        // Starts a load now if no ad is loaded. Resets the retry backoff and ignores a load that is
        // still marked as pending, because the SDK may never call back while the device is offline.
        public void RequestLoad()
        {
            if (_isShowing || IsAdLoaded)
            {
                return;
            }

            if (_retryRoutine != null)
            {
                _coroutineOwner.StopCoroutine(_retryRoutine);
                _retryRoutine = null;
            }

            _retryDelay = MinRetryDelay;
            _isLoading = false;
            Load();
        }

        // Shows the ad if CanShow. onRewarded runs once the reward is granted.
        // onFailed runs if the ad fails to display or is closed without a reward.
        // Returns false without calling either callback when the ad is not ready.
        public bool TryShow(Action onRewarded, Action onFailed)
        {
            if (_isShowing)
            {
                Log("Show refused: an ad is already showing.");
                return false;
            }

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Log("Show refused: device is offline.");
                return false;
            }

            if (!_ad.IsAdReady())
            {
                Log("Show refused: no ad loaded yet.");
                return false;
            }

            _isShowing = true;
            _rewardGranted = false;
            _onRewarded = onRewarded;
            _onFailed = onFailed;

            // Silence game audio while the ad plays.
            AudioListener.pause = true;
            Log("Showing ad.");
            _ad.ShowAd();
            return true;
        }

        public void Dispose()
        {
            _ad.OnAdLoaded -= HandleLoaded;
            _ad.OnAdLoadFailed -= HandleLoadFailed;
            _ad.OnAdDisplayed -= HandleDisplayed;
            _ad.OnAdDisplayFailed -= HandleDisplayFailed;
            _ad.OnAdRewarded -= HandleRewarded;
            _ad.OnAdClosed -= HandleClosed;
            _ad.Dispose();
            Log("Disposed.");
        }

        private void Load()
        {
            if (_isLoading || _isShowing || _ad.IsAdReady())
            {
                return;
            }

            _isLoading = true;
            Log("Loading ad.");
            _ad.LoadAd();
        }

        private void HandleLoaded(LevelPlayAdInfo info)
        {
            _isLoading = false;
            _retryDelay = MinRetryDelay;
            Log("Ad loaded and ready to show.");
        }

        private void HandleLoadFailed(LevelPlayAdError error)
        {
            _isLoading = false;
            LogWarning("Load failed: " + (error != null ? error.ErrorMessage : "unknown error") + ".");
            ScheduleRetry();
        }

        private void ScheduleRetry()
        {
            if (_retryRoutine == null)
            {
                Log("Retrying load in " + _retryDelay + "s.");
                _retryRoutine = _coroutineOwner.StartCoroutine(RetryLoad());
            }
        }

        private IEnumerator RetryLoad()
        {
            yield return new WaitForSecondsRealtime(_retryDelay);

            _retryDelay = Mathf.Min(_retryDelay * 2f, MaxRetryDelay);
            _retryRoutine = null;
            Load();
        }

        private void HandleDisplayed(LevelPlayAdInfo info)
        {
            Log("Ad displayed on screen.");
        }

        private void HandleDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            LogWarning("Display failed: " + (error != null ? error.ErrorMessage : "unknown error") + ".");

            Action failed = _onFailed;
            FinishShow();
            failed?.Invoke();
            Load();
        }

        private void HandleRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            if (!_isShowing)
            {
                LogWarning("Reward received with no pending request; ignored.");
                return;
            }

            _rewardGranted = true;
            Log("Reward granted" + (reward != null ? " (" + reward.Amount + " " + reward.Name + ")" : "") + ".");

            Action rewarded = _onRewarded;
            _onRewarded = null;
            _onFailed = null;
            rewarded?.Invoke();
        }

        private void HandleClosed(LevelPlayAdInfo info)
        {
            if (_rewardGranted)
            {
                Log("Ad closed after reward.");
                FinishShow();
                Load();
                return;
            }

            Log("Ad closed without reward yet. Waiting " + RewardGraceSeconds + "s for a late reward.");
            if (_graceRoutine == null)
            {
                _graceRoutine = _coroutineOwner.StartCoroutine(ResolveAfterGrace());
            }
        }

        private IEnumerator ResolveAfterGrace()
        {
            yield return new WaitForSecondsRealtime(RewardGraceSeconds);
            _graceRoutine = null;

            if (!_rewardGranted)
            {
                Log("No reward after grace period. Ad not completed.");
                Action failed = _onFailed;
                FinishShow();
                failed?.Invoke();
            }
            else
            {
                Log("Late reward arrived during grace period.");
                FinishShow();
            }

            Load();
        }

        private void FinishShow()
        {
            _isShowing = false;
            _rewardGranted = false;
            _onRewarded = null;
            _onFailed = null;
            AudioListener.pause = false;
        }

        private static void Log(string message)
        {
            Debug.Log(LogPrefix + message);
        }

        private static void LogWarning(string message)
        {
            Debug.LogWarning(LogPrefix + message);
        }
    }
}
