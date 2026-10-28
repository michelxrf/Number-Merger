using System;
using System.Collections.Generic;
using NUnit.Framework;
using NumberMerge.Gameplay;
using NumberMerge.Managers;

namespace NumberMerge.Gameplay.Tests
{
    public sealed class PieceGeneratorTests
    {
        private const double Tolerance = 1e-9;

        [Test]
        public void Reset_EmptyBoard_GivesMinimumValue()
        {
            var generator = new PieceGenerator(new SystemRandomSource(new Random(1)));

            generator.Reset(new BoardModel());

            Assert.AreEqual(SpawnTable.MinimumValue, generator.Current);
            Assert.AreEqual(SpawnTable.MinimumValue, generator.Next);
        }

        [Test]
        public void GeneratedValues_AreEligibleAndWithinMaxTile()
        {
            BoardModel board = TestBoards.Create(new[] { 4, 16 }, new[] { 8, 2 }, TestBoards.Column(64, 2), new[] { 32 });
            HashSet<int> eligible = EligibleValues(board);
            var generator = new PieceGenerator(new SystemRandomSource(new Random(2)));

            generator.Reset(board);
            for (int i = 0; i < 500; i++)
            {
                Assert.IsTrue(eligible.Contains(generator.Current), "Ineligible value " + generator.Current);
                Assert.LessOrEqual(generator.Current, MergeMath.MaxTileValue);
                generator.Advance(board);
            }
        }

        [Test]
        public void NearMaxTile_NeverSpawnsTheHighestPiece()
        {
            // Highest 8192, second highest 4096: values up to 4096 may spawn, the highest 8192 never does.
            BoardModel board = TestBoards.Create(new[] { MergeMath.MaxTileValue }, new[] { 4096 });
            var generator = new PieceGenerator(new SystemRandomSource(new Random(3)));

            generator.Reset(board);
            for (int i = 0; i < 300; i++)
            {
                Assert.Less(generator.Current, MergeMath.MaxTileValue);
                Assert.LessOrEqual(generator.Current, 4096);
                Assert.IsTrue(generator.Current >= SpawnTable.MinimumValue);
                generator.Advance(board);
            }
        }

        [Test]
        public void AbsentValueWeightZero_KeepsBoardOnlyBehavior()
        {
            // With absentValueWeight 0 only values on the board (plus 2) can spawn. Here that is 2 and 4096.
            BoardModel board = TestBoards.Create(new[] { MergeMath.MaxTileValue }, new[] { 4096 });
            var settings = new PieceGenerationSettings { AbsentValueWeight = 0f };
            var generator = new PieceGenerator(new SystemRandomSource(new Random(3)), settings);
            var seen = new HashSet<int>();

            generator.Reset(board);
            for (int i = 0; i < 300; i++)
            {
                seen.Add(generator.Current);
                generator.Advance(board);
            }

            CollectionAssert.AreEquivalent(new[] { SpawnTable.MinimumValue, 4096 }, seen);
        }

        [Test]
        public void AbsentValues_CanSpawnWhenNotOnBoard()
        {
            // Board has 16 and 8, so the ceiling is 8 (inclusive). The value 4 is not on the board but must be able to spawn.
            BoardModel board = TestBoards.Create(new[] { 16 }, new[] { 8 });
            var generator = new PieceGenerator(new SystemRandomSource(new Random(13)));
            var seen = new HashSet<int>();

            generator.Reset(board);
            for (int i = 0; i < 1000; i++)
            {
                seen.Add(generator.Current);
                generator.Advance(board);
            }

            CollectionAssert.AreEquivalent(new[] { 2, 4, 8 }, seen);
        }

        [Test]
        public void EligibleValues_IncludeEveryPowerBelowLimit()
        {
            // Highest 64, second highest 32: every power up to 32 (inclusive) is eligible, 64 is not.
            BoardModel board = TestBoards.Create(new[] { 64 }, new[] { 32 });

            HashSet<int> eligible = EligibleValues(board);

            CollectionAssert.AreEquivalent(new[] { 2, 4, 8, 16, 32 }, eligible);
        }

        [Test]
        public void SingleDistinctValue_ExcludesHighestAndAbove()
        {
            // Only 16 on the board: there is no second piece, so the highest (16) is excluded.
            BoardModel board = TestBoards.Create(new[] { 16, 16 });

            CollectionAssert.AreEquivalent(new[] { 2, 4, 8 }, EligibleValues(board));
        }

        [Test]
        public void MaxTileOnBoard_DoesNotLetItSpawn()
        {
            BoardModel board = TestBoards.Create(new[] { MergeMath.MaxTileValue }, new[] { 16 }, new[] { 8 });
            var generator = new PieceGenerator(new SystemRandomSource(new Random(4)));

            generator.Reset(board);
            for (int i = 0; i < 300; i++)
            {
                Assert.Less(generator.Current, MergeMath.MaxTileValue);
                Assert.IsTrue(EligibleValues(board).Contains(generator.Current), "Ineligible value " + generator.Current);
                generator.Advance(board);
            }
        }

        [Test]
        public void AllEligibleValues_RemainSelectable()
        {
            BoardModel board = TestBoards.Create(new[] { 16 }, new[] { 8 }, new[] { 4 }, new[] { 2 });
            HashSet<int> eligible = EligibleValues(board);
            var generator = new PieceGenerator(new SystemRandomSource(new Random(5)));
            var seen = new HashSet<int>();

            generator.Reset(board);
            for (int i = 0; i < 3000; i++)
            {
                seen.Add(generator.Current);
                generator.Advance(board);
            }

            Assert.AreEqual(eligible.Count, seen.Count);
            foreach (int value in eligible)
            {
                Assert.IsTrue(seen.Contains(value), "Value never selected: " + value);
            }
        }

