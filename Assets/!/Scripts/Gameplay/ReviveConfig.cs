using UnityEngine;

namespace NumberMerge.Gameplay
{
    // Balance settings for the revive: how many tiles it removes and when.
    [CreateAssetMenu(fileName = "ReviveConfig", menuName = "Number Merge/Revive Config")]
    public sealed class ReviveConfig : ScriptableObject
    {
        [Tooltip("Fewest tiles a revive removes. Overflowed columns always lose their top tile, which can exceed this.")]
        [SerializeField, Min(1)] private int _minDestroyedTiles = ReviveSelection.DefaultMinTiles;

        [Tooltip("Largest share of the board's tiles a revive removes (0 to 1). The count is rounded down.")]
        [SerializeField, Range(0f, 1f)] private float _maxDestroyedFraction = ReviveSelection.DefaultMaxFraction;

        [Tooltip("Seconds between the Game Over screen finishing its fade-out and the tile destruction starting (real time).")]
        [SerializeField, Min(0f)] private float _destructionDelay = 0.5f;

        public int MinDestroyedTiles => _minDestroyedTiles;
        public float MaxDestroyedFraction => _maxDestroyedFraction;
        public float DestructionDelay => _destructionDelay;
    }
}
