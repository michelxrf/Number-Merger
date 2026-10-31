using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NumberMerge.Gameplay.Tests
{
    public sealed class ReviveSelectionTests
    {
        private static readonly int MergeCeiling = new PieceGenerationSettings().MergeCeilingValue;

        [Test]
        public void EmptyBoard_ReturnsNoTiles()
        {
            BoardModel board = TestBoards.Create(TestBoards.NoColumn(), TestBoards.NoColumn());

            List<BoardPosition> chosen = ReviveSelection.Choose(board, new SystemRandomSource(new Random(1)));

            Assert.AreEqual(0, chosen.Count);
        }

        [Test]
        public void Count_IsTwentyPercentOfTiles()
        {
            // 5 columns x 7 visible tiles = 35 tiles, so 20% rounds down to 7.
            BoardModel board = TestBoards.Create(
                TestBoards.Column(2, 7), TestBoards.Column(2, 7), TestBoards.Column(2, 7),
                TestBoards.Column(2, 7), TestBoards.Column(2, 7));

            for (int seed = 0; seed < 100; seed++)
            {
                Assert.AreEqual(7, ReviveSelection.Choose(board, new SystemRandomSource(new Random(seed))).Count);
            }
        }

        [Test]
        public void MinTiles_ClampsThePercentage()
        {
            // 35 tiles, 20% is 7. A minimum of 10 raises the count to 10.
            BoardModel board = TestBoards.Create(
                TestBoards.Column(2, 7), TestBoards.Column(2, 7), TestBoards.Column(2, 7),
                TestBoards.Column(2, 7), TestBoards.Column(2, 7));

            for (int seed = 0; seed < 100; seed++)
            {
                Assert.AreEqual(10, ReviveSelection.Choose(board, new SystemRandomSource(new Random(seed)), 10, 0.2f).Count);
            }
        }

        [Test]
        public void MinTiles_BelowPercentage_DoesNotLowerTheCount()
        {
            // 35 tiles, 20% is 7. A minimum of 5 is below that, so the count stays at 7.
            BoardModel board = TestBoards.Create(
                TestBoards.Column(2, 7), TestBoards.Column(2, 7), TestBoards.Column(2, 7),
                TestBoards.Column(2, 7), TestBoards.Column(2, 7));

            for (int seed = 0; seed < 100; seed++)
            {
                Assert.AreEqual(7, ReviveSelection.Choose(board, new SystemRandomSource(new Random(seed)), 5, 0.2f).Count);
            }
        }

        [Test]
        public void ZeroMaxFraction_RemovesOnlyTheMinimum()
        {
            BoardModel board = TestBoards.Create(
                TestBoards.Column(2, 7), TestBoards.Column(2, 7), TestBoards.Column(2, 7),
                TestBoards.Column(2, 7), TestBoards.Column(2, 7));

            for (int seed = 0; seed < 50; seed++)
            {
                Assert.AreEqual(3, ReviveSelection.Choose(board, new SystemRandomSource(new Random(seed)), 3, 0f).Count);
            }
        }

        [Test]
        public void SmallBoard_StillRemovesAtLeastOneTile()
        {
            // 2 tiles: 20% rounds down to 0, so the minimum of 1 applies.
            BoardModel board = TestBoards.Create(new[] { 2 }, new[] { 4 });

            List<BoardPosition> chosen = ReviveSelection.Choose(board, new SystemRandomSource(new Random(3)));

            Assert.AreEqual(1, chosen.Count);
        }

        [Test]
        public void ChosenPositions_AreDistinctAndOccupied()
        {
            BoardModel board = TestBoards.Create(
                TestBoards.Column(2, 7), TestBoards.Column(4, 5), TestBoards.Column(8, 3));

            for (int seed = 0; seed < 100; seed++)
            {
                List<BoardPosition> chosen = ReviveSelection.Choose(board, new SystemRandomSource(new Random(seed)));

                var unique = new HashSet<BoardPosition>(chosen);
                Assert.AreEqual(chosen.Count, unique.Count, "duplicate position for seed " + seed);

                foreach (BoardPosition position in chosen)
                {
                    Assert.IsFalse(board.IsEmpty(position.Column, position.Row), position.ToString());
                }
            }
        }

        [Test]
        public void OverflowedColumn_AlwaysLosesItsTopTile()
        {
            // Column 0 is in overflow (8 tiles); its top tile is row 7.
            BoardModel board = TestBoards.Create(TestBoards.Column(2, 8), TestBoards.Column(4, 2));
            var topOfOverflow = new BoardPosition(0, 7);

            for (int seed = 0; seed < 100; seed++)
            {
                List<BoardPosition> chosen = ReviveSelection.Choose(board, new SystemRandomSource(new Random(seed)));
                Assert.Contains(topOfOverflow, chosen, "seed " + seed);
            }
        }

        [Test]
        public void ChosenTiles_ClearGameOver_ForEveryOverflowedColumn()
        {
            // Two overflowed columns (8 tiles each) and two normal ones.
            for (int seed = 0; seed < 100; seed++)
            {
                BoardModel board = TestBoards.Create(
                    TestBoards.Column(2, 8), TestBoards.Column(4, 2), TestBoards.Column(8, 8), TestBoards.Column(16, 3));
                var controller = new BoardController(
                    board,
                    new CascadeResolver(new MergeResolver(new SystemRandomSource(new Random(seed)), MergeCeiling)),
                    new NumberMerge.Managers.ScoreService(),
                    MergeCeiling);
                Assert.IsTrue(controller.IsGameOver);

                List<BoardPosition> chosen = ReviveSelection.Choose(board, new SystemRandomSource(new Random(seed + 500)));
                Assert.IsTrue(controller.TryDestroyTiles(chosen, out _), "seed " + seed);

                Assert.IsFalse(controller.IsGameOver, "still game over for seed " + seed);
            }
        }
    }
}
