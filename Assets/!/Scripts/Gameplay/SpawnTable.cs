using System;
using System.Collections.Generic;

namespace NumberMerge.Gameplay
{
    public readonly struct SpawnChance
    {
        public SpawnChance(int value, double probability)
        {
            Value = value;
            Probability = probability;
        }

        public int Value { get; }
        public double Probability { get; }
    }

    // Computes the probability of each value spawned for the next piece.
    //
    // Rules:
    // - Spawned values are always lower than the second highest value on the board, so both the
    //   highest and second highest pieces have zero chance.
    // - The chance of each value is proportional to how many pieces of that value are on the board.
    // - The value 2 always has at least MinimumValueChance, and it stays possible even when it is the
    //   highest or second highest value on the board.
    public static class SpawnTable
    {
        public const int MinimumValue = 2;
        public const double MinimumValueChance = 0.2;

        private const int ProbabilityResolution = 1000000;

        public static IReadOnlyList<SpawnChance> Build(BoardModel board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            // Values at or above the second highest distinct piece are excluded, so the highest and
            // second highest pieces never spawn. With a single distinct value on the board, only that
            // value (the highest) is excluded.
            int exclusiveLimit = board.SecondHighestValue > 0 ? board.SecondHighestValue : board.HighestValue;

            // Candidate values: 2 plus every distinct value on the board below the limit.
            // 2 is always a candidate, even when it is the highest or second highest value.
            var candidates = new List<int> { MinimumValue };
            var weights = new List<int> { board.GetValueCount(MinimumValue) };

            if (exclusiveLimit <= MinimumValue)
            {
                return new[] { new SpawnChance(MinimumValue, 1d) };
            }

            for (int value = MinimumValue + 1; value < exclusiveLimit; value++)
            {
                int count = board.GetValueCount(value);
                if (count > 0)
                {
                    candidates.Add(value);
                    weights.Add(count);
                }
            }

            int total = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                total += weights[i];
            }

            // Only 2 remains as a candidate and nothing of lower value is on the board.
            if (total == 0)
            {
                return new[] { new SpawnChance(MinimumValue, 1d) };
            }

            int twoWeight = weights[0];
            double twoShare = (double)twoWeight / total;
            var chances = new List<SpawnChance>(candidates.Count);

            if (twoShare >= MinimumValueChance)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    chances.Add(new SpawnChance(candidates[i], (double)weights[i] / total));
                }

                return chances;
            }

            // Raise 2 to the minimum and share the remaining chance proportionally among the other values.
            double remaining = 1d - MinimumValueChance;
            int otherTotal = total - twoWeight;

            chances.Add(new SpawnChance(MinimumValue, MinimumValueChance));
            for (int i = 1; i < candidates.Count; i++)
            {
                chances.Add(new SpawnChance(candidates[i], remaining * weights[i] / otherTotal));
            }

            return chances;
        }

        public static int Pick(IReadOnlyList<SpawnChance> chances, IRandomSource random)
        {
            if (chances == null || chances.Count == 0)
            {
                throw new ArgumentException("At least one spawn chance is required.", nameof(chances));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            double roll = random.Next(ProbabilityResolution) / (double)ProbabilityResolution;
            double cumulative = 0d;
            int lastPossible = MinimumValue;

            for (int i = 0; i < chances.Count; i++)
            {
                if (chances[i].Probability <= 0d)
                {
                    continue;
                }

                lastPossible = chances[i].Value;
                cumulative += chances[i].Probability;

                if (roll < cumulative)
                {
                    return chances[i].Value;
                }
            }

            // Protects against floating point rounding leaving the roll slightly above the total.
            return lastPossible;
        }
    }
}
