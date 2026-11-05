using NumberMerge.Save;
using NUnit.Framework;
using UnityEngine;

namespace NumberMerge.Gameplay.Tests
{
    public sealed class RechargeTests
    {
        [Test]
        public void Icon_ShownWhenEverythingHolds()
        {
            Assert.IsTrue(RechargeEligibility.ShowsAdIcon(0, 20, 20, true, false, false, true));
        }

        [Test]
        public void Icon_HiddenWhileUsesRemain()
        {
            Assert.IsFalse(RechargeEligibility.ShowsAdIcon(1, 20, 20, true, false, false, true));
        }

        [Test]
        public void Icon_HiddenBeforeTheMoveThreshold()
        {
            Assert.IsFalse(RechargeEligibility.ShowsAdIcon(0, 19, 20, true, false, false, true));
        }

        [Test]
        public void Icon_HiddenWithoutAnAd()
        {
            Assert.IsFalse(RechargeEligibility.ShowsAdIcon(0, 20, 20, false, false, false, true));
        }

        [Test]
        public void Icon_HiddenOnGameOver()
        {
            Assert.IsFalse(RechargeEligibility.ShowsAdIcon(0, 20, 20, true, true, false, true));
        }

        [Test]
        public void Icon_HiddenWhileBusy()
        {
            Assert.IsFalse(RechargeEligibility.ShowsAdIcon(0, 20, 20, true, false, true, true));
        }

        [Test]
        public void Icon_HiddenWhenTheButtonCannotBeUsed()
        {
            // For Undo: no snapshot. For Destroy: not enough tiles.
            Assert.IsFalse(RechargeEligibility.ShowsAdIcon(0, 20, 20, true, false, false, false));
        }

        [Test]
        public void Config_DefaultsToTwentyMoves()
        {
            var config = ScriptableObject.CreateInstance<RechargeConfig>();

            try
            {
                Assert.AreEqual(20, config.MovesPerRecharge);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void BoardState_MovesSinceRecharge_RoundTripsForBoardAndUndo()
        {
            var board = new BoardModel(3, 4);
            board.TrySetValue(0, 0, 2);

            BoardState undo = BoardState.Capture(board, 2, 4, 100);
            undo.RandomState = "7";
            undo.MovesSinceRecharge = 19;

            BoardState state = BoardState.Capture(board, 4, 2, 250);
            state.RandomState = "9";
            state.MovesSinceRecharge = 20;
            state.SetUndoState(undo);

            BoardState loaded = JsonUtility.FromJson<BoardState>(JsonUtility.ToJson(state));

            Assert.AreEqual(20, loaded.MovesSinceRecharge);
            Assert.AreEqual(19, loaded.GetUndoState().MovesSinceRecharge);
        }

        [Test]
        public void BoardState_ClearedUndo_ResetsItsMoveCount()
        {
            var board = new BoardModel(3, 4);
            BoardState state = BoardState.Capture(board, 2, 2, 0);
            state.SetUndoState(BoardState.Capture(board, 3, 3, 0));
            state.UndoMovesSinceRecharge = 5;
            state.SetUndoState(null);

            Assert.AreEqual(0, state.UndoMovesSinceRecharge);
        }
    }
}
