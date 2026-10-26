using System;
using System.Globalization;
using NumberMerge.Gameplay;

namespace NumberMerge.Save
{
    // Serializable state of a board in progress. Stored by SaveSystem as JSON.
    [Serializable]
    public sealed class BoardState
    {
        public int ColumnCount;
        public int VisibleRowCount;

        // Row-major tiles, index = row * ColumnCount + column. Includes the overflow row. 0 = empty.
        public int[] Tiles;

        // Piece about to be inserted.
        public int CurrentPiece;

        // Next piece in the queue.
        public int NextPiece;

        // JsonUtility does not support long, so the score is stored as text.
        public string Score;

        public static BoardState Capture(BoardModel board, int currentPiece, int nextPiece, long score)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var state = new BoardState
            {
                ColumnCount = board.ColumnCount,
                VisibleRowCount = board.VisibleRowCount,
                Tiles = new int[board.TotalRowCount * board.ColumnCount],
                CurrentPiece = currentPiece,
                NextPiece = nextPiece,
                Score = score.ToString(CultureInfo.InvariantCulture)
            };

            for (int row = 0; row < board.TotalRowCount; row++)
            {
                for (int column = 0; column < board.ColumnCount; column++)
                {
                    state.Tiles[row * board.ColumnCount + column] = board.GetValue(column, row);
                }
            }

            return state;
        }

        // Rebuilds the board and score. Returns false when the data is inconsistent.
        public bool TryRestore(out BoardModel board, out long score)
        {
            board = null;
            score = 0;

            if (ColumnCount < 1 || VisibleRowCount < 1 || Tiles == null)
            {
                return false;
            }

            int totalRowCount = VisibleRowCount + 1;
            if (Tiles.Length != totalRowCount * ColumnCount)
            {
                return false;
            }

            if (CurrentPiece <= BoardModel.EmptyValue || NextPiece <= BoardModel.EmptyValue)
            {
                return false;
            }

            if (!long.TryParse(Score, NumberStyles.Integer, CultureInfo.InvariantCulture, out score) || score < 0)
            {
                return false;
            }

            var restored = new BoardModel(ColumnCount, VisibleRowCount);

            for (int row = 0; row < totalRowCount; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    int value = Tiles[row * ColumnCount + column];
                    if (value == BoardModel.EmptyValue)
                    {
                        continue;
                    }

                    if (!restored.TrySetValue(column, row, value))
                    {
                        return false;
                    }
                }
            }

            board = restored;
            return true;
        }
    }
}
