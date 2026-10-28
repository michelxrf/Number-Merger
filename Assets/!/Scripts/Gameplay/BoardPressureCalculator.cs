using System;

namespace NumberMerge.Gameplay
{
    // Pressure signals for one board state.
    //   Height, Density, Imbalance: the signals as defined by the design formula, each in [0, 1].
    //   Raw:        0.50 * Height + 0.30 * Density + 0.20 * Imbalance, clamped to [0, 1].
    //   Normalized: Raw divided by the highest Raw a stacked board can reach, clamped to [0, 1].
    //               The generator schedules difficulty on Normalized so that the top of the scale is reachable.
    public readonly struct BoardPressure
    {
        public BoardPressure(double height, double density, double imbalance, double raw, double normalized)
        {
            Height = height;
            Density = density;
            Imbalance = imbalance;
            Raw = raw;
            Normalized = normalized;
        }

        public double Height { get; }
        public double Density { get; }
        public double Imbalance { get; }
        public double Raw { get; }
        public double Normalized { get; }
    }

    // Pure, read-only measurement of how close the board is to the danger threshold.
    //
    // The seventh visible row is the danger threshold. Column heights are clamped to the visible row
    // count, so a piece in the overflow row counts as a full column and never lowers the pressure.
    // The overflow row is also excluded from density, which only counts the 35 visible cells.
    public sealed class BoardPressureCalculator
    {
        private readonly PieceGenerationSettings _settings;

        public BoardPressureCalculator(PieceGenerationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public BoardPressure Calculate(BoardModel board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            int columns = board.ColumnCount;
            int visibleRows = board.VisibleRowCount;
            double visibleCells = (double)columns * visibleRows;

            int heightSum = 0;
            int occupiedVisible = 0;
            int minHeight = visibleRows;
            int maxHeight = 0;

            for (int column = 0; column < columns; column++)
            {
                int height = Math.Min(board.GetHeight(column), visibleRows);
                heightSum += height;
                minHeight = Math.Min(minHeight, height);
                maxHeight = Math.Max(maxHeight, height);

                for (int row = 0; row < visibleRows; row++)
                {
                    if (board.GetValue(column, row) != BoardModel.EmptyValue)
                    {
                        occupiedVisible++;
                    }
                }
            }

            double height01 = heightSum / visibleCells;
            double density01 = occupiedVisible / visibleCells;
            double imbalance01 = (maxHeight - minHeight) / (double)visibleRows;

            double raw = ScoreMath.Clamp01(
                _settings.HeightPressureWeight * height01 +
                _settings.DensityPressureWeight * density01 +
                _settings.ImbalancePressureWeight * imbalance01);

            double maxRaw = MaximumReachableRaw(columns);
            double normalized = maxRaw > 0d ? ScoreMath.Clamp01(raw / maxRaw) : 0d;

            return new BoardPressure(height01, density01, imbalance01, raw, normalized);
        }

        // Highest Raw value a gravity-stacked board can reach. On such boards density equals the height
        // term, so Raw = (height + density weights) * meanHeight + imbalance weight * imbalance.
        // The maximum is either a full board (imbalance 0) or one empty column next to full columns.
        private double MaximumReachableRaw(int columns)
        {
            double heightAndDensity = _settings.HeightPressureWeight + _settings.DensityPressureWeight;
            double fullBoard = heightAndDensity;
            double oneEmptyColumn = heightAndDensity * (columns - 1) / columns + _settings.ImbalancePressureWeight;
            return Math.Max(fullBoard, oneEmptyColumn);
        }
    }
}
