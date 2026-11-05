using UnityEngine;

namespace NumberMerge.Gameplay
{
    // Balance settings for the rewarded recharge of Undo and Destroy.
    [CreateAssetMenu(fileName = "RechargeConfig", menuName = "Number Merge/Recharge Config")]
    public sealed class RechargeConfig : ScriptableObject
    {
        public const int DefaultMovesPerRecharge = 20;
        public const float DefaultFeedbackDelay = 0.25f;
        public const float DefaultPetalEmitterScale = 0.2f;
        public const float DefaultPunchScale = 1.2f;
        public const float DefaultPunchDuration = 0.2f;

        [Tooltip("Tile insertions the player must make, since the last recharge, before a rewarded recharge is offered.")]
        [SerializeField, Min(1)] private int _movesPerRecharge = DefaultMovesPerRecharge;

        [Header("Recharge Feedback")]
        [Tooltip("Seconds to wait after the rewarded ad closes before the recharged button shows its feedback.")]
        [SerializeField, Min(0f)] private float _feedbackDelay = DefaultFeedbackDelay;
        [Tooltip("Size of the petal emitter as a fraction of the button's size. Same meaning as the tiles' Petal Emitter Scale.")]
        [SerializeField, Range(0.1f, 1f)] private float _petalEmitterScale = DefaultPetalEmitterScale;
        [Tooltip("Scale the recharged button punches up to (1 = no punch). It settles back to 1.")]
        [SerializeField, Min(1f)] private float _punchScale = DefaultPunchScale;
        [Tooltip("Total duration of the button's scale punch (up and back down), in seconds.")]
        [SerializeField, Min(0f)] private float _punchDuration = DefaultPunchDuration;

        public int MovesPerRecharge => _movesPerRecharge;
        public float FeedbackDelay => _feedbackDelay;
        public float PetalEmitterScale => _petalEmitterScale;
        public float PunchScale => _punchScale;
        public float PunchDuration => _punchDuration;
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
