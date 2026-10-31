using System;
using System.Collections.Generic;

namespace NumberMerge.Gameplay
{
    // Picks the tiles removed by a revive.
    //
    // The count is maxFraction of the tiles on the board (rounded down), clamped to at least minTiles.
    // Only which tiles are picked is random. Every overflowed column always loses its top tile, so the
    // board is no longer in game over after the removal and gravity. That can make the count larger.
    public static class ReviveSelection
    {
        public const int DefaultMinTiles = 1;
        public const float DefaultMaxFraction = 0.2f;

        public static List<BoardPosition> Choose(
            BoardModel board,
            IRandomSource random,
            int minTiles = DefaultMinTiles,
            float maxFraction = DefaultMaxFraction)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var tiles = new List<BoardPosition>();
            for (int column = 0; column < board.ColumnCount; column++)
            {
                for (int row = 0; row < board.TotalRowCount; row++)
                {
                    if (!board.IsEmpty(column, row))
                    {
                        tiles.Add(new BoardPosition(column, row));
                    }
                }
            }

            int tileCount = tiles.Count;
            if (tileCount == 0)
            {
                return new List<BoardPosition>();
            }

            // Count is the fraction of the tiles (rounded down), clamped to the minimum and to the tiles that exist.
            float fraction = Math.Max(0f, Math.Min(1f, maxFraction));
            int targetCount = (int)Math.Floor(tileCount * fraction);
            targetCount = Math.Min(tileCount, Math.Max(Math.Max(1, minTiles), targetCount));

            // Mandatory: the top tile of each overflowed column.
            var chosen = new List<BoardPosition>();
            var chosenSet = new HashSet<BoardPosition>();
            for (int column = 0; column < board.ColumnCount; column++)
            {
                if (!board.HasOverflow(column))
                {
                    continue;
                }

                var top = new BoardPosition(column, board.GetHeight(column) - 1);
                chosen.Add(top);
                chosenSet.Add(top);
            }

            targetCount = Math.Max(targetCount, chosen.Count);

            // Remaining picks: shuffle the other tiles (Fisher-Yates) and take from the front.
            var pool = new List<BoardPosition>(tileCount);
            for (int i = 0; i < tiles.Count; i++)
            {
                if (!chosenSet.Contains(tiles[i]))
                {
                    pool.Add(tiles[i]);
                }
            }

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                BoardPosition swap = pool[i];
                pool[i] = pool[j];
                pool[j] = swap;
            }

            for (int i = 0; chosen.Count < targetCount && i < pool.Count; i++)
            {
                chosen.Add(pool[i]);
            }

            return chosen;
        }
    }
}
