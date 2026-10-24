using System;
using System.Collections.Generic;

namespace NumberMerge.Gameplay
{
    public sealed class BoardModel
    {
        public const int DefaultColumnCount = 5;
        public const int DefaultVisibleRowCount = 7;
        public const int EmptyValue = 0;

        private readonly int[,] _tiles;
        private readonly Dictionary<int, int> _valueCounts = new Dictionary<int, int>();

        public BoardModel()
            : this(DefaultColumnCount, DefaultVisibleRowCount)
        {
        }

        // visibleRowCount excludes the overflow row, which is always stored as the last row.
        public BoardModel(int columnCount, int visibleRowCount)
        {
            if (columnCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(columnCount));
            }

            if (visibleRowCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(visibleRowCount));
            }

            ColumnCount = columnCount;
            VisibleRowCount = visibleRowCount;
            TotalRowCount = visibleRowCount + 1;
            _tiles = new int[TotalRowCount, ColumnCount];
        }

        public int ColumnCount { get; }
        public int VisibleRowCount { get; }
        public int TotalRowCount { get; }

        // Highest and second highest distinct piece values on the board (EmptyValue when absent).
        public int HighestValue { get; private set; } = EmptyValue;
        public int SecondHighestValue { get; private set; } = EmptyValue;

        public int GetValueCount(int value)
        {
            return _valueCounts.TryGetValue(value, out int count) ? count : 0;
        }

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
            AddValue(value);
            row = targetRow;
            return true;
        }

        public bool TrySetValue(int column, int row, int value)
        {
            if (!IsValidColumn(column) || !IsValidRow(row) || value <= EmptyValue)
            {
                return false;
            }

            int previousValue = _tiles[row, column];
            _tiles[row, column] = value;

            if (previousValue != EmptyValue)
            {
                RemoveValue(previousValue);
            }

            AddValue(value);
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
            RemoveValue(currentValue);
            value = currentValue;
            return true;
        }

        private void AddValue(int value)
        {
            _valueCounts.TryGetValue(value, out int count);
            _valueCounts[value] = count + 1;

            // A new distinct value can change the top two values; otherwise nothing changes.
            if (count == 0)
            {
                RecalculateExtremes();
            }
        }

        private void RemoveValue(int value)
        {
            int count = GetValueCount(value) - 1;

            if (count <= 0)
            {
                _valueCounts.Remove(value);
                RecalculateExtremes();
            }
            else
            {
                _valueCounts[value] = count;
            }
        }

        private void RecalculateExtremes()
        {
            int highest = EmptyValue;
            int second = EmptyValue;

            foreach (int value in _valueCounts.Keys)
            {
                if (value > highest)
                {
                    second = highest;
                    highest = value;
                }
                else if (value > second)
                {
                    second = value;
                }
            }

            HighestValue = highest;
            SecondHighestValue = second;
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

        public bool IsValidColumn(int column)
        {
            return column >= 0 && column < ColumnCount;
        }

        public bool IsValidRow(int row)
        {
            return row >= 0 && row < TotalRowCount;
        }

        private void ValidateColumn(int column)
        {
            if (!IsValidColumn(column))
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }
        }

        private void ValidateRow(int row)
        {
            if (!IsValidRow(row))
            {
                throw new ArgumentOutOfRangeException(nameof(row));
            }
        }
    }
}
