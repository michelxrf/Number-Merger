using NumberMerge.Audio;
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
        [SerializeField] private Button _restartButton;

        private void Awake()
        {
            _restartButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.Play(SfxId.ButtonPress);
                Restart();
            });
            _overlay.SetActive(false);
        }

        public void SetTextColor(Color color)
        {
            _scoreText.color = color;
            _recordText.color = color;
        }

        public void Show(long score, long record)
        {
            _scoreText.text = "Pontos: " + ScoreFormatter.Format(score);
            _recordText.text = "Recorde: " + ScoreFormatter.Format(record);
            OverlayFade.FadeIn(_overlay, _panel);
            AudioManager.Instance?.Play(SfxId.GameOver);
        }

        private void Restart()
        {
            SceneManager.LoadScene(GameplaySceneName);
        }
    }
}
