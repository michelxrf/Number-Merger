using System;

namespace NumberMerge.Managers
{
    public sealed class ScoreService
    {
        public ScoreService(long record = 0)
        {
            if (record < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(record));
            }

            Record = record;
        }

        public long CurrentScore { get; private set; }
        public long Record { get; private set; }

        public bool CanAdd(long points)
        {
            return points >= 0 && CurrentScore <= long.MaxValue - points;
        }

        public bool TryAdd(long points)
        {
            if (!CanAdd(points))
            {
                return false;
            }

            CurrentScore += points;

            if (CurrentScore > Record)
            {
                Record = CurrentScore;
            }

            return true;
        }
    }
}
