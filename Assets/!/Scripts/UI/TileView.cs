using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NumberMerge.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class TileView : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private CanvasGroup _canvasGroup;

        public void SetValue(int value, TileTheme theme)
        {
            if (_background == null)
            {
                _background = GetComponent<Image>();
            }

            if (_label == null)
            {
                _label = GetComponentInChildren<TMP_Text>(true);
            }

            _label.text = value.ToString();

            if (theme == null)
            {
                return;
            }

            // The sprite carries the color, so the image stays white to show it unchanged.
            _background.sprite = theme.GetPieceSprite(value);
            _background.color = Color.white;
            _label.color = theme.TextColor;
        }

        public void SetScale(float scale)
        {
            transform.localScale = Vector3.one * scale;
        }

        public void SetAlpha(float alpha)
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.Clamp01(alpha);
            }
        }

        // Multiplies the tile's background by this color. White shows the tile unchanged.
        public void SetTint(Color color)
        {
            if (_background == null)
            {
                _background = GetComponent<Image>();
            }

            if (_background != null)
            {
                _background.color = color;
            }
        }

        public void ResetVisual()
        {
            SetScale(1f);
            SetAlpha(1f);
            SetTint(Color.white);
        }
    }
}
