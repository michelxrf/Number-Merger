using System;

namespace NumberMerge.Gameplay
{
    public sealed class BoardSnapshot
    {
        private readonly int[,] _values = new int[BoardModel.TotalRowCount, BoardModel.ColumnCount];

        public BoardSnapshot(int[,] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.GetLength(0) != BoardModel.TotalRowCount ||
                values.GetLength(1) != BoardModel.ColumnCount)
            {
                throw new ArgumentException("Snapshot dimensions do not match the board.", nameof(values));
            }

            Array.Copy(values, _values, values.Length);
        }

        public static BoardSnapshot Capture(BoardModel board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var values = new int[BoardModel.TotalRowCount, BoardModel.ColumnCount];

            for (int row = 0; row < BoardModel.TotalRowCount; row++)
            {
                for (int column = 0; column < BoardModel.ColumnCount; column++)
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
            for (int row = 0; row < BoardModel.TotalRowCount; row++)
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

            for (int row = BoardModel.TotalRowCount - 1; row >= 0; row--)
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

        private static void ValidateColumn(int column)
        {
            if (!BoardModel.IsValidColumn(column))
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }
        }

        private static void ValidateRow(int row)
        {
            if (!BoardModel.IsValidRow(row))
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }
        }
    }
}
