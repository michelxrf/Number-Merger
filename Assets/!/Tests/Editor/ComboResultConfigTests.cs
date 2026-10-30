using NUnit.Framework;
using NumberMerge.UI;
using UnityEngine;

namespace NumberMerge.UI.Tests
{
    public sealed class ComboResultConfigTests
    {
        private ComboResultConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<ComboResultConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Defaults_MatchSpec()
        {
            Assert.AreEqual(0.5f, _config.FadeInDuration);
            Assert.AreEqual(1f, _config.HoldDuration);
            Assert.AreEqual(0.5f, _config.FadeOutDuration);
            Assert.AreEqual(2, _config.MinCombosToShow);
        }

        [Test]
        public void Pitch_IsNormalBelowStartCount()
        {
            Assert.AreEqual(1f, _config.GetComboPitch(0));
            Assert.AreEqual(1f, _config.GetComboPitch(1));
            Assert.AreEqual(1f, _config.GetComboPitch(2));
        }

        [Test]
        public void Pitch_RisesByGainFromStartCount()
        {
            Assert.AreEqual(1f + _config.ChainPitchGain, _config.GetComboPitch(3), 0.0001f);
            Assert.AreEqual(1f + 2f * _config.ChainPitchGain, _config.GetComboPitch(4), 0.0001f);
        }

        [Test]
        public void Pitch_IsCappedAtMaxPitch()
        {
            Assert.AreEqual(_config.MaxPitch, _config.GetComboPitch(1000), 0.0001f);
        }
    }
}
