using System;
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
        [Tooltip("New Game (\"Novo Jogo\"): starts over with a new board and the default revives.")]
        [SerializeField] private Button _restartButton;
        [Tooltip("Continue (\"Reviver\"): fades the screen out, then removes random tiles. Interactable only while the player has revives.")]
        [SerializeField] private Button _continueButton;

        // Raised after the screen has faded out, when the player chose to continue.
        public event Action ContinueConfirmed;

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

        public void SetTextColor(Color color)
        {
            _scoreText.color = color;
            _recordText.color = color;
        }

        public void Show(long score, long record, int revives)
        {
            _scoreText.text = "Pontos: " + ScoreFormatter.Format(score);
            _recordText.text = "Recorde: " + ScoreFormatter.Format(record);

            _restartButton.interactable = true;
            _continueButton.interactable = revives > 0;

            OverlayFade.FadeIn(_overlay, _panel);
            AudioManager.Instance?.Play(SfxId.GameOver);
        }

        private void OnContinueClicked()
        {
            AudioManager.Instance?.Play(SfxId.ButtonPress);
            TriggerContinue();
        }

        // Fades the screen out, then raises ContinueConfirmed. Used by the Continue button and the debug key.
        public void TriggerContinue()
        {
            // Buttons stay disabled until the screen is shown again, so the revive cannot be triggered twice.
            _restartButton.interactable = false;
            _continueButton.interactable = false;

            OverlayFade.FadeOut(_overlay, _panel, () => ContinueConfirmed?.Invoke());
        }

        // New Game: the saved board is discarded and the revives are reset. No fade, the scene reloads.
        private void Restart()
        {
            SaveSystem.DeleteBoardState();
            SaveSystem.SaveRevives(SaveSystem.DefaultRevives);
            SceneManager.LoadScene(GameplaySceneName);
        }
    }
}
