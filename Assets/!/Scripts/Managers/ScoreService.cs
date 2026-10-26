using System;

namespace NumberMerge.Managers
{
    public sealed class ScoreService
    {
        public ScoreService(long record = 0)
            : this(record, 0)
        {
        }

        // Restores a score in progress. The record is never lower than the restored score.
        public ScoreService(long record, long currentScore)
        {
            if (record < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(record));
            }

            if (currentScore < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentScore));
            }

            CurrentScore = currentScore;
            Record = Math.Max(record, currentScore);
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
