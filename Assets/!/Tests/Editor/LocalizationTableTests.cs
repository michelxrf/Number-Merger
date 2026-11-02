using NUnit.Framework;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace NumberMerge.Localization.Tests
{
    public sealed class LocalizationTableTests
    {
        private static readonly string[] Locales = { "en", "es", "pt-BR" };
        private const string CollectionPath = "Assets/!/Localization/Tables/UI.asset";

        [Test]
        public void UiCollection_Exists()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<StringTableCollection>(CollectionPath));
        }

        [Test]
        public void UiCollection_HasEveryLocaleTable()
        {
            StringTableCollection collection = AssetDatabase.LoadAssetAtPath<StringTableCollection>(CollectionPath);
            Assert.IsNotNull(collection);

            foreach (string locale in Locales)
            {
                Assert.IsNotNull(collection.GetTable(locale), "Missing table for " + locale);
            }
        }

        [Test]
        public void EveryKey_HasNonEmptyValueInEveryLocale()
        {
            StringTableCollection collection = AssetDatabase.LoadAssetAtPath<StringTableCollection>(CollectionPath);
            Assert.IsNotNull(collection);

            foreach (var key in collection.SharedData.Entries)
            {
                foreach (string locale in Locales)
                {
                    StringTable table = (StringTable)collection.GetTable(locale);
                    StringTableEntry entry = table.GetEntry(key.Id);

                    Assert.IsNotNull(entry, "Missing " + key.Key + " in " + locale);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Value), "Empty " + key.Key + " in " + locale);
                }
            }
        }
    }
}
