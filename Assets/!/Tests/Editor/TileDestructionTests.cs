using System;
using System.Collections.Generic;
using NumberMerge.Managers;
using NUnit.Framework;

namespace NumberMerge.Gameplay.Tests
{
    public sealed class TileDestructionTests
    {
        private static readonly int MergeCeiling = new PieceGenerationSettings().MergeCeilingValue;

        private static BoardController CreateController(BoardModel board, int seed = 1)
        {
            return new BoardController(
                board,
                new CascadeResolver(new MergeResolver(new SystemRandomSource(new Random(seed)), MergeCeiling)),
                new ScoreService(),
                MergeCeiling);
        }

        [Test]
        public void Destroy_RemovesTiles_AndScoresNothingWithoutMerges()
        {
            // Column 0 = [2 (bottom), 4]; column 1 = [16]. Removing the 4 leaves no equal neighbours.
            BoardModel board = TestBoards.Create(new[] { 2, 4 }, new[] { 16 });
            BoardController controller = CreateController(board);

            bool ok = controller.TryDestroyTiles(new List<BoardPosition> { new BoardPosition(0, 1) }, out DestructionResult result);

            Assert.IsTrue(ok);
            Assert.AreEqual(0, board.GetValue(0, 1));
            Assert.AreEqual(0, controller.CurrentScore);
            Assert.AreEqual(0, result.Cascade.MergeCount);
            Assert.AreEqual(1, result.Destroyed.Count);
        }

        [Test]
        public void Destroy_AllTilesAtOnce_RemovesEachOne()
        {
            BoardModel board = TestBoards.Create(new[] { 2, 4 }, new[] { 8, 16 }, new[] { 32 });
            BoardController controller = CreateController(board);

            var targets = new List<BoardPosition>
            {
                new BoardPosition(0, 1),
                new BoardPosition(1, 0),
                new BoardPosition(2, 0)
            };

            Assert.IsTrue(controller.TryDestroyTiles(targets, out DestructionResult result));
            Assert.AreEqual(3, result.Destroyed.Count);
            Assert.AreEqual(BoardModel.EmptyValue, result.AfterRemoval.GetValue(2, 0));
            Assert.AreEqual(0, controller.CurrentScore);
        }

        [Test]
        public void Destroy_FollowedBySolve_MergesAndScoresTheMerge()
        {
            // Removing the 2 at the bottom of column 0 lets the 4 fall next to the 4 in column 1.
            BoardModel board = TestBoards.Create(new[] { 2, 4 }, new[] { 4 });
            BoardController controller = CreateController(board);

            Assert.IsTrue(controller.TryDestroyTiles(new List<BoardPosition> { new BoardPosition(0, 0) }, out DestructionResult result));

            Assert.AreEqual(1, result.Cascade.MergeCount);
            Assert.AreEqual(8, controller.CurrentScore);
            Assert.AreEqual(8, board.GetValue(0, 0) + board.GetValue(1, 0));
        }

        [Test]
        public void Destroy_RejectsEmptyCell_WithoutChangingBoard()
        {
            BoardModel board = TestBoards.Create(new[] { 2 }, new[] { 4 });
            BoardController controller = CreateController(board);
            string before = TestBoards.Snapshot(board);

            bool ok = controller.TryDestroyTiles(new List<BoardPosition> { new BoardPosition(0, 0), new BoardPosition(0, 3) }, out DestructionResult result);

            Assert.IsFalse(ok);
            Assert.IsNull(result);
            Assert.AreEqual(before, TestBoards.Snapshot(board));
        }

        [Test]
        public void Destroy_RejectsOutOfRangeAndDuplicatePositions()
        {
            BoardModel board = TestBoards.Create(new[] { 2, 4 });
            BoardController controller = CreateController(board);
            string before = TestBoards.Snapshot(board);

            Assert.IsFalse(controller.TryDestroyTiles(new List<BoardPosition> { new BoardPosition(9, 0) }, out _));
            Assert.IsFalse(controller.TryDestroyTiles(new List<BoardPosition> { new BoardPosition(0, 99) }, out _));
            Assert.IsFalse(controller.TryDestroyTiles(new List<BoardPosition> { new BoardPosition(0, 0), new BoardPosition(0, 0) }, out _));
            Assert.IsFalse(controller.TryDestroyTiles(new List<BoardPosition>(), out _));
            Assert.IsFalse(controller.TryDestroyTiles(null, out _));

            Assert.AreEqual(before, TestBoards.Snapshot(board));
        }

        [Test]
        public void Destroy_IsAllowedOnGameOverBoard_AndClearsGameOverWhenOverflowIsRemoved()
        {
            // Column 0 holds 8 tiles: one more than the 7 visible rows, so it is in game over.
            BoardModel board = TestBoards.Create(TestBoards.Column(2, 8), TestBoards.Column(4, 3));
            BoardController controller = CreateController(board);
            Assert.IsTrue(controller.IsGameOver);

            bool ok = controller.TryDestroyTiles(new List<BoardPosition> { new BoardPosition(0, 7) }, out _);

            Assert.IsTrue(ok);
            Assert.IsFalse(controller.IsGameOver);
            Assert.AreEqual(7, board.GetPieceCount(0));
        }

        [Test]
        public void Destroy_RecordsBeforeAndAfterRemovalSnapshots()
        {
            BoardModel board = TestBoards.Create(new[] { 2, 4 });
            BoardController controller = CreateController(board);

            controller.TryDestroyTiles(new List<BoardPosition> { new BoardPosition(0, 0) }, out DestructionResult result);

            Assert.AreEqual(2, result.Before.GetValue(0, 0));
            Assert.AreEqual(BoardModel.EmptyValue, result.AfterRemoval.GetValue(0, 0));
            Assert.AreEqual(4, result.AfterRemoval.GetValue(0, 1));
        }
    }
}
