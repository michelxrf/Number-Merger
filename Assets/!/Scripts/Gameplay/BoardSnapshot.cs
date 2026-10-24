using System;

namespace NumberMerge.Gameplay
{
    public sealed class BoardSnapshot
    {
        private readonly int[,] _values;

        public BoardSnapshot(int[,] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.GetLength(0) < 1 || values.GetLength(1) < 1)
            {
                throw new ArgumentException("Snapshot must have at least one row and one column.", nameof(values));
            }

            _values = (int[,])values.Clone();
        }

        public int TotalRowCount => _values.GetLength(0);
        public int ColumnCount => _values.GetLength(1);

        public static BoardSnapshot Capture(BoardModel board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var values = new int[board.TotalRowCount, board.ColumnCount];

            for (int row = 0; row < board.TotalRowCount; row++)
            {
                for (int column = 0; column < board.ColumnCount; column++)
                {
                    values[row, column] = board.GetValue(column, row);
                }
            }

            return new BoardSnapshot(values);
        }

        public int GetValue(int column, int row)
        {
            ValidateColumn(column);
            ValidateRow(row);

            return _values[row, column];
        }

        public int GetPieceCount(int column)
        {
            ValidateColumn(column);

            int count = 0;
            for (int row = 0; row < TotalRowCount; row++)
            {
                if (_values[row, column] != BoardModel.EmptyValue)
                {
                    count++;
                }
            }

            return count;
        }

        public int GetHeight(int column)
        {
            ValidateColumn(column);

            for (int row = TotalRowCount - 1; row >= 0; row--)
            {
                if (_values[row, column] != BoardModel.EmptyValue)
                {
                    return row + 1;
                }
            }

            return 0;
        }

        public bool IsEmpty(int column, int row)
        {
            return GetValue(column, row) == BoardModel.EmptyValue;
        }

        private void ValidateColumn(int column)
        {
            if (column < 0 || column >= ColumnCount)
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }
        }

        private void ValidateRow(int row)
        {
            if (row < 0 || row >= TotalRowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }
        }
    }
}
