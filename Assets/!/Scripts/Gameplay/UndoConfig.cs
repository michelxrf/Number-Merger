using UnityEngine;

namespace NumberMerge.Gameplay
{
    // Balance settings for Undo: how many times it can be used and how long its animation takes.
    [CreateAssetMenu(fileName = "UndoConfig", menuName = "Number Merge/Undo Config")]
    public sealed class UndoConfig : ScriptableObject
    {
        public const int DefaultMaxUses = 5;
        public const float DefaultAnimationDuration = 0.35f;

        [Tooltip("Undos a player gets on a fresh install and after a restart. Each use lowers the counter by one.")]
        [SerializeField, Min(0)] private int _maxUses = DefaultMaxUses;

        [Tooltip("Seconds the undo animation takes: tiles slide back and the destroyed ones fade in.")]
        [SerializeField, Min(0f)] private float _animationDuration = DefaultAnimationDuration;

        public int MaxUses => _maxUses;
        public float AnimationDuration => _animationDuration;
    }
}
