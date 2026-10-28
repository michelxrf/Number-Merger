using System;
using NUnit.Framework;
using NumberMerge.Gameplay;
using UnityEngine;
using Random = System.Random;

namespace NumberMerge.Gameplay.Tests
{
    public sealed class PieceGenerationConfigTests
    {
        private PieceGenerationConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PieceGenerationConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void NewConfig_HasDefaultSettings()
        {
            var defaults = new PieceGenerationSettings();

            Assert.AreEqual(defaults.Temperature, _config.Settings.Temperature, 1e-6f);
            Assert.AreEqual(defaults.HeightPressureWeight, _config.Settings.HeightPressureWeight, 1e-6f);
            Assert.AreEqual(defaults.HighPressureWeights.Congestion, _config.Settings.HighPressureWeights.Congestion, 1e-6f);
            Assert.AreEqual(defaults.MergeParticipantValues.Length, _config.Settings.MergeParticipantValues.Length);
        }

        [Test]
        public void ConfigValues_DriveGeneratorSelection()
        {
            // A very low temperature makes the best-scoring candidate almost certain. The mid-game board
            // below has different scores for 2 and 4, so the generator must follow the asset's temperature.
            _config.Settings.Temperature = 0.01f;
            _config.Settings.MinimumCandidateProbability = 0f;

            BoardModel board = TestBoards.Create(new[] { 2, 4, 8 }, new[] { 4, 8 }, new[] { 2, 4, 8, 16 });
            var generator = new PieceGenerator(new SystemRandomSource(new Random(1)), _config.Settings)
            {
                RecordDecisions = true
            };

            generator.Reset(board);

            PieceGenerationDecision decision = generator.LastDecision;
            Assert.IsNotNull(decision);

            double best = 0d;
            foreach (CandidateDecision candidate in decision.Candidates)
            {
                best = Math.Max(best, candidate.Probability);
            }

            Assert.Greater(best, 0.99);
        }

        [Test]
        public void InvalidConfig_IsRejectedByGenerator()
        {
            _config.Settings.Temperature = 0f;

            Assert.Throws<InvalidOperationException>(() =>
                new PieceGenerator(new SystemRandomSource(new Random(2)), _config.Settings));
        }

        [Test]
        public void InvalidMergeValues_AreRejected()
        {
            _config.Settings.MergeParticipantValues = new float[] { 0.5f, 1f };

            Assert.Throws<InvalidOperationException>(() => _config.Settings.Validate());
        }
    }
}
