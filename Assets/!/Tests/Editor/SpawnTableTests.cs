using System;
using System.Collections.Generic;
using NUnit.Framework;
using NumberMerge.Gameplay;

namespace NumberMerge.Gameplay.Tests
{
    public sealed class SpawnTableTests
    {
        private static readonly int DefaultMergeCeiling = new PieceGenerationSettings().MergeCeilingValue;

        [TestCase(16, -1, 8)]
        [TestCase(16, 0, 16)]
        [TestCase(16, 1, 32)]
        [TestCase(16, -2, 4)]
        [TestCase(16, -4, 1)]
        [TestCase(8192, 1, 8192)]
        [TestCase(4096, 2, 8192)]
        [TestCase(0, 3, 0)]
        [TestCase(0, -3, 0)]
        public void CalculateCeiling_ShiftsHighestByLevels(int highest, int offset, int expected)
        {
            Assert.AreEqual(expected, SpawnTable.CalculateCeiling(highest, offset, DefaultMergeCeiling));
        }

        [Test]
        public void CalculateCeiling_ClampsToMergeCeiling()
        {
            Assert.AreEqual(64, SpawnTable.CalculateCeiling(16, 4, 64));
        }

        [Test]
        public void MergeCeilingAboveOldLimit_AllowsSpawnsAboveIt()
        {
            // Highest 8192 with level +1 would be 16384; the merge ceiling of 16384 now allows it.
            BoardModel board = TestBoards.Create(new[] { 8192 });

            HashSet<int> eligible = Eligible(board, 1, 0, 16384);

            Assert.IsTrue(eligible.Contains(16384));
        }

        [Test]
        public void OffsetZero_LetsTheHighestPieceSpawn()
        {
            BoardModel board = TestBoards.Create(new[] { 16, 16 });

            HashSet<int> eligible = Eligible(board, 0);

            CollectionAssert.AreEquivalent(new[] { 2, 4, 8, 16 }, eligible);
        }

        [Test]
        public void PositiveOffset_AllowsValuesAboveHighest()
        {
            // Highest 16, one level up: 32 is allowed even though it is not on the board.
            BoardModel board = TestBoards.Create(new[] { 16 });

            HashSet<int> eligible = Eligible(board, 1);

            CollectionAssert.AreEquivalent(new[] { 2, 4, 8, 16, 32 }, eligible);
        }

        [Test]
        public void NegativeOffset_LimitsValuesBelowHighest()
        {
            // Highest 64, two levels down: ceiling 16.
            BoardModel board = TestBoards.Create(new[] { 64 });

            HashSet<int> eligible = Eligible(board, -2);

            CollectionAssert.AreEquivalent(new[] { 2, 4, 8, 16 }, eligible);
        }

        [Test]
        public void NegativeOffset_BelowMinimumLeavesOnlyTheMinimumValue()
        {
            BoardModel board = TestBoards.Create(new[] { 16 });

            HashSet<int> eligible = Eligible(board, -5);

            CollectionAssert.AreEquivalent(new[] { SpawnTable.MinimumValue }, eligible);
        }

        [Test]
        public void EmptyBoard_IgnoresOffset()
        {
            HashSet<int> eligible = Eligible(new BoardModel(), 5);

            CollectionAssert.AreEquivalent(new[] { SpawnTable.MinimumValue }, eligible);
        }

        [Test]
        public void OffsetZero_HighestPieceActuallySpawns()
        {
            BoardModel board = TestBoards.Create(new[] { 16, 16 });
            var generator = new PieceGenerator(
                new SystemRandomSource(new Random(21)),
                new PieceGenerationSettings { SpawnCeilingLevelOffset = 0 });
            var seen = new HashSet<int>();

            generator.Reset(board);
            for (int i = 0; i < 1000; i++)
            {
                seen.Add(generator.Current);
                generator.Advance(board);
            }

            Assert.IsTrue(seen.Contains(16));
        }

        [Test]
        public void OffsetOutOfRange_IsRejectedBySettings()
        {
            var settings = new PieceGenerationSettings { SpawnCeilingLevelOffset = PieceGenerationSettings.MaxSpawnCeilingLevelOffset + 1 };

            Assert.Throws<InvalidOperationException>(() => settings.Validate());
        }

        [Test]
        public void HardCeiling_CapsSpawnsBelowBoardCeiling()
        {
            // Highest 64 with level +2 would allow up to 256, but the hard cap limits spawns to 16.
            BoardModel board = TestBoards.Create(new[] { 64 });

            HashSet<int> eligible = Eligible(board, 2, 16);

            CollectionAssert.AreEquivalent(new[] { 2, 4, 8, 16 }, eligible);
        }

        [Test]
        public void HardCeiling_AboveMergeCeiling_IsRejectedBySettings()
        {
            var settings = new PieceGenerationSettings { MergeCeilingValue = 64, SpawnHardCeiling = 128 };

            Assert.Throws<InvalidOperationException>(() => settings.Validate());
        }

        [Test]
        public void HardCeiling_DoesNotLimitMergeResults()
        {
            // The hard cap only applies to spawns. Playing a 4 next to a 4 still merges into 8,
            // which is above the cap of 2 used by the settings below.
            var settings = new PieceGenerationSettings { SpawnHardCeiling = 2 };
            Assert.DoesNotThrow(() => settings.Validate());

            // Column 1 holds a 4; playing a 4 on top of it merges into 8 (single neighbour, no other merges).
            BoardModel board = TestBoards.Create(TestBoards.NoColumn(), new[] { 4 });
            var controller = new BoardController(
                board,
                new CascadeResolver(new MergeResolver(
                    new SystemRandomSource(new Random(1)),
                    new PieceGenerationSettings().MergeCeilingValue)),
                new NumberMerge.Managers.ScoreService(),
                DefaultMergeCeiling);

            Assert.IsTrue(controller.TryPlay(1, 4));
            Assert.AreEqual(8, board.GetValue(1, 0));
        }

        [Test]
        public void HardCeiling_ZeroMeansNoCap()
        {
            BoardModel board = TestBoards.Create(new[] { 64 });

            HashSet<int> eligible = Eligible(board, 2, 0);

            Assert.IsTrue(eligible.Contains(256));
        }

        [TestCase(3)]
        [TestCase(16384)]
        public void HardCeiling_InvalidValuesAreRejectedBySettings(int value)
        {
            var settings = new PieceGenerationSettings { SpawnHardCeiling = value };

            Assert.Throws<InvalidOperationException>(() => settings.Validate());
        }

        private static HashSet<int> Eligible(BoardModel board, int offset, int hardCeiling = 0, int mergeCeiling = 0)
        {
            var values = new HashSet<int>();
            if (mergeCeiling == 0)
            {
                mergeCeiling = DefaultMergeCeiling;
            }

            foreach (SpawnChance chance in SpawnTable.Build(board, 0.5, offset, hardCeiling, mergeCeiling))
            {
                values.Add(chance.Value);
            }

            return values;
        }
    }
}
