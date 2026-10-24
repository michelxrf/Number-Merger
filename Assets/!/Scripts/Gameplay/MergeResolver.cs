using System;
using System.Collections.Generic;

namespace NumberMerge.Gameplay
{
    public sealed class MergeResolver
    {
        private readonly IRandomSource _random;

        public MergeResolver()
            : this(new SystemRandomSource())
        {
        }

        public MergeResolver(IRandomSource random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public bool TryResolveOnce(BoardModel board, out MergeOutcome outcome)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            outcome = null;
            List<MergeOutcome> candidates = FindCandidates(board, out int highestPriority);

            for (int priority = highestPriority; priority >= 1; priority--)
            {
                List<MergeOutcome> tier = FilterByPriority(candidates, priority);
                if (tier.Count == 0)
                {
                    continue;
                }

                int index = tier.Count == 1 ? 0 : _random.Next(tier.Count);
                MergeOutcome selected = tier[index];

                if (!Apply(board, selected))
                {
                    return false;
                }

                outcome = selected;
                return true;
            }

            return false;
        }

        private static List<MergeOutcome> FindCandidates(BoardModel board, out int highestPriority)
        {
            var candidates = new List<MergeOutcome>();
            highestPriority = 0;

            for (int row = 0; row < board.TotalRowCount; row++)
            {
                for (int column = 0; column < board.ColumnCount; column++)
                {
                    int value = board.GetValue(column, row);
                    if (value <= BoardModel.EmptyValue)
                    {
                        continue;
                    }

                    List<BoardPosition> neighbors = CollectEqualNeighbors(board, column, row, value);
                    if (neighbors.Count == 0)
                    {
                        continue;
                    }

                    if (neighbors.Count > highestPriority)
                    {
                        highestPriority = neighbors.Count;
                    }

                    if (!MergeMath.TryCalculateResult(value, neighbors.Count + 1, out int resultValue))
                    {
                        continue;
                    }

                    candidates.Add(new MergeOutcome(new BoardPosition(column, row), value, resultValue, neighbors));
                }
            }

            return candidates;
        }

        private static List<BoardPosition> CollectEqualNeighbors(BoardModel board, int column, int row, int value)
        {
            var neighbors = new List<BoardPosition>(4);
            AddNeighborIfEqual(board, neighbors, column, row + 1, value);
            AddNeighborIfEqual(board, neighbors, column, row - 1, value);
            AddNeighborIfEqual(board, neighbors, column - 1, row, value);
            AddNeighborIfEqual(board, neighbors, column + 1, row, value);
            return neighbors;
        }

        private static void AddNeighborIfEqual(
            BoardModel board,
            List<BoardPosition> neighbors,
            int column,
            int row,
            int value)
        {
            if (!board.IsValidColumn(column) || !board.IsValidRow(row))
            {
                return;
            }

            if (board.GetValue(column, row) == value)
            {
                neighbors.Add(new BoardPosition(column, row));
            }
        }

        private static List<MergeOutcome> FilterByPriority(List<MergeOutcome> candidates, int priority)
        {
            var filtered = new List<MergeOutcome>();

            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].NeighborCount == priority)
                {
                    filtered.Add(candidates[i]);
                }
            }

            return filtered;
        }

        private static bool Apply(BoardModel board, MergeOutcome outcome)
        {
            if (!board.TrySetValue(outcome.Origin.Column, outcome.Origin.Row, outcome.ResultValue))
            {
                return false;
            }

            for (int i = 0; i < outcome.Neighbors.Count; i++)
            {
                BoardPosition neighbor = outcome.Neighbors[i];
                if (!board.TryRemove(neighbor.Column, neighbor.Row, out _))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
