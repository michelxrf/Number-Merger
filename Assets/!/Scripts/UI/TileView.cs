using UnityEngine;
using UnityEngine.UI;

namespace NumberMerge.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class TileView : MonoBehaviour
    {
        private static readonly Color[] Palette =
        {
            new Color(0.22f, 0.49f, 0.80f),
            new Color(0.20f, 0.62f, 0.56f),
            new Color(0.36f, 0.67f, 0.29f),
            new Color(0.80f, 0.66f, 0.20f),
            new Color(0.88f, 0.48f, 0.20f),
            new Color(0.82f, 0.32f, 0.32f),
            new Color(0.61f, 0.36f, 0.75f),
            new Color(0.31f, 0.39f, 0.55f)
        };

        [SerializeField] private Image _background;
        [SerializeField] private Text _label;
        [SerializeField] private CanvasGroup _canvasGroup;

        public void SetValue(int value)
        {
            if (_background == null)
            {
                _background = GetComponent<Image>();
            }

            if (_label == null)
            {
                _label = GetComponentInChildren<Text>(true);
            }

            _label.text = value.ToString();
            _background.color = Palette[Mathf.Abs(value) % Palette.Length];
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

        public void ResetVisual()
        {
            SetScale(1f);
            SetAlpha(1f);
        }
    }
}
