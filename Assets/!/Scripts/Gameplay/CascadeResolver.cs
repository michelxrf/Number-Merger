using System;
using System.Collections.Generic;
using System.Text;

namespace NumberMerge.Gameplay
{
    public sealed class CascadeResolver
    {
        public const int DefaultMaxSteps = 10000;

        private readonly MergeResolver _mergeResolver;
        private readonly int _maxSteps;

        public CascadeResolver(MergeResolver mergeResolver)
            : this(mergeResolver, DefaultMaxSteps)
        {
        }

        public CascadeResolver(MergeResolver mergeResolver, int maxSteps)
        {
            _mergeResolver = mergeResolver ?? throw new ArgumentNullException(nameof(mergeResolver));

            if (maxSteps <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSteps));
            }

            _maxSteps = maxSteps;
        }

        public CascadeResult Resolve(BoardModel board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            board.ApplyGravity();
            BoardSnapshot afterInitialGravity = BoardSnapshot.Capture(board);

            var merges = new List<MergeOutcome>();
            var steps = new List<CascadeStep>();
            var visitedStates = new HashSet<string> { BuildSnapshot(board) };
            int step = 0;

            while (true)
            {
                if (step >= _maxSteps)
                {
                    throw new InvalidOperationException(
                        "Cascade resolution exceeded " + _maxSteps + " steps. Merges applied: " + merges.Count +
                        ". Board: " + BuildSnapshot(board));
                }

                step++;

                BoardSnapshot before = BoardSnapshot.Capture(board);
                int piecesBefore = CountPieces(board);
                if (!_mergeResolver.TryResolveOnce(board, out MergeOutcome merge))
                {
                    break;
                }

                if (merge == null)
                {
                    throw new InvalidOperationException("Cascade resolution reported a merge without outcome data.");
                }

                BoardSnapshot afterMerge = BoardSnapshot.Capture(board);
                merges.Add(merge);

                int expectedReduction = merge.ParticipantCount - 1;
                int piecesAfter = CountPieces(board);

                if (expectedReduction < 1 || piecesAfter != piecesBefore - expectedReduction)
                {
                    throw new InvalidOperationException(
                        "Cascade resolution made no expected progress at step " + step +
                        ". Origin: " + merge.Origin +
                        ", participants: " + merge.ParticipantCount +
                        ", result: " + merge.ResultValue +
                        ", pieces before: " + piecesBefore +
                        ", pieces after: " + piecesAfter +
                        ". Board: " + BuildSnapshot(board));
                }

                board.ApplyGravity();
                BoardSnapshot afterGravity = BoardSnapshot.Capture(board);

                string snapshot = BuildSnapshot(board);
                if (!visitedStates.Add(snapshot))
                {
                    throw new InvalidOperationException(
                        "Cascade resolution entered a cycle at step " + step +
                        ". Merges applied: " + merges.Count +
                        ". Board: " + snapshot);
                }

                steps.Add(new CascadeStep(merge, before, afterMerge, afterGravity));
            }

            return new CascadeResult(
                merges,
                step,
                steps,
                afterInitialGravity,
                BoardSnapshot.Capture(board));
        }

        private static int CountPieces(BoardModel board)
        {
            int count = 0;

            for (int column = 0; column < board.ColumnCount; column++)
            {
                count += board.GetPieceCount(column);
            }

            return count;
        }

        private static string BuildSnapshot(BoardModel board)
        {
            var builder = new StringBuilder();

            for (int row = board.TotalRowCount - 1; row >= 0; row--)
            {
                for (int column = 0; column < board.ColumnCount; column++)
                {
                    builder.Append(board.GetValue(column, row));
                    builder.Append(',');
                }

                builder.Append(';');
            }

            return builder.ToString();
        }
    }
}
