using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

namespace NumberMerge.UI
{
    // Applies every LocalizeStringEvent under this object once the localization system is ready,
    // and again whenever the selected locale changes.
    public sealed class LocalizedUiRefresher : MonoBehaviour
    {
        private bool _started;

        private void OnEnable()
        {
            Subscribe();

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
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        // Subscribes again after initialization as well: the locale event did not reach this object when it
        // subscribed before the localization system finished starting.
        private void ApplyWhenReady()
        {
            Subscribe();

            var initialization = LocalizationSettings.InitializationOperation;
            if (initialization.IsDone)
            {
                ApplyAll();
            }
            else
            {
                initialization.Completed += _ =>
                {
                    if (this != null && isActiveAndEnabled)
                    {
                        Subscribe();
                        ApplyAll();
                    }
                };
            }
        }

        private void Subscribe()
        {
            // Removing first keeps a single subscription no matter how often this runs.
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        private void OnLocaleChanged(Locale locale)
        {
            ApplyAll(locale);
        }

        private void ApplyAll(Locale locale = null)
        {
            foreach (LocalizeStringEvent text in GetComponentsInChildren<LocalizeStringEvent>(true))
            {
                UiText.Apply(text, locale);
            }
        }
    }
}
