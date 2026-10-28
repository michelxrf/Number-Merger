using System;
using System.Collections.Generic;

namespace NumberMerge.Gameplay
{
    // Converts candidate scores into spawn probabilities.
    //
    //   weight(v)      = exp((score(v) - maxScore) / temperature)
    //   probability(v) = (1 - n * floor) * weight(v) / sum(weights) + floor
    //
    // This is softmax with the sign of a conventional "favor the best" softmax kept as-is: higher
    // scores get higher weight, so higher difficulty is more likely. Subtracting maxScore keeps every
    // exponent at or below zero, so the sum is at least 1 and nothing overflows. The floor is mixed in
    // uniformly, so probabilities still sum to 1 and every candidate keeps a nonzero probability.
    public static class CandidateSelection
    {
        // Fills weights with the unnormalized softmax weight of each score and chances with the final
        // probabilities. Both lists keep the order of scores.
        public static void Compute(
            IReadOnlyList<CandidateScores> scores,
            double temperature,
            double minimumProbability,
            List<double> weights,
            List<SpawnChance> chances)
        {
            if (scores == null)
            {
                throw new ArgumentNullException(nameof(scores));
            }

            if (weights == null)
            {
                throw new ArgumentNullException(nameof(weights));
            }

            if (chances == null)
            {
                throw new ArgumentNullException(nameof(chances));
            }

            if (!(temperature > 0d) || ScoreMath.IsNotFinite(temperature))
            {
                throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be a positive finite number.");
            }

            if (minimumProbability < 0d || minimumProbability > 1d || ScoreMath.IsNotFinite(minimumProbability))
            {
                throw new ArgumentOutOfRangeException(nameof(minimumProbability));
            }

            weights.Clear();
            chances.Clear();

            if (scores.Count == 0)
            {
                return;
            }

            double maxScore = double.NegativeInfinity;
            for (int i = 0; i < scores.Count; i++)
            {
                double score = scores[i].Score;
                if (ScoreMath.IsNotFinite(score))
                {
                    throw new InvalidOperationException("Candidate score must be finite. Value: " + scores[i].Value);
                }

                maxScore = Math.Max(maxScore, score);
            }

            double total = 0d;
            for (int i = 0; i < scores.Count; i++)
            {
                double weight = Math.Exp((scores[i].Score - maxScore) / temperature);
                weights.Add(weight);
                total += weight;
            }

            // The candidate with maxScore contributes exactly 1, so total >= 1 and the division is safe.
            int count = scores.Count;
            double floor = Math.Min(minimumProbability, 1d / count);
            double share = 1d - count * floor;

            for (int i = 0; i < count; i++)
            {
                double probability = share * (weights[i] / total) + floor;
                chances.Add(new SpawnChance(scores[i].Value, probability));
            }
        }
    }
}
