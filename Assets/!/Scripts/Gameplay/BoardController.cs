using System;
using System.Collections.Generic;
using NumberMerge.Managers;

namespace NumberMerge.Gameplay
{
    public sealed class BoardController
    {
        private readonly CascadeResolver _cascadeResolver;
        private readonly ScoreService _scoreService;

        // mergeCeilingValue is PieceGenerationSettings.MergeCeilingValue; it must match the merge resolver's ceiling.
        public BoardController(BoardModel board, CascadeResolver cascadeResolver, ScoreService scoreService, int mergeCeilingValue)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            _cascadeResolver = cascadeResolver ?? throw new ArgumentNullException(nameof(cascadeResolver));
            _scoreService = scoreService ?? throw new ArgumentNullException(nameof(scoreService));

            if (!MergeMath.IsValidCeiling(mergeCeilingValue))
            {
                throw new ArgumentOutOfRangeException(nameof(mergeCeilingValue));
            }

            MergeCeilingValue = mergeCeilingValue;

            // A restored board may already be in game over state.
            IsGameOver = HasOverflowedColumn();
        }

        public BoardModel Board { get; }

        public int MergeCeilingValue { get; }

        // Upper bound for the points a single move can add: every cell merging at the merge ceiling.
        public long MaxPossibleCascadePoints =>
            (long)Board.TotalRowCount * Board.ColumnCount * MergeCeilingValue;

        public long CurrentScore => _scoreService.CurrentScore;
        public long Record => _scoreService.Record;
        public bool IsGameOver { get; private set; }

        // Combos since the owner last called Reset (the result phase resets it before the next move).
        public ComboCounter Combos { get; } = new ComboCounter();

        public bool TryPlay(int column, int value)
        {
            return TryPlay(column, value, out MoveResult _);
        }

        public bool TryPlay(int column, int value, out CascadeResult cascade)
        {
            bool played = TryPlay(column, value, out MoveResult move);
            cascade = played ? move.Cascade : null;
            return played;
        }

        public bool TryPlay(int column, int value, out MoveResult move)
        {
            move = null;

            if (IsGameOver)
            {
                return false;
            }

            if (!_scoreService.CanAdd(MaxPossibleCascadePoints))
            {
                return false;
            }

            BoardSnapshot before = BoardSnapshot.Capture(Board);

            if (!Board.TryInsert(column, value, out int insertedRow))
            {
                return false;
            }

            BoardSnapshot afterInsertion = BoardSnapshot.Capture(Board);
            CascadeResult cascade = _cascadeResolver.Resolve(Board);
            CommitCascade(cascade);
            move = new MoveResult(before, afterInsertion, column, value, insertedRow, cascade);
            return true;
        }

        // Removes the given tiles at once, then resolves the board (gravity and merges).
        // The removal itself scores nothing; merges caused by the resolve score like any other merge.
        // Unlike TryPlay, this is allowed on a game over board: it is how a game over is recovered from.
        // Returns false, without changing the board, when a position is invalid, empty, or repeated.
        public bool TryDestroyTiles(IReadOnlyList<BoardPosition> positions, out DestructionResult destruction)
        {
            destruction = null;

            if (positions == null || positions.Count == 0)
            {
                return false;
            }

            var seen = new HashSet<BoardPosition>();
            for (int i = 0; i < positions.Count; i++)
            {
                BoardPosition position = positions[i];

                if (!Board.IsValidColumn(position.Column) ||
                    !Board.IsValidRow(position.Row) ||
                    Board.IsEmpty(position.Column, position.Row) ||
                    !seen.Add(position))
                {
                    return false;
                }
            }

            if (!_scoreService.CanAdd(MaxPossibleCascadePoints))
            {
                return false;
            }

            BoardSnapshot before = BoardSnapshot.Capture(Board);

            for (int i = 0; i < positions.Count; i++)
            {
                Board.TryRemove(positions[i].Column, positions[i].Row, out _);
            }

            BoardSnapshot afterRemoval = BoardSnapshot.Capture(Board);
            CascadeResult cascade = _cascadeResolver.Resolve(Board);
            CommitCascade(cascade);

            destruction = new DestructionResult(before, afterRemoval, positions, cascade);
            return true;
        }

        // Columns whose stack reaches the overflow row. A game over is caused by one of these.
        public List<int> GetOverflowedColumns()
        {
            var columns = new List<int>();

            for (int column = 0; column < Board.ColumnCount; column++)
            {
                if (Board.HasOverflow(column))
                {
                    columns.Add(column);
                }
            }

            return columns;
        }

        private void CommitCascade(CascadeResult cascade)
        {
            long points = 0;

            for (int i = 0; i < cascade.Merges.Count; i++)
            {
                points += cascade.Merges[i].ResultValue;
            }

            if (!_scoreService.TryAdd(points))
            {
                throw new InvalidOperationException(
                    "Score overflow while committing a resolved move. Current score: " + _scoreService.CurrentScore +
                    ", points: " + points);
            }

            Combos.AddCascade(cascade.MergeCount);
            IsGameOver = HasOverflowedColumn();
        }

        private bool HasOverflowedColumn()
        {
            return GetOverflowedColumns().Count > 0;
        }
    }
}
