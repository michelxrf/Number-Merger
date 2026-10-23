using System;

namespace NumberMerge.Gameplay
{
    public sealed class PieceGenerator
    {
        public const int TwoValue = 2;
        public const int FourValue = 4;
        public const int FourChancePercent = 10;

        private readonly IRandomSource _random;

        public PieceGenerator()
            : this(new SystemRandomSource())
        {
        }

        public PieceGenerator(IRandomSource random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            Current = GenerateValue();
            Next = GenerateValue();
        }

        public int Current { get; private set; }
        public int Next { get; private set; }

        public void Advance()
        {
            Current = Next;
            Next = GenerateValue();
        }

        public void Reset()
        {
            Current = GenerateValue();
            Next = GenerateValue();
        }

        private int GenerateValue()
        {
            return _random.Next(100) < FourChancePercent ? FourValue : TwoValue;
        }
    }
}
