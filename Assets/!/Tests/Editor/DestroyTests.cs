using NumberMerge.Save;
using NUnit.Framework;
using UnityEngine;

namespace NumberMerge.Gameplay.Tests
{
    public sealed class DestroyTests
    {
        [Test]
        public void Eligibility_AllowedWhenUsesLeftAndBoardHasEnoughTiles()
        {
            Assert.IsTrue(DestroyEligibility.CanDestroy(2, false, false, 10, 1));
        }

        [Test]
        public void Eligibility_RejectedWithoutUsesLeft()
        {
            Assert.IsFalse(DestroyEligibility.CanDestroy(0, false, false, 10, 1));
        }

        [Test]
        public void Eligibility_RejectedOnGameOver()
        {
            Assert.IsFalse(DestroyEligibility.CanDestroy(2, true, false, 10, 1));
        }

        [Test]
        public void Eligibility_RejectedWhileBusy()
        {
            Assert.IsFalse(DestroyEligibility.CanDestroy(2, false, true, 10, 1));
        }

        [Test]
        public void Eligibility_RequiresTheMinimumTileCount()
        {
            // Revive minimum of 3: two tiles is not enough, three is.
            Assert.IsFalse(DestroyEligibility.CanDestroy(2, false, false, 2, 3));
            Assert.IsTrue(DestroyEligibility.CanDestroy(2, false, false, 3, 3));
        }

        [Test]
        public void Eligibility_EmptyBoardIsRejected()
        {
            Assert.IsFalse(DestroyEligibility.CanDestroy(2, false, false, 0, 1));
        }

        [Test]
        public void Config_DefaultsToTwoUses()
        {
            var config = ScriptableObject.CreateInstance<DestroyConfig>();

            try
            {
                Assert.AreEqual(2, config.MaxUses);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void SaveSystem_DestroyUses_RoundTripAndFallBackAfterDelete()
        {
            try
            {
                SaveSystem.DeleteDestroyUses();
                Assert.AreEqual(2, SaveSystem.LoadDestroyUses(2));

                SaveSystem.SaveDestroyUses(1);
                Assert.AreEqual(1, SaveSystem.LoadDestroyUses(2));

                SaveSystem.DeleteDestroyUses();
                Assert.AreEqual(2, SaveSystem.LoadDestroyUses(2));
            }
            finally
            {
                SaveSystem.DeleteDestroyUses();
            }
        }

        [Test]
        public void SaveSystem_UndoUsesAndDestroyUses_AreIndependent()
        {
            try
            {
                SaveSystem.DeleteUndoUses();
                SaveSystem.DeleteDestroyUses();

                SaveSystem.SaveDestroyUses(0);

                Assert.AreEqual(5, SaveSystem.LoadUndoUses(5));
            }
            finally
            {
                SaveSystem.DeleteUndoUses();
                SaveSystem.DeleteDestroyUses();
            }
        }
    }
}
