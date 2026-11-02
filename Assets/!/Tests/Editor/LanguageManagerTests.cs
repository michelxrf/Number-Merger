using NUnit.Framework;
using NumberMerge.Localization;
using UnityEngine;

namespace NumberMerge.Localization.Tests
{
    public sealed class LanguageManagerTests
    {
        [Test]
        public void NextIndex_CyclesThroughEveryLanguageAndWraps()
        {
            Assert.AreEqual(1, LanguageManager.NextIndex(0));
            Assert.AreEqual(2, LanguageManager.NextIndex(1));
            Assert.AreEqual(0, LanguageManager.NextIndex(2));
        }

        [Test]
        public void Order_ContainsTheThreeLanguagesInFlagOrder()
        {
            CollectionAssert.AreEqual(new[] { "pt-BR", "es", "en" }, LanguageManager.Order);
        }

        [Test]
        public void SystemLanguageCode_MapsPortugueseAndSpanish()
        {
            Assert.AreEqual("pt-BR", LanguageManager.SystemLanguageCode(SystemLanguage.Portuguese));
            Assert.AreEqual("es", LanguageManager.SystemLanguageCode(SystemLanguage.Spanish));
        }

        [Test]
        public void SystemLanguageCode_FallsBackToEnglishForOtherLanguages()
        {
            Assert.AreEqual("en", LanguageManager.SystemLanguageCode(SystemLanguage.English));
            Assert.AreEqual("en", LanguageManager.SystemLanguageCode(SystemLanguage.French));
            Assert.AreEqual("en", LanguageManager.SystemLanguageCode(SystemLanguage.Unknown));
        }
    }
}
