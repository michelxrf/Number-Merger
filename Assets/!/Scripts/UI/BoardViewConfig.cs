using UnityEngine;

namespace NumberMerge.UI
{
    // Layout and animation settings for BoardView. BoardView reads these values on Start.
    [CreateAssetMenu(fileName = "BoardViewConfig", menuName = "Number Merge/Board View Config")]
    public sealed class BoardViewConfig : ScriptableObject
    {
        [Header("Grid")]
        [Tooltip("Number of columns in the grid.")]
        [SerializeField, Min(1)] private int _columnCount = 5;
        [Tooltip("Number of visible rows. One extra overflow row is added above them.")]
        [SerializeField, Min(1)] private int _visibleRowCount = 7;
        [Tooltip("Gap between cells, in pixels.")]
        [SerializeField, Min(0f)] private float _spacing = 6f;
        [Tooltip("Cell size in pixels. 0 = fit the grid inside the board's RectTransform.")]
        [SerializeField, Min(0f)] private float _cellSizeOverride = 0f;

        [Header("Animation Durations (seconds)")]
        [SerializeField, Min(0f)] private float _insertDuration = 0.18f;
        [SerializeField, Min(0f)] private float _mergeDuration = 0.12f;
        [SerializeField, Min(0f)] private float _popDuration = 0.12f;
        [SerializeField, Min(0f)] private float _gravityDuration = 0.12f;

        public int ColumnCount => _columnCount;
        public int VisibleRowCount => _visibleRowCount;
        public float Spacing => _spacing;
        public float CellSizeOverride => _cellSizeOverride;
        public float InsertDuration => _insertDuration;
        public float MergeDuration => _mergeDuration;
        public float PopDuration => _popDuration;
        public float GravityDuration => _gravityDuration;
    }
}
