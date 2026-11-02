using System;
using NumberMerge.Ads;
using NumberMerge.Audio;
using NumberMerge.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace NumberMerge.UI
{
    public sealed class GameOverScreen : MonoBehaviour
    {
        private const string GameplaySceneName = "Gameplay";

        [SerializeField] private GameObject _overlay;
        [Tooltip("Panel inside the overlay. It is scaled during the fade; the overlay itself is not.")]
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _recordText;
        [Tooltip("New Game (\"Novo Jogo\"): starts over with a new board.")]
        [SerializeField] private Button _restartButton;
        [Tooltip("Continue (\"Reviver\"): watches a rewarded ad. The revive runs only if the reward is granted. " +
                 "Interactable only when online and a rewarded ad is loaded.")]
        [SerializeField] private Button _continueButton;
        [Tooltip("Ad icon shown on the Continue button. Hidden whenever the button is not interactable.")]
        [SerializeField] private GameObject _adIcon;

        // Raised after the screen has faded out, when the player has watched the ad (or from the debug key).
        public event Action ContinueConfirmed;

        // True while the game over screen is visible. Buttons are refreshed only then.
        private bool _isShown;

        // True while a rewarded ad is on screen or the revive is running. Blocks both buttons.
        private bool _isBusy;

        // Revives left. The ad button is enabled only while at least one is left.
        private int _revives;

        private void Awake()
        {
            _restartButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.Play(SfxId.ButtonPress);
                Restart();
            });

            _continueButton.onClick.AddListener(OnContinueClicked);
            _overlay.SetActive(false);
        }

        private void Update()
        {
            // Network and ad availability can change while the screen is open.
            if (_isShown)
            {
                RefreshButtons();
            }
        }

        // Covers every text under the overlay, including the labels of the buttons.
        public void SetTextColor(Color color)
        {
            foreach (TMP_Text text in _overlay.GetComponentsInChildren<TMP_Text>(true))
            {
                text.color = color;
            }

            _scoreText.color = color;
            _recordText.color = color;
        }

        public void Show(long score, long record, int revives)
        {
            _scoreText.text = "Pontos: " + ScoreFormatter.Format(score);
            _recordText.text = "Recorde: " + ScoreFormatter.Format(record);

            _revives = revives;
            _isBusy = false;
            _isShown = true;

            AdsManager ads = AdsManager.Instance;
            RewardedAdService rewarded = ads != null ? ads.Rewarded : null;
            // Try to load now, so an ad that failed or was never loaded is ready when the player taps Continue.
            rewarded?.RequestLoad();

            Debug.Log("[Ads] Continue check at game over: revives=" + revives +
                      ", sdkInitialized=" + (ads != null && ads.IsInitialized) +
                      ", rewardedService=" + (rewarded != null) +
                      ", adLoaded=" + (rewarded != null && rewarded.IsAdLoaded) +
                      ", online=" + (Application.internetReachability != NetworkReachability.NotReachable) +
                      ", enabled=" + (revives > 0 && rewarded != null && rewarded.CanShow) + ".");

            RefreshButtons();

            OverlayFade.FadeIn(_overlay, _panel);
            AudioManager.Instance?.Play(SfxId.GameOver);
        }

        private void RefreshButtons()
        {
            bool adAvailable = AdsManager.Instance != null &&
                               AdsManager.Instance.Rewarded != null &&
                               AdsManager.Instance.Rewarded.CanShow;

            bool canContinue = !_isBusy && _revives > 0 && adAvailable;

            _continueButton.interactable = canContinue;
            _restartButton.interactable = !_isBusy;

            if (_adIcon != null)
            {
                _adIcon.SetActive(canContinue);
            }
        }

        private void OnContinueClicked()
        {
            if (_isBusy)
            {
                return;
            }

            AudioManager.Instance?.Play(SfxId.ButtonPress);
            ShowRewardedAd();
        }

        // Shows the rewarded ad. The revive starts only from the reward callback.
        private void ShowRewardedAd()
        {
            RewardedAdService rewarded = AdsManager.Instance != null ? AdsManager.Instance.Rewarded : null;
            if (rewarded == null)
            {
                return;
            }

            _isBusy = true;
            RefreshButtons();

            bool started = rewarded.TryShow(onRewarded: TriggerContinue, onFailed: OnAdNotCompleted);
            if (!started)
            {
                OnAdNotCompleted();
            }
        }

        // The ad did not finish, so the player keeps the game over screen with the buttons enabled again.
        private void OnAdNotCompleted()
        {
            _isBusy = false;
            RefreshButtons();
        }

        // Fades the screen out, then raises ContinueConfirmed. Called after a rewarded ad, and by the debug key.
        public void TriggerContinue()
        {
            // Buttons stay disabled until the screen is shown again, so the revive cannot be triggered twice.
            _isShown = false;
            _isBusy = true;
            RefreshButtons();

            OverlayFade.FadeOut(_overlay, _panel, () => ContinueConfirmed?.Invoke());
        }

        // New Game: the saved board is discarded. No fade, the scene reloads.
        private void Restart()
        {
            SaveSystem.DeleteBoardState();
            SaveSystem.SaveRevives(SaveSystem.DefaultRevives);
            SceneManager.LoadScene(GameplaySceneName);
        }
    }
}
