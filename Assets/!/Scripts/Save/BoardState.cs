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

        // State of the gameplay random source (ulong, stored as text). Null in saves made before it existed.
        public string RandomState;

        // The playable state before the last insertion, for Undo. Stored flat because JsonUtility drops fields
        // whose type contains the same type. UndoTiles is null when there is nothing to undo.
        // The undo state has the same ColumnCount and VisibleRowCount as the board.
        public int[] UndoTiles;
        public int UndoCurrentPiece;
        public int UndoNextPiece;
        public string UndoScore;
        public string UndoRandomState;

        // Tile insertions since the last rewarded recharge (shared by Undo and Destroy). The undo copy is the
        // value before the undone move, so undoing does not count as a move.
        public int MovesSinceRecharge;
        public int UndoMovesSinceRecharge;

        public bool TryGetRandomState(out ulong state)
        {
            return ulong.TryParse(RandomState, NumberStyles.Integer, CultureInfo.InvariantCulture, out state);
        }

        // Undo state as a standalone BoardState (its own undo fields stay empty). Null when there is none.
        public BoardState GetUndoState()
        {
            // JsonUtility loads a missing array as an empty array, so empty means "no undo" too.
            if (UndoTiles == null || UndoTiles.Length == 0)
            {
                return null;
            }

            return new BoardState
            {
                ColumnCount = ColumnCount,
                VisibleRowCount = VisibleRowCount,
                Tiles = UndoTiles,
                CurrentPiece = UndoCurrentPiece,
                NextPiece = UndoNextPiece,
                Score = UndoScore,
                RandomState = UndoRandomState,
                MovesSinceRecharge = UndoMovesSinceRecharge
            };
        }

        // Copies a standalone state into the undo fields. Null clears them.
        public void SetUndoState(BoardState undo)
        {
            if (undo == null)
            {
                UndoTiles = null;
                UndoCurrentPiece = 0;
                UndoNextPiece = 0;
                UndoScore = null;
                UndoRandomState = null;
                UndoMovesSinceRecharge = 0;
                return;
            }

            UndoTiles = undo.Tiles;
            UndoCurrentPiece = undo.CurrentPiece;
            UndoNextPiece = undo.NextPiece;
            UndoScore = undo.Score;
            UndoRandomState = undo.RandomState;
            UndoMovesSinceRecharge = undo.MovesSinceRecharge;
        }

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
