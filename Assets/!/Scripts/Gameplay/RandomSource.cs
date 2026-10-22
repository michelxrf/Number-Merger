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
}
