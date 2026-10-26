using System;

namespace NumberMerge.Gameplay
{
    public sealed class PieceGenerator
    {
        private readonly IRandomSource _random;

        public PieceGenerator()
            : this(new SystemRandomSource())
        {
        }

        public PieceGenerator(IRandomSource random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int Current { get; private set; }
        public int Next { get; private set; }

        // Generates both pieces of the queue from the current board state.
        public void Reset(BoardModel board)
        {
            Current = GenerateValue(board);
            Next = GenerateValue(board);
        }

        // Restores a saved queue instead of generating a new one.
        public void Restore(int current, int next)
        {
            Current = current;
            Next = next;
        }

        // Promotes Next to Current and generates a new Next from the current board state.
        public void Advance(BoardModel board)
        {
            Current = Next;
            Next = GenerateValue(board);
        }

        private int GenerateValue(BoardModel board)
        {
            return SpawnTable.Pick(SpawnTable.Build(board), _random);
        }
    }
}
