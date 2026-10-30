using System.Globalization;

namespace NumberMerge.UI
{
    // Formats score values for display. Digit grouping (thousands, millions, ...) follows the current culture,
    // so the separator is correct for each language instead of being hardcoded.
    public static class ScoreFormatter
    {
        public static string Format(long value)
        {
            return value.ToString("N0", CultureInfo.CurrentCulture);
        }
    }
}