        [Test]
        public void Generation_DoesNotMutateBoard()
        {
            BoardModel board = TestBoards.Create(TestBoards.Column(2, 7), new[] { 4, 4 }, new[] { 8, 2 }, TestBoards.Column(16, 8));
            string before = TestBoards.Snapshot(board);
            var generator = new PieceGenerator(new SystemRandomSource(new Random(6)));

            generator.Reset(board);
            for (int i = 0; i < 50; i++)
            {
                generator.Advance(board);
            }

            Assert.AreEqual(before, TestBoards.Snapshot(board));
        }

        [Test]
        public void Generation_ConsumesOneRandomRollPerPiece()
        {
            var counting = new CountingRandomSource(new SystemRandomSource(new Random(7)));
            var generator = new PieceGenerator(counting);
            BoardModel board = TestBoards.Create(new[] { 4 }, new[] { 2, 2 }, TestBoards.Column(8, 3));

            generator.Reset(board);
            Assert.AreEqual(2, counting.Calls, "Reset should draw exactly one roll per piece.");

            generator.Advance(board);
            Assert.AreEqual(3, counting.Calls, "Advance should draw exactly one roll.");
        }

        [Test]
        public void Restore_DoesNotUseRandom()
        {
            var counting = new CountingRandomSource(new SystemRandomSource(new Random(8)));
            var generator = new PieceGenerator(counting);

            generator.Restore(16, 4);

            Assert.AreEqual(16, generator.Current);
            Assert.AreEqual(4, generator.Next);
            Assert.AreEqual(0, counting.Calls);
        }

        [Test]
        public void SeededRuns_AreReproducible()
        {
            List<string> first = PlayTrace(42);
            List<string> second = PlayTrace(42);

            Assert.Greater(first.Count, 5);
            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            List<string> first = PlayTrace(42);
            List<string> other = PlayTrace(43);

            Assert.AreNotEqual(string.Join("\n", first), string.Join("\n", other));
        }

        [Test]
        public void RecordDecisions_IsOffByDefault()
        {
            var generator = new PieceGenerator(new SystemRandomSource(new Random(9)));

            generator.Reset(new BoardModel());

            Assert.IsNull(generator.LastDecision);
        }

        [Test]
        public void RecordDecisions_ExposesSelectionDetails()
        {
            var generator = new PieceGenerator(new SystemRandomSource(new Random(10)))
            {
                RecordDecisions = true
            };
            BoardModel board = TestBoards.Create(new[] { 4, 2 }, new[] { 8 }, TestBoards.Column(16, 3));

            generator.Reset(board);
            generator.Advance(board);

            PieceGenerationDecision decision = generator.LastDecision;
            Assert.IsNotNull(decision);
            Assert.AreEqual(generator.Next, decision.SelectedValue);
            Assert.AreEqual(EligibleValues(board).Count, decision.Candidates.Count);

            double sum = 0d;
            foreach (CandidateDecision candidate in decision.Candidates)
            {
                sum += candidate.Probability;
            }

            Assert.AreEqual(1d, sum, 1e-9);
            Assert.That(decision.Pressure.Raw, Is.InRange(0d, 1d));
            Assert.IsNotEmpty(decision.ToString());
        }

        [Test]
        public void InvalidSettings_ThrowOnConstruction()
        {
            var settings = new PieceGenerationSettings { Temperature = 0f };

            Assert.Throws<InvalidOperationException>(() =>
                new PieceGenerator(new SystemRandomSource(new Random(11)), settings));
        }

        [Test]
        public void Reset_NullBoard_Throws()
        {
            var generator = new PieceGenerator(new SystemRandomSource(new Random(12)));

            Assert.Throws<ArgumentNullException>(() => generator.Reset(null));
        }

        private static List<string> PlayTrace(int seed)
        {
            var trace = new List<string>();
            var controller = new BoardController(
                new BoardModel(),
                new CascadeResolver(new MergeResolver(new SystemRandomSource(new Random(seed + 1000)))),
                new ScoreService());
            var generator = new PieceGenerator(new SystemRandomSource(new Random(seed)));
            var policy = new Random(seed + 2000);

            generator.Reset(controller.Board);
            trace.Add(generator.Current + "," + generator.Next);

            for (int attempt = 0; attempt < 80 && !controller.IsGameOver; attempt++)
            {
                int column = policy.Next(controller.Board.ColumnCount);
                if (!controller.TryPlay(column, generator.Current))
                {
                    continue;
                }

                generator.Advance(controller.Board);
                trace.Add(column + ":" + generator.Current + "," + generator.Next + "|" + TestBoards.Snapshot(controller.Board));
            }

            return trace;
        }

        private static HashSet<int> EligibleValues(BoardModel board)
        {
            var values = new HashSet<int>();
            var defaults = new PieceGenerationSettings();
            foreach (SpawnChance chance in SpawnTable.Build(
                board, defaults.AbsentValueWeight, defaults.SpawnCeilingLevelOffset, defaults.SpawnHardCeiling))
            {
                values.Add(chance.Value);
            }

            return values;
        }

        private sealed class CountingRandomSource : IRandomSource
        {
            private readonly IRandomSource _inner;

            public CountingRandomSource(IRandomSource inner)
            {
                _inner = inner;
            }

            public int Calls { get; private set; }

            public int Next(int maxExclusive)
            {
                Calls++;
                return _inner.Next(maxExclusive);
            }
        }
    }
}
