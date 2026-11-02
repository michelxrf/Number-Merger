using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NumberMerge.UI
{
    // Points a LocalizeStringEvent at a key of the "UI" string table and applies it.
    // Arguments are the smart-format values for the placeholders ({0}); pass none for static text.
    public static class UiText
    {
        public const string TableName = "UI";

        // Latest request per label. A slower lookup for an older key must not overwrite a newer one.
        private static readonly Dictionary<LocalizeStringEvent, int> Requests = new Dictionary<LocalizeStringEvent, int>();

        public static void SetKey(LocalizeStringEvent text, string key, params object[] arguments)
        {
            if (text == null)
            {
                return;
            }

            text.StringReference.SetReference(TableName, key);
            text.StringReference.Arguments = arguments != null && arguments.Length > 0 ? arguments : null;
            Apply(text);
        }

        // Resolves the label's key in the current locale and writes the result to the label.
        // Used instead of LocalizeStringEvent.RefreshString, which does not update the label in this project.
        // locale: the locale to resolve in. Null uses the selected locale.
        public static void Apply(LocalizeStringEvent text, Locale locale = null)
        {
            if (text == null || text.StringReference.IsEmpty)
            {
                return;
            }

            // Before the localization system has a selected locale there is nothing to resolve yet.
            // The refresher applies every label again once it is ready.
            if (locale == null && LocalizationSettings.SelectedLocale == null)
            {
                return;
            }

            int request = Requests.TryGetValue(text, out int previous) ? previous + 1 : 1;
            Requests[text] = request;

            // The selected locale can still be the previous one during a locale change event, so pass the locale explicitly.
            AsyncOperationHandle<string> operation;
            if (locale != null)
            {
                object[] arguments = text.StringReference.Arguments as object[] ?? new object[0];
                operation = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                    text.StringReference.TableReference,
                    text.StringReference.TableEntryReference,
                    locale,
                    FallbackBehavior.UseProjectSettings,
                    arguments);
            }
            else
            {
                operation = text.StringReference.GetLocalizedStringAsync();
            }
            operation.Completed += handle =>
            {
                if (text == null || Requests[text] != request || handle.Status != AsyncOperationStatus.Succeeded)
                {
                    return;
                }

                text.OnUpdateString.Invoke(handle.Result);
            };
        }
    }
}
