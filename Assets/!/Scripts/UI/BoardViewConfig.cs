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
        [Tooltip("Fade-out of destroyed tiles (revive).")]
        [SerializeField, Min(0f)] private float _destroyDuration = 0.5f;
        [Tooltip("Time for destroyed tiles to turn to the blink color before they fade out. The petal burst starts with it.")]
        [SerializeField, Min(0f)] private float _blinkDuration = 0.2f;
        [Tooltip("Color destroyed tiles turn to. It stays on them while they fade out.")]
        [SerializeField] private Color _blinkColor = new Color(1f, 0.25f, 0.25f, 1f);
        [Tooltip("Scale a destroyed tile punches up to during the blink (1 = no punch). It settles back to 1.")]
        [SerializeField, Min(1f)] private float _punchScale = 1.2f;
        [Tooltip("Size of the petal emitter as a fraction of the tile size. Lower = petals start closer to the tile center.")]
        [SerializeField, Range(0.1f, 1f)] private float _petalEmitterScale = 0.2f;

        public int ColumnCount => _columnCount;
        public int VisibleRowCount => _visibleRowCount;
        public float Spacing => _spacing;
        public float CellSizeOverride => _cellSizeOverride;
        public float InsertDuration => _insertDuration;
        public float MergeDuration => _mergeDuration;
        public float PopDuration => _popDuration;
        public float GravityDuration => _gravityDuration;
        public float DestroyDuration => _destroyDuration;
        public float BlinkDuration => _blinkDuration;
        public Color BlinkColor => _blinkColor;
        public float PunchScale => _punchScale;
        public float PetalEmitterScale => _petalEmitterScale;
    }
}
