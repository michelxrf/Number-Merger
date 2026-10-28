using System;
using NUnit.Framework;
using NumberMerge.Gameplay;

namespace NumberMerge.Gameplay.Tests
{
    public sealed class BoardPressureCalculatorTests
    {
        // Settings are float-backed (inspector values), so comparisons allow float precision.
        private const double Tolerance = 1e-6;

        private BoardPressureCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _calculator = new BoardPressureCalculator(new PieceGenerationSettings());
        }

        [Test]
        public void EmptyBoard_ReturnsZeroPressure()
        {
            BoardPressure pressure = _calculator.Calculate(new BoardModel());

            Assert.AreEqual(0d, pressure.Raw, Tolerance);
            Assert.AreEqual(0d, pressure.Normalized, Tolerance);
            Assert.AreEqual(0d, pressure.Height, Tolerance);
            Assert.AreEqual(0d, pressure.Density, Tolerance);
            Assert.AreEqual(0d, pressure.Imbalance, Tolerance);
        }

        [Test]
        public void FullVisibleBoard_MatchesFormula()
        {
            // Every column holds 7 visible pieces: H = 1, D = 1, I = 0 -> 0.50 + 0.30 = 0.80.
            BoardModel board = TestBoards.Create(
                TestBoards.Column(2, 7), TestBoards.Column(2, 7), TestBoards.Column(2, 7),
                TestBoards.Column(2, 7), TestBoards.Column(2, 7));

            BoardPressure pressure = _calculator.Calculate(board);

            Assert.AreEqual(0.8d, pressure.Raw, Tolerance);
            Assert.AreEqual(1d, pressure.Height, Tolerance);
            Assert.AreEqual(1d, pressure.Density, Tolerance);
            Assert.AreEqual(0d, pressure.Imbalance, Tolerance);
        }

        [Test]
        public void StackedMaximum_IsFourFullColumnsWithOneEmpty()
        {
            // Full board is 0.80, but imbalance makes four full columns plus one empty column higher:
            // 0.50 * 0.8 + 0.30 * 0.8 + 0.20 * 1 = 0.84. This is the highest reachable Raw value.
            BoardModel board = TestBoards.Create(
                TestBoards.Column(2, 7), TestBoards.Column(2, 7), TestBoards.Column(2, 7),
                TestBoards.Column(2, 7), TestBoards.NoColumn());

            BoardPressure pressure = _calculator.Calculate(board);

            Assert.AreEqual(0.84d, pressure.Raw, Tolerance);
            Assert.AreEqual(1d, pressure.Normalized, Tolerance);
        }

        [Test]
        public void SingleTallColumn_MatchesFormulaComponents()
        {
            // Heights [7,0,0,0,0]: H = 7/35 = 0.2, D = 7/35 = 0.2, I = 1.
            BoardModel board = TestBoards.Create(TestBoards.Column(2, 7));

            BoardPressure pressure = _calculator.Calculate(board);

            Assert.AreEqual(0.2d, pressure.Height, Tolerance);
            Assert.AreEqual(0.2d, pressure.Density, Tolerance);
            Assert.AreEqual(1d, pressure.Imbalance, Tolerance);
            Assert.AreEqual(0.5 * 0.2 + 0.3 * 0.2 + 0.2 * 1d, pressure.Raw, Tolerance);
        }

        [Test]
        public void SingleTallColumn_ExceedsUniformLowBoardWithComparableOccupancy()
        {
            // Both boards hold 7 pieces. The tall column is more dangerous than the spread-out board.
            BoardModel tall = TestBoards.Create(TestBoards.Column(2, 7));
            BoardModel uniform = TestBoards.Create(
                TestBoards.Column(2, 1), TestBoards.Column(2, 1), TestBoards.Column(2, 1),
                TestBoards.Column(2, 2), TestBoards.Column(2, 2));

            double tallPressure = _calculator.Calculate(tall).Raw;
            double uniformPressure = _calculator.Calculate(uniform).Raw;

            Assert.Greater(tallPressure, uniformPressure);
        }

        [Test]
        public void OverflowRow_DoesNotReducePressure()
        {
            // A piece in the overflow row must not make the board look safer than the same board at 7.
            BoardModel atThreshold = TestBoards.Create(TestBoards.Column(2, 7), TestBoards.NoColumn());
            BoardModel overflowed = TestBoards.Create(TestBoards.Column(2, 8), TestBoards.NoColumn());

            BoardPressure atThresholdPressure = _calculator.Calculate(atThreshold);
            BoardPressure overflowedPressure = _calculator.Calculate(overflowed);

            Assert.GreaterOrEqual(overflowedPressure.Raw, atThresholdPressure.Raw - Tolerance);
            Assert.AreEqual(atThresholdPressure.Raw, overflowedPressure.Raw, Tolerance);

            // The overflowed column is clamped to the visible height: H = 7 / 35 = 0.2, same as at threshold.
            Assert.AreEqual(0.2d, overflowedPressure.Height, Tolerance);
            Assert.AreEqual(atThresholdPressure.Density, overflowedPressure.Density, Tolerance);
        }

        [Test]
        public void RandomBoards_StayWithinUnitInterval()
        {
            var random = new Random(123);
            int[] values = { 2, 4, 8, 16, 1024 };

            for (int iteration = 0; iteration < 500; iteration++)
            {
                var board = new BoardModel();
                for (int column = 0; column < board.ColumnCount; column++)
                {
                    int count = random.Next(0, board.TotalRowCount + 1);
                    for (int i = 0; i < count; i++)
                    {
                        board.TryInsert(column, values[random.Next(values.Length)]);
                    }
                }

                BoardPressure pressure = _calculator.Calculate(board);

                AssertUnit(pressure.Raw, "raw");
                AssertUnit(pressure.Normalized, "normalized");
                AssertUnit(pressure.Height, "height");
                AssertUnit(pressure.Density, "density");
                AssertUnit(pressure.Imbalance, "imbalance");
            }
        }

        [Test]
        public void Calculate_DoesNotMutateBoard()
        {
            BoardModel board = TestBoards.Create(TestBoards.Column(2, 7), TestBoards.Column(4, 3), TestBoards.Column(2, 8));
            string before = TestBoards.Snapshot(board);

            _calculator.Calculate(board);

            Assert.AreEqual(before, TestBoards.Snapshot(board));
        }

        [Test]
        public void Calculate_NullBoard_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _calculator.Calculate(null));
        }

        private static void AssertUnit(double value, string name)
        {
            Assert.That(value, Is.InRange(0d, 1d), name + " out of range");
        }
    }
}
