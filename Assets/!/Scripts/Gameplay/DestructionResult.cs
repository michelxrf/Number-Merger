using System;
using System.Collections.Generic;

namespace NumberMerge.Gameplay
{
    // Result of BoardController.TryDestroyTiles. Used by the view to animate the destruction and the solve.
    public sealed class DestructionResult
    {
        private readonly BoardPosition[] _destroyed;

        public DestructionResult(
            BoardSnapshot before,
            BoardSnapshot afterRemoval,
            IReadOnlyList<BoardPosition> destroyed,
            CascadeResult cascade)
        {
            if (destroyed == null)
            {
                throw new ArgumentNullException(nameof(destroyed));
            }

            Before = before ?? throw new ArgumentNullException(nameof(before));
            AfterRemoval = afterRemoval ?? throw new ArgumentNullException(nameof(afterRemoval));
            Cascade = cascade ?? throw new ArgumentNullException(nameof(cascade));
            _destroyed = new List<BoardPosition>(destroyed).ToArray();
        }

        // Board before anything was removed.
        public BoardSnapshot Before { get; }

        // Board right after the tiles were removed, before gravity.
        public BoardSnapshot AfterRemoval { get; }

        public IReadOnlyList<BoardPosition> Destroyed => _destroyed;

        // Gravity and merges that followed the removal.
        public CascadeResult Cascade { get; }
    }
}
