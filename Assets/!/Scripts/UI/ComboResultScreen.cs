using System.Collections;
using NumberMerge.Audio;
using TMPro;
using UnityEngine;

namespace NumberMerge.UI
{
    // Shows the combo count ("5x") in the center of the screen after a move's result phase.
    // Fades and scales in, holds, then fades out. Uses unscaled time so it still runs while paused.
    public sealed class ComboResultScreen : MonoBehaviour
    {
        [SerializeField] private ComboResultConfig _config;
        [Tooltip("Root object with the CanvasGroup. Activated while the screen is shown.")]
        [SerializeField] private GameObject _overlay;
        [SerializeField] private CanvasGroup _canvasGroup;
        [Tooltip("Scaled object that holds the image and the text.")]
        [SerializeField] private RectTransform _content;
        [SerializeField] private TMP_Text _countText;

        private Coroutine _sequence;

        private void Awake()
        {
            _overlay.SetActive(false);
        }

        public void SetTextColor(Color color)
        {
            _countText.color = color;
        }

        public void Show(int comboCount)
        {
            if (_config == null || comboCount < _config.MinCombosToShow)
            {
                return;
            }

            if (_sequence != null)
            {
                StopCoroutine(_sequence);
                LeanTween.cancel(_canvasGroup.gameObject);
                LeanTween.cancel(_content.gameObject);
            }

            _countText.text = comboCount + 1 + "x";
            _overlay.SetActive(true);
            _canvasGroup.alpha = 0f;
            _content.localScale = Vector3.one * _config.StartScale;

            AudioManager.Instance?.PlayComboSfx(_config.GetComboPitch(comboCount));
            _sequence = StartCoroutine(RunSequence());
        }

        private IEnumerator RunSequence()
        {
            float fadeIn = _config.FadeInDuration;
            LeanTween.alphaCanvas(_canvasGroup, 1f, fadeIn).setIgnoreTimeScale(true);
            LeanTween.scale(_content.gameObject, Vector3.one * _config.EndScale, fadeIn)
                .setIgnoreTimeScale(true)
                .setEase(LeanTweenType.easeOutBack);
            yield return new WaitForSecondsRealtime(fadeIn);

            yield return new WaitForSecondsRealtime(_config.HoldDuration);

            float fadeOut = _config.FadeOutDuration;
            LeanTween.alphaCanvas(_canvasGroup, 0f, fadeOut).setIgnoreTimeScale(true);
            yield return new WaitForSecondsRealtime(fadeOut);

            _overlay.SetActive(false);
            _sequence = null;
        }
    }
}
