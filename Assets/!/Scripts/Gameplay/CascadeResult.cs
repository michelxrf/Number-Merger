using System;
using System.Collections.Generic;

namespace NumberMerge.Gameplay
{
    public sealed class CascadeResult
    {
        private readonly MergeOutcome[] _merges;
        private readonly CascadeStep[] _steps;

        public CascadeResult(
            IReadOnlyList<MergeOutcome> merges,
            int stepCount,
            IReadOnlyList<CascadeStep> steps,
            BoardSnapshot afterInitialGravity,
            BoardSnapshot finalBoard)
        {
            if (merges == null)
            {
                throw new ArgumentNullException(nameof(merges));
            }

            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            _merges = new List<MergeOutcome>(merges).ToArray();
            _steps = new List<CascadeStep>(steps).ToArray();
            StepCount = stepCount;
            AfterInitialGravity = afterInitialGravity ?? throw new ArgumentNullException(nameof(afterInitialGravity));
            FinalBoard = finalBoard ?? throw new ArgumentNullException(nameof(finalBoard));
        }

        public IReadOnlyList<MergeOutcome> Merges => _merges;
        public IReadOnlyList<CascadeStep> Steps => _steps;
        public int MergeCount => _merges.Length;
        public int StepCount { get; }
        public BoardSnapshot AfterInitialGravity { get; }
        public BoardSnapshot FinalBoard { get; }
    }
}
