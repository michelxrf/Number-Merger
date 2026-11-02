using System;
using NumberMerge.Save;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace NumberMerge.Localization
{
    // Selects the game language. Each Next() call moves to the next language and saves it as the player's choice.
    // Until the player chooses one, the system language is used (Portuguese, Spanish, otherwise English).
    public static class LanguageManager
    {
        // Locale codes in cycle order. The index matches the flag sprites on the language button.
        public static readonly string[] Order = { "pt-BR", "es", "en" };

        // Index into Order of the selected language. -1 until the language has been applied.
        public static int CurrentIndex { get; private set; } = -1;

        // Raised after the selected locale has changed. Also raised once at startup with the initial language.
        public static event Action<Locale> LanguageChanged;

        private static bool _waitingForInitialization;

        // Applies the saved language, or the system language. Safe to call more than once.
        public static void Initialize()
        {
            if (CurrentIndex >= 0 || _waitingForInitialization)
            {
                return;
            }

            var initialization = LocalizationSettings.InitializationOperation;
            if (initialization.IsDone)
            {
                ApplyStartupLanguage();
            }
            else
            {
                _waitingForInitialization = true;
                initialization.Completed += _ =>
                {
                    _waitingForInitialization = false;
                    ApplyStartupLanguage();
                };
            }
        }

        // Moves to the next language in Order and saves it as the player's preferred language.
        public static void Next()
        {
            int from = CurrentIndex < 0 ? 0 : CurrentIndex;
            SetLanguage(Order[NextIndex(from)], save: true);
        }

        // Pure logic, kept static so it can be tested without the editor.
        public static int NextIndex(int index)
        {
            return (index + 1) % Order.Length;
        }

        // Locale code used when the player has never chosen a language.
        public static string SystemLanguageCode(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Portuguese:
                    return "pt-BR";
                case SystemLanguage.Spanish:
                    return "es";
                default:
                    return "en";
            }
        }

        private static void ApplyStartupLanguage()
        {
            string code;
            if (SaveSystem.TryLoadLanguage(out string saved) && Array.IndexOf(Order, saved) >= 0)
            {
                code = saved;
            }
            else
            {
                code = SystemLanguageCode(Application.systemLanguage);
            }

            // The system default is not saved: the player has not chosen a language yet.
            SetLanguage(code, save: false);
        }

        private static void SetLanguage(string code, bool save)
        {
            int index = Array.IndexOf(Order, code);
            if (index < 0)
            {
                Debug.LogError("[Language] Unknown language code: " + code + ".");
                return;
            }

            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(code);
            if (locale == null)
            {
                Debug.LogError("[Language] No locale asset for " + code + ".");
                return;
            }

            CurrentIndex = index;
            LocalizationSettings.SelectedLocale = locale;

            if (save)
            {
                SaveSystem.SaveLanguage(code);
            }

            LanguageChanged?.Invoke(locale);
        }
    }
}
