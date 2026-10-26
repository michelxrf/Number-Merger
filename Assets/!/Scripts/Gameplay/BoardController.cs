using System;
using NumberMerge.Managers;

namespace NumberMerge.Gameplay
{
    public sealed class BoardController
    {
        private readonly CascadeResolver _cascadeResolver;
        private readonly ScoreService _scoreService;

        public BoardController()
            : this(new BoardModel(), new CascadeResolver(), new ScoreService())
        {
        }

        public BoardController(BoardModel board, CascadeResolver cascadeResolver, ScoreService scoreService)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            _cascadeResolver = cascadeResolver ?? throw new ArgumentNullException(nameof(cascadeResolver));
            _scoreService = scoreService ?? throw new ArgumentNullException(nameof(scoreService));

            // A restored board may already be in game over state.
            IsGameOver = HasOverflowedColumn();
        }

        public BoardModel Board { get; }

        // Upper bound for the points a single move can add: every cell merging at the maximum value.
        public long MaxPossibleCascadePoints =>
            (long)Board.TotalRowCount * Board.ColumnCount * MergeMath.MaxTileValue;

        public long CurrentScore => _scoreService.CurrentScore;
        public long Record => _scoreService.Record;
        public bool IsGameOver { get; private set; }

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

            IsGameOver = HasOverflowedColumn();
            move = new MoveResult(before, afterInsertion, column, value, insertedRow, cascade);
            return true;
        }

        private bool HasOverflowedColumn()
        {
            for (int column = 0; column < Board.ColumnCount; column++)
            {
                if (Board.HasOverflow(column))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
