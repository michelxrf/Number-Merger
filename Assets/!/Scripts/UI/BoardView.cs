using System.Collections;
using System.Collections.Generic;
using NumberMerge.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace NumberMerge.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private TileView _tilePrefab;

        [Header("Grid")]
        [Tooltip("Number of columns in the grid.")]
        [SerializeField, Min(1)] private int _columnCount = 5;
        [Tooltip("Number of visible rows. One extra overflow row is added above them.")]
        [SerializeField, Min(1)] private int _visibleRowCount = 7;
        [Tooltip("Gap between cells, in pixels.")]
        [SerializeField, Min(0f)] private float _spacing = 6f;
        [Tooltip("Cell size in pixels. 0 = fit the grid inside this object's RectTransform.")]
        [SerializeField, Min(0f)] private float _cellSizeOverride = 0f;
        [SerializeField] private Color _cellColor = new Color(0.12f, 0.15f, 0.21f, 1f);
        [SerializeField] private float _insertDuration = 0.18f;
        [SerializeField] private float _mergeDuration = 0.12f;
        [SerializeField] private float _popDuration = 0.12f;
        [SerializeField] private float _gravityDuration = 0.12f;

        private readonly List<TileView> _spawnedTiles = new List<TileView>();
        private TileView[,] _tileGrid;
        private RectTransform _rectTransform;
        private float _cellSize;
        private float _gridWidth;
        private float _gridHeight;
        private bool _built;

        public int ColumnCount => _columnCount;
        public int VisibleRowCount => _visibleRowCount;
        public int TotalRowCount => _visibleRowCount + 1;

        // Size of one cell in pixels, valid after the grid is built.
        public float CellSize
        {
            get
            {
                EnsureBuilt();
                return _cellSize;
            }
        }

        public void Initialize(BoardModel board)
        {
            EnsureBuilt();

            if (board.ColumnCount != ColumnCount || board.TotalRowCount != TotalRowCount)
            {
                Debug.LogError(
                    "BoardView grid (" + ColumnCount + "x" + TotalRowCount + ") does not match the board model (" +
                    board.ColumnCount + "x" + board.TotalRowCount + ").", this);
            }

            SetBoard(BoardSnapshot.Capture(board));
        }

        public void Refresh(BoardModel board)
        {
            if (board == null)
            {
                return;
            }

            EnsureBuilt();
            SetBoard(BoardSnapshot.Capture(board));
        }

        public int GetNearestColumn(Vector2 localPoint)
        {
            EnsureBuilt();

            float step = _cellSize + _spacing;
            float firstCenter = _gridWidth * -0.5f + _cellSize * 0.5f;
            int column = Mathf.RoundToInt((localPoint.x - firstCenter) / step);
            return Mathf.Clamp(column, 0, ColumnCount - 1);
        }

        public float GetColumnCenterX(int column)
        {
            EnsureBuilt();
            return GetPosition(Mathf.Clamp(column, 0, ColumnCount - 1), 0).x;
        }

        public float GetDropStartLocalY()
        {
            EnsureBuilt();
            // Center of the overflow row, which sits one spacing above the top visible row.
            return _gridHeight * 0.5f + _cellSize * 0.5f + _spacing;
        }

        public Vector2 GetDropStartPosition(int column)
        {
            return new Vector2(GetColumnCenterX(column), GetDropStartLocalY());
        }

        public IEnumerator PlayMove(MoveResult move, Vector2? insertionStart = null)
        {
            if (move == null)
            {
                yield break;
            }

            EnsureBuilt();
            SetBoard(move.Before);

            TileView inserted = CreateTile(
                move.Column,
                move.InsertedRow,
                move.Value,
                TotalRowCount - 1,
                false);

            if (inserted != null)
            {
                if (insertionStart.HasValue)
                {
                    inserted.GetComponent<RectTransform>().anchoredPosition = insertionStart.Value;
                }

                yield return AnimateInsertion(inserted, GetPosition(move.Column, move.InsertedRow));
                _tileGrid[move.InsertedRow, move.Column] = inserted;
                inserted.name = BuildTileName(move.Column, move.InsertedRow, move.Value);
            }

            yield return AnimateGravity(move.AfterInsertion, move.Cascade.AfterInitialGravity);

            for (int i = 0; i < move.Cascade.Steps.Count; i++)
            {
                CascadeStep step = move.Cascade.Steps[i];
                yield return AnimateMerge(step);
                yield return AnimateGravity(step.AfterMerge, step.AfterGravity);
            }

            EnsureGridMatches(move.Cascade.FinalBoard);
        }

        private void EnsureBuilt()
        {
            if (_built)
            {
                return;
            }

            _rectTransform = GetComponent<RectTransform>();
            _tileGrid = new TileView[TotalRowCount, ColumnCount];

            _cellSize = ComputeCellSize();
            _gridWidth = ColumnCount * _cellSize + (ColumnCount - 1) * _spacing;
            _gridHeight = VisibleRowCount * _cellSize + (VisibleRowCount - 1) * _spacing;

            // Only the visible rows get a background cell. The overflow row (index VisibleRowCount)
            // exists in the model and is positioned just above the grid, where pieces enter.
            for (int row = 0; row < VisibleRowCount; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    CreateCell(column, row);
                }
            }

            _built = true;
        }

        // The board keeps the size of its own RectTransform. The grid is centered in it, and the
        // cell size is the largest one that fits both ways unless an override is set.
        private float ComputeCellSize()
        {
            if (_cellSizeOverride > 0f)
            {
                return _cellSizeOverride;
            }

            Rect rect = _rectTransform.rect;
            float fitWidth = (rect.width - (ColumnCount - 1) * _spacing) / ColumnCount;
            float fitHeight = (rect.height - (VisibleRowCount - 1) * _spacing) / VisibleRowCount;
            return Mathf.Max(1f, Mathf.Min(fitWidth, fitHeight));
        }

        private void CreateCell(int column, int row)
        {
            var cellObject = new GameObject("Cell_" + column + "_" + row, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cellObject.layer = 5;
            cellObject.transform.SetParent(transform, false);

            var cellRect = cellObject.GetComponent<RectTransform>();
            cellRect.anchorMin = new Vector2(0.5f, 0.5f);
            cellRect.anchorMax = new Vector2(0.5f, 0.5f);
            cellRect.pivot = new Vector2(0.5f, 0.5f);
            cellRect.sizeDelta = new Vector2(_cellSize, _cellSize);
            cellRect.anchoredPosition = GetPosition(column, row);

            var image = cellObject.GetComponent<Image>();
            image.color = _cellColor;
            image.raycastTarget = false;
        }

        private TileView CreateTile(int column, int row, int value, int startRow, bool register)
        {
            if (_tilePrefab == null)
            {
                Debug.LogError("BoardView has no tile prefab assigned.", this);
                return null;
            }

            TileView tile = Instantiate(_tilePrefab, transform, false);
            tile.name = BuildTileName(column, row, value);

            var tileRect = tile.GetComponent<RectTransform>();
            tileRect.anchorMin = new Vector2(0.5f, 0.5f);
            tileRect.anchorMax = new Vector2(0.5f, 0.5f);
            tileRect.pivot = new Vector2(0.5f, 0.5f);
            tileRect.sizeDelta = new Vector2(_cellSize, _cellSize);
            tileRect.anchoredPosition = GetPosition(column, startRow);

            tile.SetValue(value);
            tile.ResetVisual();
            _spawnedTiles.Add(tile);

            if (register)
            {
                _tileGrid[row, column] = tile;
            }

            return tile;
        }

        private void SetBoard(BoardSnapshot snapshot)
        {
            ClearTiles();

            for (int row = 0; row < TotalRowCount; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    int value = snapshot.GetValue(column, row);
                    if (value <= BoardModel.EmptyValue)
                    {
                        continue;
                    }

                    CreateTile(column, row, value, row, true);
                }
            }
        }

        private IEnumerator AnimateInsertion(TileView tile, Vector2 target)
        {
            RectTransform rect = tile.GetComponent<RectTransform>();

            tile.SetScale(0.9f);
            tile.SetAlpha(0.8f);

            if (_insertDuration > 0f)
            {
                LeanTween.move(rect, new Vector3(target.x, target.y, 0f), _insertDuration)
                    .setEase(LeanTweenType.easeOutQuad);
                LeanTween.scale(rect, Vector3.one, _insertDuration)
                    .setEase(LeanTweenType.easeOutQuad);
                LeanTween.value(tile.gameObject, alpha => tile.SetAlpha(alpha), 0.8f, 1f, _insertDuration);

                yield return new WaitForSeconds(_insertDuration);
            }

            rect.anchoredPosition = target;
            tile.ResetVisual();
        }

        private IEnumerator AnimateMerge(CascadeStep step)
        {
            MergeOutcome merge = step.Merge;
            Vector2 target = GetPosition(merge.Origin.Column, merge.Origin.Row);

            var participants = new List<TileView>(merge.ParticipantCount);
            var positions = new List<BoardPosition>(merge.ParticipantCount);

            AddParticipant(merge.Origin, participants, positions);

            for (int i = 0; i < merge.Neighbors.Count; i++)
            {
                AddParticipant(merge.Neighbors[i], participants, positions);
            }

            var startPositions = new List<Vector2>(participants.Count);
            for (int i = 0; i < participants.Count; i++)
            {
                startPositions.Add(participants[i].GetComponent<RectTransform>().anchoredPosition);
            }

            if (_mergeDuration > 0f)
            {
                float elapsed = 0f;

                while (elapsed < _mergeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / _mergeDuration);
                    float eased = t * t * (3f - 2f * t);

                    for (int i = 0; i < participants.Count; i++)
                    {
                        TileView participant = participants[i];
                        if (participant == null)
                        {
                            continue;
                        }

                        var rect = participant.GetComponent<RectTransform>();
                        rect.anchoredPosition = Vector2.LerpUnclamped(startPositions[i], target, eased);
                        participant.SetScale(Mathf.Lerp(1f, 0.2f, eased));
                        participant.SetAlpha(Mathf.Lerp(1f, 0.2f, eased));
                    }

                    yield return null;
                }
            }

            for (int i = 0; i < participants.Count; i++)
            {
                BoardPosition position = positions[i];
                TileView participant = participants[i];

                if (participant == null)
                {
                    continue;
                }

                if (_tileGrid[position.Row, position.Column] == participant)
                {
                    _tileGrid[position.Row, position.Column] = null;
                }

                DestroyTile(participant);
            }

            TileView result = CreateTile(
                merge.Origin.Column,
                merge.Origin.Row,
                merge.ResultValue,
                merge.Origin.Row,
                false);

            if (result == null)
            {
                yield break;
            }

            _tileGrid[merge.Origin.Row, merge.Origin.Column] = result;
            result.SetScale(0.25f);
            result.SetAlpha(0f);

            if (_popDuration > 0f)
            {
                float elapsed = 0f;

                while (elapsed < _popDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / _popDuration);
                    float eased = 1f - (1f - t) * (1f - t);

                    result.SetScale(Mathf.Lerp(0.25f, 1f, eased));
                    result.SetAlpha(Mathf.Lerp(0f, 1f, eased));
                    yield return null;
                }
            }

            result.ResetVisual();
        }

        private IEnumerator AnimateGravity(BoardSnapshot from, BoardSnapshot to)
        {
            if (from == null || to == null)
            {
                yield break;
            }

            var moves = new List<TileMove>();

            for (int column = 0; column < ColumnCount; column++)
            {
                var sourceRows = new List<int>();
                var targetRows = new List<int>();

                for (int row = 0; row < TotalRowCount; row++)
                {
                    if (from.GetValue(column, row) > BoardModel.EmptyValue)
                    {
                        sourceRows.Add(row);
                    }

                    if (to.GetValue(column, row) > BoardModel.EmptyValue)
                    {
                        targetRows.Add(row);
                    }
                }

                if (sourceRows.Count != targetRows.Count)
                {
                    SetBoard(to);
                    yield break;
                }

                for (int i = 0; i < sourceRows.Count; i++)
                {
                    int sourceRow = sourceRows[i];
                    int targetRow = targetRows[i];

                    if (from.GetValue(column, sourceRow) != to.GetValue(column, targetRow))
                    {
                        SetBoard(to);
                        yield break;
                    }

                    TileView tile = _tileGrid[sourceRow, column];
                    if (tile == null)
                    {
                        SetBoard(to);
                        yield break;
                    }

                    moves.Add(new TileMove
                    {
                        Tile = tile,
                        Start = tile.GetComponent<RectTransform>().anchoredPosition,
                        Target = GetPosition(column, targetRow),
                        Column = column,
                        Row = targetRow,
                        Value = to.GetValue(column, targetRow)
                    });
                }
            }

            bool moved = false;
            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].Start != moves[i].Target)
                {
                    moved = true;
                    break;
                }
            }

            if (!moved)
            {
                yield break;
            }

            if (_gravityDuration > 0f)
            {
                float elapsed = 0f;

                while (elapsed < _gravityDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / _gravityDuration);
                    float eased = t * t * (3f - 2f * t);

                    for (int i = 0; i < moves.Count; i++)
                    {
                        TileMove move = moves[i];
                        move.Tile.GetComponent<RectTransform>().anchoredPosition =
                            Vector2.LerpUnclamped(move.Start, move.Target, eased);
                    }

                    yield return null;
                }
            }

            for (int row = 0; row < TotalRowCount; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    _tileGrid[row, column] = null;
                }
            }

            for (int i = 0; i < moves.Count; i++)
            {
                TileMove move = moves[i];
                move.Tile.GetComponent<RectTransform>().anchoredPosition = move.Target;
                move.Tile.name = BuildTileName(move.Column, move.Row, move.Value);
                _tileGrid[move.Row, move.Column] = move.Tile;
            }
        }

        private void EnsureGridMatches(BoardSnapshot snapshot)
        {
            for (int row = 0; row < TotalRowCount; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    int value = snapshot.GetValue(column, row);
                    TileView tile = _tileGrid[row, column];

                    if (value <= BoardModel.EmptyValue)
                    {
                        if (tile != null)
                        {
                            _tileGrid[row, column] = null;
                            DestroyTile(tile);
                        }

                        continue;
                    }

                    if (tile == null)
                    {
                        CreateTile(column, row, value, row, true);
                        continue;
                    }

                    tile.SetValue(value);
                    tile.ResetVisual();
                    tile.name = BuildTileName(column, row, value);
                    tile.GetComponent<RectTransform>().anchoredPosition = GetPosition(column, row);
                }
            }
        }

        private void AddParticipant(BoardPosition position, List<TileView> participants, List<BoardPosition> positions)
        {
            TileView tile = _tileGrid[position.Row, position.Column];
            if (tile == null)
            {
                return;
            }

            participants.Add(tile);
            positions.Add(position);
        }

        private void DestroyTile(TileView tile)
        {
            if (tile == null)
            {
                return;
            }

            _spawnedTiles.Remove(tile);
            tile.gameObject.SetActive(false);
            Destroy(tile.gameObject);
        }

        private void ClearTiles()
        {
            for (int i = 0; i < _spawnedTiles.Count; i++)
            {
                TileView tile = _spawnedTiles[i];
                if (tile == null)
                {
                    continue;
                }

                tile.gameObject.SetActive(false);
                Destroy(tile.gameObject);
            }

            _spawnedTiles.Clear();

            for (int row = 0; row < TotalRowCount; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    _tileGrid[row, column] = null;
                }
            }
        }

        private Vector2 GetPosition(int column, int row)
        {
            float step = _cellSize + _spacing;
            float x = _gridWidth * -0.5f + _cellSize * 0.5f + column * step;
            float y = _gridHeight * -0.5f + _cellSize * 0.5f + row * step;
            return new Vector2(x, y);
        }

        private static string BuildTileName(int column, int row, int value)
        {
            return "Tile_" + column + "_" + row + "_" + value;
        }

        private struct TileMove
        {
            public TileView Tile;
            public Vector2 Start;
            public Vector2 Target;
            public int Column;
            public int Row;
            public int Value;
        }
    }
}
