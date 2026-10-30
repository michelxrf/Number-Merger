using System;

namespace NumberMerge.Managers
{
    // Counts chained combinations for the current move. A chain is every cascade merge after the first
    // merge of a move. The owner resets it once the result phase is over and the player regains control.
    public sealed class ComboCounter
    {
        public int Count { get; private set; }

        // mergeCount is the number of merges one move's cascade produced. The first merge is not a combo.
        public void AddCascade(int mergeCount)
        {
            if (mergeCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(mergeCount));
            }

            Count += Math.Max(0, mergeCount - 1);
        }

        public void Reset()
        {
            Count = 0;
        }
    }
}
