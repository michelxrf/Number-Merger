using System;

namespace NumberMerge.Gameplay
{
    public sealed class BoardModel
    {
        public const int ColumnCount = 5;
        public const int VisibleRowCount = 7;
        public const int TotalRowCount = VisibleRowCount + 1;
        public const int EmptyValue = 0;

        private readonly int[,] _tiles = new int[TotalRowCount, ColumnCount];

        public int GetValue(int column, int row)
        {
            ValidateColumn(column);
            ValidateRow(row);

            return _tiles[row, column];
        }

        public bool IsEmpty(int column, int row)
        {
            return GetValue(column, row) == EmptyValue;
        }

        public bool TryInsert(int column, int value)
        {
            return TryInsert(column, value, out _);
        }

        public bool TryInsert(int column, int value, out int row)
        {
            row = -1;

            if (!IsValidColumn(column) || value <= EmptyValue)
            {
                return false;
            }

            if (!TryFindLowestEmptyRow(column, out int targetRow))
            {
                return false;
            }

            _tiles[targetRow, column] = value;
            row = targetRow;
            return true;
        }

        public bool TrySetValue(int column, int row, int value)
        {
            if (!IsValidColumn(column) || !IsValidRow(row) || value <= EmptyValue)
            {
                return false;
            }

            _tiles[row, column] = value;
            return true;
        }

        public bool TryRemove(int column, int row, out int value)
        {
            value = EmptyValue;

            if (!IsValidColumn(column) || !IsValidRow(row))
            {
                return false;
            }

            int currentValue = _tiles[row, column];
            if (currentValue == EmptyValue)
            {
                return false;
            }

            _tiles[row, column] = EmptyValue;
            value = currentValue;
            return true;
        }

        public bool TryFindLowestEmptyRow(int column, out int row)
        {
            row = -1;

            if (!IsValidColumn(column))
            {
                return false;
            }

            for (int candidate = 0; candidate < TotalRowCount; candidate++)
            {
                if (_tiles[candidate, column] == EmptyValue)
                {
                    row = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool ApplyGravity()
        {
            bool moved = false;

            for (int column = 0; column < ColumnCount; column++)
            {
                int writeRow = 0;

                for (int readRow = 0; readRow < TotalRowCount; readRow++)
                {
                    if (_tiles[readRow, column] == EmptyValue)
                    {
                        continue;
                    }

                    if (readRow != writeRow)
                    {
                        _tiles[writeRow, column] = _tiles[readRow, column];
                        _tiles[readRow, column] = EmptyValue;
                        moved = true;
                    }

                    writeRow++;
                }
            }

            return moved;
        }

        public int GetPieceCount(int column)
        {
            ValidateColumn(column);

            int count = 0;
            for (int row = 0; row < TotalRowCount; row++)
            {
                if (_tiles[row, column] != EmptyValue)
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
                if (_tiles[row, column] != EmptyValue)
                {
                    return row + 1;
                }
            }

            return 0;
        }

        public bool IsColumnFull(int column)
        {
            return GetPieceCount(column) == TotalRowCount;
        }

        public bool HasOverflow(int column)
        {
            return GetPieceCount(column) > VisibleRowCount;
        }

        public static bool IsValidColumn(int column)
        {
            return column >= 0 && column < ColumnCount;
        }

        public static bool IsValidRow(int row)
        {
            return row >= 0 && row < TotalRowCount;
        }

        private static void ValidateColumn(int column)
        {
            if (!IsValidColumn(column))
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }
        }

        private static void ValidateRow(int row)
        {
            if (!IsValidRow(row))
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }
        }
    }
}
