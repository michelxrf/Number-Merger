using System;
using System.Text;
using NumberMerge.Gameplay;

namespace NumberMerge.Gameplay.Tests
{
    // Builds boards for tests. Each argument is one column listed bottom to top.
    internal static class TestBoards
    {
        public static BoardModel Create(params int[][] columns)
        {
            var board = new BoardModel();

            for (int column = 0; column < columns.Length && column < board.ColumnCount; column++)
            {
                for (int i = 0; i < columns[column].Length; i++)
                {
                    if (!board.TryInsert(column, columns[column][i]))
                    {
                        throw new InvalidOperationException("Test board column overflow at column " + column);
                    }
                }
            }

            return board;
        }

        public static int[] Column(int value, int count)
        {
            var values = new int[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = value;
            }

            return values;
        }

        public static int[] NoColumn()
        {
            return new int[0];
        }

        // Captures every cell (including overflow) and the extremes, so mutation is easy to detect.
        public static string Snapshot(BoardModel board)
        {
            var builder = new StringBuilder();
            for (int row = 0; row < board.TotalRowCount; row++)
            {
                for (int column = 0; column < board.ColumnCount; column++)
                {
                    builder.Append(board.GetValue(column, row)).Append(',');
                }

                builder.Append(';');
            }

            builder.Append("H=").Append(board.HighestValue).Append(" S=").Append(board.SecondHighestValue);
            return builder.ToString();
        }
    }
}
