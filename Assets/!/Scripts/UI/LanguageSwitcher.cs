using NumberMerge.Audio;
using NumberMerge.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace NumberMerge.UI
{
    // Language button. Each press moves to the next language and shows its flag.
    public sealed class LanguageSwitcher : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _image;

        [Tooltip("Flag for each language, in LanguageManager.Order: pt-BR, es, en.")]
        [SerializeField] private Sprite[] _flags;

        private void Awake()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void OnEnable()
        {
            LanguageManager.LanguageChanged += OnLanguageChanged;

            if (LanguageManager.CurrentIndex >= 0)
            {
                ShowFlag(LanguageManager.CurrentIndex);
            }
        }

        private void OnDisable()
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        private void Start()
        {
            LanguageManager.Initialize();
        }

        private void OnClicked()
        {
            AudioManager.Instance?.Play(SfxId.ButtonPress);
            LanguageManager.Next();
        }

        private void OnLanguageChanged(Locale locale)
        {
            ShowFlag(LanguageManager.CurrentIndex);
        }

        private void ShowFlag(int index)
        {
            if (_image != null && index >= 0 && index < _flags.Length && _flags[index] != null)
            {
                _image.sprite = _flags[index];
            }
        }
    }
}
