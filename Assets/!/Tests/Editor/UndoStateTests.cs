using System;
using NumberMerge.Save;
using NUnit.Framework;
using UnityEngine;

namespace NumberMerge.Gameplay.Tests
{
    public sealed class UndoStateTests
    {
        [Test]
        public void SeededRandom_SameStateGivesSameSequence()
        {
            var a = new SeededRandomSource(42);
            var b = new SeededRandomSource(42);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(a.Next(1000), b.Next(1000));
            }
        }

        [Test]
        public void SeededRandom_ResumingFromSavedStateRepeatsTheSequence()
        {
            var random = new SeededRandomSource(7);
            for (int i = 0; i < 10; i++)
            {
                random.Next(100);
            }

            ulong saved = random.State;

            var expected = new int[20];
            for (int i = 0; i < expected.Length; i++)
            {
                expected[i] = random.Next(100);
            }

            random.State = saved;

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], random.Next(100));
            }
        }

        [Test]
        public void SeededRandom_StaysWithinRange()
        {
            var random = new SeededRandomSource(99);

            for (int i = 0; i < 2000; i++)
            {
                int value = random.Next(7);
                Assert.That(value, Is.InRange(0, 6));
            }
        }

        [Test]
        public void SeededRandom_RejectsNonPositiveMaximum()
        {
            var random = new SeededRandomSource(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => random.Next(0));
        }

        [Test]
        public void BoardState_RoundTripsThroughJson_WithNestedUndoState()
        {
            var board = new BoardModel(3, 4);
            board.TrySetValue(0, 0, 2);
            board.TrySetValue(1, 0, 4);

            BoardState undo = BoardState.Capture(board, 2, 4, 100);
            undo.RandomState = "123";

            BoardState state = BoardState.Capture(board, 4, 2, 250);
            state.RandomState = "999";
            state.SetUndoState(undo);

            BoardState loaded = JsonUtility.FromJson<BoardState>(JsonUtility.ToJson(state));

            Assert.IsTrue(loaded.TryGetRandomState(out ulong randomState));
            Assert.AreEqual(999UL, randomState);

            BoardState loadedUndo = loaded.GetUndoState();
            Assert.IsNotNull(loadedUndo);
            Assert.IsTrue(loadedUndo.TryGetRandomState(out ulong undoRandomState));
            Assert.AreEqual(123UL, undoRandomState);

            Assert.IsTrue(loadedUndo.TryRestore(out BoardModel restored, out long score));
            Assert.AreEqual(100L, score);
            Assert.AreEqual(2, restored.GetValue(0, 0));
            Assert.AreEqual(4, restored.GetValue(1, 0));
            Assert.AreEqual(2, loadedUndo.CurrentPiece);
            Assert.AreEqual(4, loadedUndo.NextPiece);
        }

        [Test]
        public void BoardState_WithoutUndo_ReturnsNullUndoState()
        {
            var board = new BoardModel(3, 4);
            BoardState state = BoardState.Capture(board, 2, 2, 0);
            state.RandomState = "5";

            BoardState loaded = JsonUtility.FromJson<BoardState>(JsonUtility.ToJson(state));

            Assert.IsNull(loaded.GetUndoState());
        }

        [Test]
        public void BoardState_ClearingUndo_RemovesItFromTheSave()
        {
            var board = new BoardModel(3, 4);
            BoardState state = BoardState.Capture(board, 2, 2, 0);
            state.SetUndoState(BoardState.Capture(board, 3, 3, 0));
            state.SetUndoState(null);

            BoardState loaded = JsonUtility.FromJson<BoardState>(JsonUtility.ToJson(state));

            Assert.IsNull(loaded.GetUndoState());
        }

        [Test]
        public void BoardState_WithoutRandomState_ReportsNotAvailable()
        {
            BoardState state = BoardState.Capture(new BoardModel(3, 4), 2, 2, 0);

            BoardState loaded = JsonUtility.FromJson<BoardState>(JsonUtility.ToJson(state));

            Assert.IsFalse(loaded.TryGetRandomState(out _));
        }

        [Test]
        public void UndoConfig_DefaultsMatchTheBalanceValues()
        {
            var config = ScriptableObject.CreateInstance<UndoConfig>();

            try
            {
                Assert.AreEqual(5, config.MaxUses);
                Assert.AreEqual(UndoConfig.DefaultAnimationDuration, config.AnimationDuration, 0.0001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
