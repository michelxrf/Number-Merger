using UnityEngine;

namespace NumberMerge.Gameplay
{
    // Balance settings for Destroy. The tiles it removes are picked with the same rules as Revive (ReviveConfig).
    [CreateAssetMenu(fileName = "DestroyConfig", menuName = "Number Merge/Destroy Config")]
    public sealed class DestroyConfig : ScriptableObject
    {
        public const int DefaultMaxUses = 2;

        [Tooltip("Destroy uses a player gets on a fresh install and after a restart. Each use lowers the counter by one.")]
        [SerializeField, Min(0)] private int _maxUses = DefaultMaxUses;

        public int MaxUses => _maxUses;
    }

    // Decides whether Destroy can be used right now. Kept free of Unity objects so it can be tested.
    public static class DestroyEligibility
    {
        // Destroy needs a use left, a playable board (not game over, not animating or previewing),
        // and at least the revive minimum of tiles on the board.
        public static bool CanDestroy(
            int usesLeft,
            bool isGameOver,
            bool isBusy,
            int tileCount,
            int minimumTiles)
        {
            return usesLeft > 0 &&
                   !isGameOver &&
                   !isBusy &&
                   tileCount >= Mathf.Max(1, minimumTiles);
        }
    }
}
