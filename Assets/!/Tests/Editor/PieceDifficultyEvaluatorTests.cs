using System;
using System.Collections.Generic;
using NUnit.Framework;
using NumberMerge.Gameplay;

namespace NumberMerge.Gameplay.Tests
{
    public sealed class PieceDifficultyEvaluatorTests
    {
        private const double Tolerance = 1e-9;

        private PieceGenerationSettings _settings;
        private PieceDifficultyEvaluator _evaluator;
        private List<CandidateScores> _results;

        [SetUp]
        public void SetUp()
        {
            _settings = new PieceGenerationSettings();
            _evaluator = new PieceDifficultyEvaluator(_settings);
            _results = new List<CandidateScores>();
        }

        [Test]
        public void AllEligibleCandidates_ReceiveFiniteNormalizedScores()
        {
            BoardModel board = TestBoards.Create(
                new[] { 2, 4, 8 }, new[] { 16, 8, 4, 2 }, TestBoards.Column(2, 5),
                new[] { 64, 32 }, TestBoards.NoColumn());
            IReadOnlyList<SpawnChance> eligible = SpawnTable.Build(
                board, _settings.AbsentValueWeight, _settings.SpawnCeilingLevelOffset, _settings.SpawnHardCeiling,
                _settings.MergeCeilingValue);

            _evaluator.Evaluate(board, eligible, 0.5, _results, out _);

            Assert.AreEqual(eligible.Count, _results.Count);
            foreach (CandidateScores scores in _results)
            {
                AssertUnit(scores.Frequency, "frequency");
                AssertUnit(scores.MergeOpportunity, "mergeOpportunity");
                AssertUnit(scores.Congestion, "congestion");
                AssertUnit(scores.Progression, "progression");
                AssertUnit(scores.Score, "score");
                Assert.IsFalse(double.IsNaN(scores.Score));
                Assert.IsFalse(double.IsInfinity(scores.Score));
            }
        }

        [Test]
        public void MoreMergeOpportunity_IsEasierAllElse()
        {
            // Column 1 holds a 4. Landing a 4 next to it forms a pair merge; a 2 has no merge partner.
            BoardModel board = TestBoards.Create(TestBoards.NoColumn(), new[] { 4 });
            var candidates = new[] { new SpawnChance(2, 0.5), new SpawnChance(4, 0.5) };

            _evaluator.Evaluate(board, candidates, 0.5, _results, out _);

            CandidateScores two = Find(2);
            CandidateScores four = Find(4);

            Assert.AreEqual(0d, two.MergeOpportunity, Tolerance);
            Assert.Greater(four.MergeOpportunity, two.MergeOpportunity);
        }

        [Test]
        public void CongestionRisk_IsHigherWhenPlacementCannotMerge()
        {
            // Columns 1-4 are 6 high, so the next cell is almost full. Column 0 is empty.
            // A 4 merges with the 4 at the bottom of column 1 (which lowers risk in column 0); an 8 cannot.
            BoardModel board = TestBoards.Create(
                TestBoards.NoColumn(),
                new[] { 4, 2, 2, 2, 2, 2 },
                TestBoards.Column(2, 6),
                TestBoards.Column(2, 6),
                TestBoards.Column(2, 6));
            var candidates = new[] { new SpawnChance(4, 0.5), new SpawnChance(8, 0.5) };

            _evaluator.Evaluate(board, candidates, 0.5, _results, out _);

            CandidateScores four = Find(4);
            CandidateScores eight = Find(8);

            Assert.Greater(four.MergeOpportunity, eight.MergeOpportunity);
            Assert.Greater(eight.Congestion, four.Congestion);
        }

        [Test]
        public void Progression_IsLadderPositionWithinEligibleValues()
        {
            BoardModel board = new BoardModel();
            var candidates = new[] { new SpawnChance(2, 0.2), new SpawnChance(4, 0.3), new SpawnChance(8, 0.5) };

            _evaluator.Evaluate(board, candidates, 0.5, _results, out _);

            Assert.AreEqual(0d, Find(2).Progression, Tolerance);
            Assert.AreEqual(0.5d, Find(4).Progression, Tolerance);
            Assert.AreEqual(1d, Find(8).Progression, Tolerance);
        }

