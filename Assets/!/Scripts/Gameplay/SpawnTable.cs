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

    // Computes the eligible values and their base probability for the next piece.
    //
    // Rules:
    // - Spawned values are at most the ceiling: highest * 2^ceilingLevelOffset, clamped to the merge ceiling
    //   (see CalculateCeiling). The offset and merge ceiling come from PieceGenerationSettings.
    // - Eligible values are every power of two up to that ceiling, including values that are not on the board.
    // - A value's weight is the number of pieces of that value on the board. A power of two that is not on
    //   the board gets the caller-supplied absentValueWeight instead (0 keeps the old board-only behavior).
    // - The value 2 always has at least MinimumValueChance, and it stays possible even when it is the
    //   highest or second highest value on the board.
    public static class SpawnTable
    {
        public const int MinimumValue = 2;
        public const double MinimumValueChance = 0.2;

        private const int ProbabilityResolution = 1000000;

        // Highest spawnable value (inclusive) for a board, given as levels relative to its highest piece:
        // highest * 2^ceilingLevelOffset. Positive offsets go above the highest piece, negative offsets go below it.
        // The result is clamped to mergeCeilingValue and is 0 on an empty board.
        public static int CalculateCeiling(int highestValue, int ceilingLevelOffset, int mergeCeilingValue)
        {
            if (highestValue <= BoardModel.EmptyValue)
            {
                return 0;
            }

            if (ceilingLevelOffset >= 0)
            {
                long shifted = (long)highestValue << Math.Min(ceilingLevelOffset, 31);
                return (int)Math.Min(shifted, mergeCeilingValue);
            }

            return highestValue >> Math.Min(-ceilingLevelOffset, 31);
        }

        // hardCeiling caps the spawned value regardless of the board: no piece above it is generated.
        // 0 means no cap. It only affects spawns; merge results are not limited by it.
        // mergeCeilingValue (PieceGenerationSettings.MergeCeilingValue) is the highest value that can spawn.
        public static IReadOnlyList<SpawnChance> Build(
            BoardModel board,
            double absentValueWeight,
            int ceilingLevelOffset,
            int hardCeiling,
            int mergeCeilingValue)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (double.IsNaN(absentValueWeight) || double.IsInfinity(absentValueWeight) || absentValueWeight < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(absentValueWeight));
            }

            if (hardCeiling < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hardCeiling));
            }

            int ceiling = CalculateCeiling(board.HighestValue, ceilingLevelOffset, mergeCeilingValue);
            if (hardCeiling > 0)
            {
                ceiling = Math.Min(ceiling, hardCeiling);
            }

            if (ceiling <= MinimumValue)
            {
                return new[] { new SpawnChance(MinimumValue, 1d) };
            }

            // Candidates: 2 first, then every power of two below the limit (on the board or not), plus any
            // other distinct value on the board below the limit.
            var candidates = new List<int> { MinimumValue };
            var weights = new List<double> { WeightOf(board, MinimumValue, absentValueWeight) };

            for (int value = MinimumValue + 1; value <= ceiling; value++)
            {
                double weight = WeightOf(board, value, absentValueWeight);
                if (weight > 0d)
                {
                    candidates.Add(value);
                    weights.Add(weight);
                }
            }

            double total = 0d;
            for (int i = 0; i < weights.Count; i++)
            {
                total += weights[i];
            }

            // Nothing else can spawn and the minimum value has no weight (absentValueWeight is 0).
            if (total <= 0d)
            {
                return new[] { new SpawnChance(MinimumValue, 1d) };
            }

            double twoShare = weights[0] / total;
            var chances = new List<SpawnChance>(candidates.Count);

            if (twoShare >= MinimumValueChance)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    chances.Add(new SpawnChance(candidates[i], weights[i] / total));
                }

                return chances;
            }

            // Raise 2 to the minimum and share the remaining chance proportionally among the other values.
            double remaining = 1d - MinimumValueChance;
            double otherTotal = total - weights[0];

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

        private static double WeightOf(BoardModel board, int value, double absentValueWeight)
        {
            int count = board.GetValueCount(value);
            if (count > 0)
            {
                return count;
            }

            return IsPowerOfTwo(value) ? absentValueWeight : 0d;
        }

        private static bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }
    }
}
