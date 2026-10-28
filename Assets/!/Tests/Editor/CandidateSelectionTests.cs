using System;
using System.Collections.Generic;
using NUnit.Framework;
using NumberMerge.Gameplay;

namespace NumberMerge.Gameplay.Tests
{
    public sealed class CandidateSelectionTests
    {
        private const double Tolerance = 1e-9;

        private List<double> _weights;
        private List<SpawnChance> _chances;

        [SetUp]
        public void SetUp()
        {
            _weights = new List<double>();
            _chances = new List<SpawnChance>();
        }

        [Test]
        public void Probabilities_SumToOne()
        {
            var random = new Random(5);
            var scores = new List<CandidateScores>();
            for (int i = 0; i < 20; i++)
            {
                scores.Add(Score(2 << i, random.NextDouble()));
            }

            CandidateSelection.Compute(scores, 0.8, 0.01, _weights, _chances);

            Assert.AreEqual(scores.Count, _chances.Count);
            double sum = 0d;
            foreach (SpawnChance chance in _chances)
            {
                Assert.Greater(chance.Probability, 0d);
                sum += chance.Probability;
            }

            Assert.AreEqual(1d, sum, 1e-9);
        }

        [Test]
        public void HigherScore_GetsHigherProbability_WithSoftmaxRatio()
        {
            var scores = new List<CandidateScores> { Score(2, 0.2), Score(4, 0.8) };

            CandidateSelection.Compute(scores, 0.8, 0d, _weights, _chances);

            Assert.Greater(_chances[1].Probability, _chances[0].Probability);

            // With no floor, the ratio is exactly exp((0.8 - 0.2) / 0.8), so the sign and normalization are verified.
            double ratio = _chances[1].Probability / _chances[0].Probability;
            Assert.AreEqual(Math.Exp(0.6 / 0.8), ratio, 1e-9);
        }

        [Test]
        public void EqualScores_GetEqualProbabilities()
        {
            var scores = new List<CandidateScores> { Score(2, 0.4), Score(4, 0.4), Score(8, 0.4) };

            CandidateSelection.Compute(scores, 0.8, 0d, _weights, _chances);

            foreach (SpawnChance chance in _chances)
            {
                Assert.AreEqual(1d / 3d, chance.Probability, Tolerance);
            }
        }

        [Test]
        public void ExtremeLowTemperature_SelectsBestWithoutNaN()
        {
            var scores = new List<CandidateScores> { Score(2, 0d), Score(4, 1d), Score(8, 1d), Score(16, 0.5d) };

            CandidateSelection.Compute(scores, 1e-12, 0d, _weights, _chances);

            AssertValidDistribution(_chances);
            Assert.AreEqual(0.5d, _chances[1].Probability, Tolerance);
            Assert.AreEqual(0.5d, _chances[2].Probability, Tolerance);
            Assert.AreEqual(0d, _chances[0].Probability, Tolerance);
            Assert.AreEqual(0d, _chances[3].Probability, Tolerance);
        }

        [Test]
        public void ExtremeHighTemperature_ApproachesUniform()
        {
            var scores = new List<CandidateScores> { Score(2, 0d), Score(4, 1d), Score(8, 0.3d) };

            CandidateSelection.Compute(scores, 1e12, 0d, _weights, _chances);

            AssertValidDistribution(_chances);
            foreach (SpawnChance chance in _chances)
            {
                Assert.AreEqual(1d / 3d, chance.Probability, 1e-6);
            }
        }

        [Test]
        public void MinimumProbability_IsRespectedAtExtremeTemperature()
        {
            var scores = new List<CandidateScores> { Score(2, 0d), Score(4, 1d), Score(8, 1d), Score(16, 0.5d) };

            CandidateSelection.Compute(scores, 1e-12, 0.05, _weights, _chances);

            AssertValidDistribution(_chances);
            foreach (SpawnChance chance in _chances)
            {
                Assert.GreaterOrEqual(chance.Probability, 0.05 - Tolerance);
            }
        }

        [Test]
        public void MinimumProbability_IsCappedAtUniform()
        {
            // A floor larger than 1 / count must not produce negative shares.
            var scores = new List<CandidateScores> { Score(2, 0d), Score(4, 1d) };

            CandidateSelection.Compute(scores, 1e-12, 0.9, _weights, _chances);

            AssertValidDistribution(_chances);
            Assert.AreEqual(0.5d, _chances[0].Probability, Tolerance);
            Assert.AreEqual(0.5d, _chances[1].Probability, Tolerance);
        }

        [Test]
        public void ExtremeScores_StayFinite()
        {
            // Score gap of 1 divided by a tiny temperature gives an exponent of -1e300, which must underflow to 0.
            var scores = new List<CandidateScores> { Score(2, 0d), Score(4, 1d) };

            CandidateSelection.Compute(scores, 1e-300, 0.01, _weights, _chances);

            AssertValidDistribution(_chances);
        }

        [Test]
        public void EmptyScores_ProduceEmptyChances()
        {
            CandidateSelection.Compute(new List<CandidateScores>(), 0.8, 0.01, _weights, _chances);

            Assert.AreEqual(0, _chances.Count);
        }

        [Test]
        public void NonPositiveTemperature_Throws()
        {
            var scores = new List<CandidateScores> { Score(2, 0.5) };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CandidateSelection.Compute(scores, 0d, 0.01, _weights, _chances));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CandidateSelection.Compute(scores, double.NaN, 0.01, _weights, _chances));
        }

        [Test]
        public void NonFiniteScore_Throws()
        {
            var scores = new List<CandidateScores> { Score(2, double.NaN) };

            Assert.Throws<InvalidOperationException>(() =>
                CandidateSelection.Compute(scores, 0.8, 0.01, _weights, _chances));
        }

        private static CandidateScores Score(int value, double score)
        {
            return new CandidateScores(value, 0d, 0d, 0d, 0d, score);
        }

        private static void AssertValidDistribution(IReadOnlyList<SpawnChance> chances)
        {
            double sum = 0d;
            foreach (SpawnChance chance in chances)
            {
                Assert.IsFalse(double.IsNaN(chance.Probability), "NaN probability");
                Assert.IsFalse(double.IsInfinity(chance.Probability), "Infinite probability");
                Assert.That(chance.Probability, Is.InRange(0d, 1d));
                sum += chance.Probability;
            }

            Assert.AreEqual(1d, sum, 1e-9);
        }
    }
}
