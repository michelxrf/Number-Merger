using UnityEngine;

namespace NumberMerge.Gameplay
{
    // Balance settings for the rewarded recharge of Undo and Destroy.
    [CreateAssetMenu(fileName = "RechargeConfig", menuName = "Number Merge/Recharge Config")]
    public sealed class RechargeConfig : ScriptableObject
    {
        public const int DefaultMovesPerRecharge = 20;

        [Tooltip("Tile insertions the player must make, since the last recharge, before a rewarded recharge is offered.")]
        [SerializeField, Min(1)] private int _movesPerRecharge = DefaultMovesPerRecharge;

        public int MovesPerRecharge => _movesPerRecharge;
    }

    // Decides when the rewarded ad icon is shown on a power-up button. Kept free of Unity objects so it can be tested.
    public static class RechargeEligibility
    {
        // The icon is offered only when the power-up is empty, the player has made enough insertions since the
        // last recharge, an ad is available, and the board is in a state where the power-up could be used
        // (buttonConditionsMet: an undo snapshot exists, or enough tiles for Destroy).
        public static bool ShowsAdIcon(
            int usesLeft,
            int movesSinceRecharge,
            int movesPerRecharge,
            bool adAvailable,
            bool isGameOver,
            bool isBusy,
            bool buttonConditionsMet)
        {
            return usesLeft == 0 &&
                   movesSinceRecharge >= Mathf.Max(1, movesPerRecharge) &&
                   adAvailable &&
                   !isGameOver &&
                   !isBusy &&
                   buttonConditionsMet;
        }
    }
}
