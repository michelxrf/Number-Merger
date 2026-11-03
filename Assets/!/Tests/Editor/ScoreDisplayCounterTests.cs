using System;
using NUnit.Framework;
using NumberMerge.UI;

namespace NumberMerge.UI.Tests
{
    public sealed class ScoreDisplayCounterTests
    {
        [Test]
        public void NewCounter_IsZeroWithNothingPending()
        {
            var counter = new ScoreDisplayCounter();
            Assert.AreEqual(0, counter.Displayed);
            Assert.AreEqual(0, counter.Pending);
        }

        [Test]
        public void SpawnedPoints_AreNotDisplayedUntilTheyArrive()
        {
            var counter = new ScoreDisplayCounter();
            counter.SyncTo(100);
            counter.AddPending(8);

            Assert.AreEqual(100, counter.Displayed);
            Assert.AreEqual(8, counter.Pending);
        }

        [Test]
        public void Arrive_MovesPointsFromPendingToDisplayed()
        {
            var counter = new ScoreDisplayCounter();
            counter.SyncTo(100);
            counter.AddPending(8);

            long displayed = counter.Arrive(8);

            Assert.AreEqual(108, displayed);
            Assert.AreEqual(108, counter.Displayed);
            Assert.AreEqual(0, counter.Pending);
        }

        [Test]
        public void MultipleMerges_ArriveInAnyOrder_EndOnTheSum()
        {
            var counter = new ScoreDisplayCounter();
            counter.SyncTo(10);
            counter.AddPending(4);
            counter.AddPending(16);
            counter.AddPending(2);

            counter.Arrive(16);
            counter.Arrive(2);
            counter.Arrive(4);

            Assert.AreEqual(32, counter.Displayed);
            Assert.AreEqual(0, counter.Pending);
        }

        [Test]
        public void PendingAcrossMoves_StaysConsistent()
        {
            var counter = new ScoreDisplayCounter();
            counter.SyncTo(0);
            counter.AddPending(4);
            counter.Arrive(4);
            counter.AddPending(8);

            Assert.AreEqual(4, counter.Displayed);
            Assert.AreEqual(8, counter.Pending);
        }

        [Test]
        public void SyncTo_ClearsPendingAndSetsScore()
        {
            var counter = new ScoreDisplayCounter();
            counter.SyncTo(0);
            counter.AddPending(50);

            counter.SyncTo(500);

            Assert.AreEqual(500, counter.Displayed);
            Assert.AreEqual(0, counter.Pending);
        }

        [Test]
        public void Arrive_MoreThanPending_Throws()
        {
            var counter = new ScoreDisplayCounter();
            counter.AddPending(4);

            Assert.Throws<InvalidOperationException>(() => counter.Arrive(5));
        }

        [Test]
        public void NegativePoints_Throw()
        {
            var counter = new ScoreDisplayCounter();

            Assert.Throws<ArgumentOutOfRangeException>(() => counter.AddPending(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => counter.SyncTo(-1));
        }
    }
}
