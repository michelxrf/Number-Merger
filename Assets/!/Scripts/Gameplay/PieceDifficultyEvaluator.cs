using System;
using System.Collections.Generic;

namespace NumberMerge.Gameplay
{
    // Component scores for one candidate value. Every component is in [0, 1] where higher means
    // "more favored" for selection. Score is the weighted combination used by the selection step.
    public readonly struct CandidateScores
    {
        public CandidateScores(int value, double frequency, double mergeOpportunity, double congestion, double progression, double score)
        {
            Value = value;
            Frequency = frequency;
            MergeOpportunity = mergeOpportunity;
            Congestion = congestion;
            Progression = progression;
            Score = score;
        }

        public int Value { get; }

        // Existing spawn-frequency prior from SpawnTable, divided by the largest prior among candidates.
        public double Frequency { get; }

        // Flexibility: average over columns of the merge value available there. A column with a useful
        // merge contributes more when the merge has more participants and sits higher in the column.
        // Higher means easier, and it is favored at low pressure.
        public double MergeOpportunity { get; }

        // Placement risk: a column's risk is its fill fraction after placement, reduced by any merge
        // it offers. Congestion blends the least risky column with the mean. Higher means harder.
        public double Congestion { get; }

        // Ladder position: 0 for the smallest candidate and 1 for the largest eligible candidate, on a
        // log scale. Higher-ladder values create longer merge chains that need planning.
        public double Progression { get; }

        public double Score { get; }
    }

    // Scores every eligible candidate for the current board. Read-only: it never mutates the board,
    // never consumes random values, and allocates only the caller-provided result list.
    //
    // Merge evaluation uses the same rule as MergeResolver: the piece lands at the lowest empty cell of
    // a column and merges with orthogonally adjacent equal pieces. Cascades and gravity after a merge
    // are not simulated; that is the heuristic's main limitation.
    public sealed class PieceDifficultyEvaluator
    {
        private readonly PieceGenerationSettings _settings;

        public PieceDifficultyEvaluator(PieceGenerationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        // Fills results with one entry per candidate with positive probability, in candidate order.
        // Outputs the weights used for this pressure so callers can report them.
        public void Evaluate(
            BoardModel board,
            IReadOnlyList<SpawnChance> candidates,
            double normalizedPressure,
            List<CandidateScores> results,
            out ScoreWeights weights)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            weights = _settings.GetWeights(normalizedPressure);

            int columns = board.ColumnCount;
            int visibleRows = board.VisibleRowCount;

            // Landing row per column: the lowest empty cell, or -1 when the column has no free cell.
            var landingRows = new int[columns];
            for (int column = 0; column < columns; column++)
            {
                landingRows[column] = board.TryFindLowestEmptyRow(column, out int row) ? row : -1;
            }

            double maxProbability = 0d;
            int highestValue = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Probability <= 0d)
                {
                    continue;
                }

                maxProbability = Math.Max(maxProbability, candidates[i].Probability);
                highestValue = Math.Max(highestValue, candidates[i].Value);
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                SpawnChance candidate = candidates[i];
                if (candidate.Probability <= 0d)
                {
                    continue;
                }

                double frequency = candidate.Probability / maxProbability;
                EvaluatePlacements(board, candidate.Value, landingRows, visibleRows,
                    out double mergeOpportunity, out double congestion);
                double progression = LadderPosition(candidate.Value, highestValue);

                double score =
                    weights.Frequency * frequency +
                    weights.MergeOpportunity * mergeOpportunity +
                    weights.Congestion * congestion +
                    weights.Progression * progression;

                results.Add(new CandidateScores(
                    candidate.Value,
                    frequency,
                    mergeOpportunity,
                    congestion,
                    progression,
                    ScoreMath.Clamp01(score)));
            }
        }

        private void EvaluatePlacements(
            BoardModel board,
            int value,
            int[] landingRows,
            int visibleRows,
            out double mergeOpportunity,
            out double congestion)
        {
            int columns = landingRows.Length;
            double mergeSum = 0d;
            double riskSum = 0d;
            double riskMin = 1d;

            for (int column = 0; column < columns; column++)
            {
                int row = landingRows[column];
                double risk;

                if (row < 0)
                {
                    // A full column cannot take the piece, so it offers no relief.
                    risk = 1d;
                }
                else
                {
                    double mergeValue = MergeValueAt(board, column, row, value, visibleRows);
                    mergeSum += mergeValue;

                    // Landing in the overflow row counts as a full column.
                    double fill = Math.Min(row + 1, visibleRows) / (double)visibleRows;
                    risk = ScoreMath.Clamp01(fill * (1d - _settings.MergeReliefFactor * mergeValue));
                }

                riskSum += risk;
                riskMin = Math.Min(riskMin, risk);
            }

            mergeOpportunity = ScoreMath.Clamp01(mergeSum / columns);

            double meanRisk = riskSum / columns;
            double bestWeight = ScoreMath.Clamp01(_settings.CongestionBestColumnWeight);
            congestion = ScoreMath.Clamp01(bestWeight * riskMin + (1d - bestWeight) * meanRisk);
        }

        // Merge value of landing a piece at (column, row): zero unless the piece forms a valid merge.
        private double MergeValueAt(BoardModel board, int column, int row, int value, int visibleRows)
        {
            int neighbors = CountEqualNeighbors(board, column, row, value);
            if (neighbors == 0)
            {
                return 0d;
            }

            int participants = neighbors + 1;
            if (!MergeMath.TryCalculateResult(value, participants, _settings.MergeCeilingValue, out _))
            {
                return 0d;
            }

            double rowFraction = Math.Min(row, visibleRows) / (double)visibleRows;
            double bias = ScoreMath.Clamp01(_settings.MergeHeightBias);
            double heightFactor = (1d - bias) + bias * rowFraction;

            return ScoreMath.Clamp01(_settings.GetMergeValue(participants) * heightFactor);
        }

        // Same four-neighbour rule as MergeResolver.CollectEqualNeighbors.
        private static int CountEqualNeighbors(BoardModel board, int column, int row, int value)
        {
            int count = 0;
            count += IsEqualAt(board, column, row + 1, value) ? 1 : 0;
            count += IsEqualAt(board, column, row - 1, value) ? 1 : 0;
            count += IsEqualAt(board, column - 1, row, value) ? 1 : 0;
            count += IsEqualAt(board, column + 1, row, value) ? 1 : 0;
            return count;
        }

        private static bool IsEqualAt(BoardModel board, int column, int row, int value)
        {
            return board.IsValidColumn(column) && board.IsValidRow(row) && board.GetValue(column, row) == value;
        }

        private static double LadderPosition(int value, int highestValue)
        {
            int floor = SpawnTable.MinimumValue;
            if (highestValue <= floor || value <= floor)
            {
                return 0d;
            }

            double position = Math.Log((double)value / floor) / Math.Log((double)highestValue / floor);
            return ScoreMath.Clamp01(position);
        }
    }
}