        [Test]
        public void Frequency_IsPriorNormalizedByLargestCandidate()
        {
            BoardModel board = new BoardModel();
            var candidates = new[] { new SpawnChance(2, 0.5), new SpawnChance(4, 0.25) };

            _evaluator.Evaluate(board, candidates, 0.5, _results, out _);

            Assert.AreEqual(1d, Find(2).Frequency, Tolerance);
            Assert.AreEqual(0.5d, Find(4).Frequency, Tolerance);
        }

        [Test]
        public void PressureChangesScoringBalance_AsIntended()
        {
            _evaluator.Evaluate(new BoardModel(), new[] { new SpawnChance(2, 1d) }, 0d, _results, out ScoreWeights low);
            _evaluator.Evaluate(new BoardModel(), new[] { new SpawnChance(2, 1d) }, 1d, _results, out ScoreWeights high);

            Assert.Greater(high.Congestion, low.Congestion);
            Assert.Less(high.Frequency, low.Frequency);
            Assert.Less(high.MergeOpportunity, low.MergeOpportunity);
            AssertNormalized(low);
            AssertNormalized(high);
        }

        [Test]
        public void HigherPressure_ShiftsProbabilityTowardCongestedCandidate()
        {
            // Same board as the congestion test. The 8 is congested, the 4 can merge. Both have equal priors.
            BoardModel board = TestBoards.Create(
                TestBoards.NoColumn(),
                new[] { 4, 2, 2, 2, 2, 2 },
                TestBoards.Column(2, 6),
                TestBoards.Column(2, 6),
                TestBoards.Column(2, 6));
            var candidates = new[] { new SpawnChance(4, 0.5), new SpawnChance(8, 0.5) };

            double eightAtLow = ProbabilityOf(board, candidates, 0d, 8);
            double eightAtHigh = ProbabilityOf(board, candidates, 1d, 8);

            Assert.Greater(eightAtHigh, eightAtLow);
        }

        [Test]
        public void Evaluate_DoesNotMutateBoard()
        {
            BoardModel board = TestBoards.Create(new[] { 4, 4 }, TestBoards.Column(2, 7), TestBoards.Column(8, 3));
            string before = TestBoards.Snapshot(board);

            _evaluator.Evaluate(board, SpawnTable.Build(
                board, _settings.AbsentValueWeight, _settings.SpawnCeilingLevelOffset, _settings.SpawnHardCeiling,
                _settings.MergeCeilingValue), 0.7, _results, out _);

            Assert.AreEqual(before, TestBoards.Snapshot(board));
        }

        [Test]
        public void Evaluate_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _evaluator.Evaluate(null, new[] { new SpawnChance(2, 1d) }, 0.5, _results, out _));
            Assert.Throws<ArgumentNullException>(() =>
                _evaluator.Evaluate(new BoardModel(), null, 0.5, _results, out _));
        }

        private double ProbabilityOf(BoardModel board, IReadOnlyList<SpawnChance> candidates, double pressure, int value)
        {
            _evaluator.Evaluate(board, candidates, pressure, _results, out _);

            var chances = new List<SpawnChance>();
            var weights = new List<double>();
            CandidateSelection.Compute(_results, _settings.Temperature, 0d, weights, chances);

            foreach (SpawnChance chance in chances)
            {
                if (chance.Value == value)
                {
                    return chance.Probability;
                }
            }

            throw new InvalidOperationException("Value not found: " + value);
        }

        private CandidateScores Find(int value)
        {
            foreach (CandidateScores scores in _results)
            {
                if (scores.Value == value)
                {
                    return scores;
                }
            }

            throw new InvalidOperationException("Candidate not found: " + value);
        }

        private static void AssertNormalized(ScoreWeights weights)
        {
            double sum = weights.Frequency + weights.MergeOpportunity + weights.Congestion + weights.Progression;
            Assert.AreEqual(1d, sum, Tolerance);
        }

        private static void AssertUnit(double value, string name)
        {
            Assert.That(value, Is.InRange(0d, 1d), name + " out of range");
        }
    }
}
