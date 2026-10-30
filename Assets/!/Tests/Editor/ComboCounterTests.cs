using NUnit.Framework;
using NumberMerge.Managers;

namespace NumberMerge.Managers.Tests
{
    public sealed class ComboCounterTests
    {
        [Test]
        public void NewCounter_IsZero()
        {
            Assert.AreEqual(0, new ComboCounter().Count);
        }

        [Test]
        public void SingleMerge_IsNotACombo()
        {
            var counter = new ComboCounter();
            counter.AddCascade(1);
            Assert.AreEqual(0, counter.Count);
        }

        [Test]
        public void ZeroMerges_IsNotACombo()
        {
            var counter = new ComboCounter();
            counter.AddCascade(0);
            Assert.AreEqual(0, counter.Count);
        }

        [Test]
        public void MergesAfterTheFirst_AreCombos()
        {
            var counter = new ComboCounter();
            counter.AddCascade(3);
            Assert.AreEqual(2, counter.Count);
        }

        [Test]
        public void Reset_ClearsCount()
        {
            var counter = new ComboCounter();
            counter.AddCascade(4);
            counter.Reset();
            Assert.AreEqual(0, counter.Count);
        }

        [Test]
        public void NegativeMergeCount_Throws()
        {
            var counter = new ComboCounter();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => counter.AddCascade(-1));
        }
    }
}
