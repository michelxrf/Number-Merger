using System;

namespace NumberMerge.Gameplay
{
    public interface IRandomSource
    {
        int Next(int maxExclusive);
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SystemRandomSource()
            : this(new Random())
        {
        }

        public SystemRandomSource(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            return _random.Next(maxExclusive);
        }
    }

    // Deterministic generator (SplitMix64). Its whole state is one ulong, so it can be saved with the board
    // and rewound by undo. Gameplay uses one instance for every random decision.
    public sealed class SeededRandomSource : IRandomSource
    {
        private const ulong Increment = 0x9E3779B97F4A7C15UL;
        private const ulong MixA = 0xBF58476D1CE4E5B9UL;
        private const ulong MixB = 0x94D049BB133111EBUL;

        private ulong _state;

        public SeededRandomSource(ulong state)
        {
            _state = state;
        }

        // Current generator state. Setting it resumes the sequence from that point.
        public ulong State
        {
            get => _state;
            set => _state = value;
        }

        // A seed that differs between games. Not reproducible, by design.
        public static ulong NewSeed()
        {
            var bytes = new byte[8];
            new Random().NextBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            return (int)(NextUInt64() % (ulong)maxExclusive);
        }

        private ulong NextUInt64()
        {
            _state += Increment;
            ulong z = _state;
            z = (z ^ (z >> 30)) * MixA;
            z = (z ^ (z >> 27)) * MixB;
            return z ^ (z >> 31);
        }
    }
}
