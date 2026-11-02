using NumberMerge.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

namespace NumberMerge.UI
{
    // Applies every LocalizeStringEvent under this object once the localization system is ready,
    // and again whenever the language changes (see LanguageManager).
    public sealed class LocalizedUiRefresher : MonoBehaviour
    {
        private bool _started;

        private void OnEnable()
        {
            LanguageManager.LanguageChanged += OnLanguageChanged;

            // Start runs after every OnEnable in the scene, so the labels' own enable logic cannot overwrite the result.
            if (_started)
            {
                ApplyWhenReady();
            }
        }

        private void Start()
        {
            _started = true;
            ApplyWhenReady();
        }

        private void OnDisable()
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        private void ApplyWhenReady()
        {
            var initialization = LocalizationSettings.InitializationOperation;
            if (initialization.IsDone)
            {
                ApplyAll(null);
            }
            else
            {
                initialization.Completed += _ =>
                {
                    if (this != null && isActiveAndEnabled)
                    {
                        ApplyAll(null);
                    }
                };
            }
        }

        private void OnLanguageChanged(Locale locale)
        {
            ApplyAll(locale);
        }

        private void ApplyAll(Locale locale)
        {
            foreach (LocalizeStringEvent text in GetComponentsInChildren<LocalizeStringEvent>(true))
            {
                UiText.Apply(text, locale);
            }
        }
    }
}
