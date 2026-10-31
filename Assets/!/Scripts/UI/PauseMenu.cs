using NumberMerge.Audio;
using NumberMerge.Save;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NumberMerge.UI
{
    public sealed class PauseMenu : MonoBehaviour
    {
        private const string GameplaySceneName = "Gameplay";

        [Tooltip("Theme whose Text Color is applied to every text in the pause overlay.")]
        [SerializeField] private TileTheme _theme;
        [SerializeField] private GameObject _overlay;
        [Tooltip("Panel inside the overlay. It is scaled during the fade; the overlay itself is not.")]
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;

        public static bool IsPaused { get; private set; }

        private void Awake()
        {
            // Never start a scene frozen, even if the previous scene was paused.
            Time.timeScale = 1f;
            IsPaused = false;

            _pauseButton.onClick.AddListener(() => { PlayButtonSound(); Pause(); });
            _closeButton.onClick.AddListener(() => { PlayButtonSound(); Resume(); });
            _restartButton.onClick.AddListener(() => { PlayButtonSound(); Restart(); });
            _musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            _sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

            ApplyTextColor();
            _overlay.SetActive(false);
        }

        // Covers every text under the overlay, including labels of buttons.
        private void ApplyTextColor()
        {
            if (_theme == null)
            {
                return;
            }

            foreach (TMP_Text text in _overlay.GetComponentsInChildren<TMP_Text>(true))
            {
                text.color = _theme.TextColor;
            }
        }

        private void OnDestroy()
        {
            IsPaused = false;
        }

        public void Pause()
        {
            if (IsPaused)
            {
                return;
            }

            IsPaused = true;
            Time.timeScale = 0f;
            RefreshSliders();
            OverlayFade.FadeIn(_overlay, _panel);
        }

        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            Time.timeScale = 1f;
            OverlayFade.FadeOut(_overlay, _panel);
        }

        public void Restart()
        {
            // Wipe the board in progress and reset the revives; the record is kept.
            SaveSystem.DeleteBoardState();
            SaveSystem.SaveRevives(SaveSystem.DefaultRevives);

            IsPaused = false;
            Time.timeScale = 1f;

            // Prevent a second click from queueing another scene load during the fade.
            _restartButton.interactable = false;
            OverlayFade.FadeOut(_overlay, _panel, () => SceneManager.LoadScene(GameplaySceneName));
        }

        private static void PlayButtonSound()
        {
            AudioManager.Instance?.Play(SfxId.ButtonPress);
        }

        public void OnMusicVolumeChanged(float value)
        {
            AudioManager.Instance?.SetVolume(AudioChannel.Music, value);
        }

        public void OnSfxVolumeChanged(float value)
        {
            AudioManager.Instance?.SetVolume(AudioChannel.Sfx, value);
        }

        private void RefreshSliders()
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            // Setting the value fires onValueChanged, which writes the same volume back to the mixer.
            _musicSlider.SetValueWithoutNotify(AudioManager.Instance.GetVolume(AudioChannel.Music));
            _sfxSlider.SetValueWithoutNotify(AudioManager.Instance.GetVolume(AudioChannel.Sfx));
        }
    }
}
