using System.Globalization;
using UnityEngine.Localization.Settings;

namespace NumberMerge.UI
{
    // Formats score values for display. Digit grouping (thousands, millions, ...) follows the selected locale,
    // so the separator matches the language the player sees.
    public static class ScoreFormatter
    {
        public static string Format(long value)
        {
            return value.ToString("N0", GetCulture());
        }

        private static CultureInfo GetCulture()
        {
            // Null before the localization system has initialized: fall back to the device culture.
            var locale = LocalizationSettings.SelectedLocale;
            return locale != null && locale.Identifier.CultureInfo != null
                ? locale.Identifier.CultureInfo
                : CultureInfo.CurrentCulture;
        }
    }
}
