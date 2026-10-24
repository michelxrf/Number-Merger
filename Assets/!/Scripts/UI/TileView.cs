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

        public void SetValue(int value)
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
