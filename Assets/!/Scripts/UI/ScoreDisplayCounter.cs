using System;

namespace NumberMerge.UI
{
    // Tracks the score shown on screen while floating score values are still travelling to it.
    // Displayed is the number the player sees. Pending is points that have spawned a floater but have not arrived yet.
    public sealed class ScoreDisplayCounter
    {
        public long Displayed { get; private set; }

        public long Pending { get; private set; }

        // Sets both values to the given score with nothing in flight.
        public void SyncTo(long score)
        {
            if (score < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(score));
            }

            Displayed = score;
            Pending = 0;
        }

        // Called when a floater for these points is spawned.
        public void AddPending(long points)
        {
            if (points < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points));
            }

            Pending += points;
        }

        // Called when the floater for these points reaches the score. Returns the new displayed value.
        public long Arrive(long points)
        {
            if (points < 0 || points > Pending)
            {
                throw new InvalidOperationException(
                    "Arrived points (" + points + ") do not match the pending points (" + Pending + ").");
            }

            Pending -= points;
            Displayed += points;
            return Displayed;
        }
    }
}
