using System;

namespace NumberMerge.Gameplay
{
    public sealed class CascadeStep
    {
        public CascadeStep(
            MergeOutcome merge,
            BoardSnapshot before,
            BoardSnapshot afterMerge,
            BoardSnapshot afterGravity)
        {
            Merge = merge ?? throw new ArgumentNullException(nameof(merge));
            Before = before ?? throw new ArgumentNullException(nameof(before));
            AfterMerge = afterMerge ?? throw new ArgumentNullException(nameof(afterMerge));
            AfterGravity = afterGravity ?? throw new ArgumentNullException(nameof(afterGravity));
        }

        public MergeOutcome Merge { get; }
        public BoardSnapshot Before { get; }
        public BoardSnapshot AfterMerge { get; }
        public BoardSnapshot AfterGravity { get; }
    }
}
