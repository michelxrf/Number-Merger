using System;
using UnityEngine;

namespace NumberMerge.Gameplay
{
    // Relative influence of each candidate component on the selection score. Every component is
    // normalized to [0, 1] where a higher value means the candidate is more likely to be selected.
    // Calculation type only; the serialized form is ScoreWeightSettings.
    public readonly struct ScoreWeights
    {
        public ScoreWeights(double frequency, double mergeOpportunity, double congestion, double progression)
        {
            Frequency = frequency;
            MergeOpportunity = mergeOpportunity;
            Congestion = congestion;
            Progression = progression;
        }

        public double Frequency { get; }
        public double MergeOpportunity { get; }
        public double Congestion { get; }
        public double Progression { get; }

        public static ScoreWeights Lerp(ScoreWeights from, ScoreWeights to, double t)
        {
            return new ScoreWeights(
                from.Frequency + (to.Frequency - from.Frequency) * t,
                from.MergeOpportunity + (to.MergeOpportunity - from.MergeOpportunity) * t,
                from.Congestion + (to.Congestion - from.Congestion) * t,
                from.Progression + (to.Progression - from.Progression) * t);
        }

        public ScoreWeights Normalized()
        {
            double sum = Frequency + MergeOpportunity + Congestion + Progression;
            if (!(sum > 0d) || ScoreMath.IsNotFinite(sum))
            {
                throw new InvalidOperationException("Score weights must have a positive finite sum.");
            }

            return new ScoreWeights(
                Frequency / sum,
                MergeOpportunity / sum,
                Congestion / sum,
                Progression / sum);
        }
    }

    // Serialized set of score weights for one pressure anchor. Values are normalized when used,
    // so they do not need to sum to 1 in the inspector.
    [Serializable]
    public sealed class ScoreWeightSettings
    {
        [Tooltip("Share of the base spawn probability (SpawnTable). Keeps the legacy frequency behavior.")]
        [Range(0f, 1f)] public float Frequency = 0.45f;

        [Tooltip("Flexibility: candidates with more merge options in more columns. Higher favors easier pieces.")]
        [Range(0f, 1f)] public float MergeOpportunity = 0.40f;

        [Tooltip("Placement risk: candidates with no useful placement. Higher makes awkward pieces more likely.")]
        [Range(0f, 1f)] public float Congestion = 0.15f;

        [Tooltip("Ladder position: larger eligible values score higher. Note this tilts toward larger values.")]
        [Range(0f, 1f)] public float Progression = 0f;

        public ScoreWeightSettings()
        {
        }

        public ScoreWeightSettings(float frequency, float mergeOpportunity, float congestion, float progression)
        {
            Frequency = frequency;
            MergeOpportunity = mergeOpportunity;
            Congestion = congestion;
            Progression = progression;
        }

        public ScoreWeights ToWeights()
        {
            return new ScoreWeights(Frequency, MergeOpportunity, Congestion, Progression);
        }
    }

    // Every tuning parameter of the piece generator. This is plain serializable data: the
    // PieceGenerationConfig ScriptableObject holds one instance so designers can edit it in the inspector.
    // Tests and tools can also create instances directly.
    [Serializable]
    public sealed class PieceGenerationSettings
    {
        [Header("Board pressure")]
        [Tooltip("Weight of the average normalized column height (H).")]
        [Range(0f, 1f)] public float HeightPressureWeight = 0.50f;

        [Tooltip("Weight of the occupied visible cell ratio (D). The overflow row is not counted.")]
        [Range(0f, 1f)] public float DensityPressureWeight = 0.30f;

        [Tooltip("Weight of the height difference between the tallest and shortest column (I).")]
        [Range(0f, 1f)] public float ImbalancePressureWeight = 0.20f;

        [Header("Score weights by pressure")]
        [Tooltip("Weights at zero pressure. Favor frequency and flexible merges so the opening stays readable. " +
                 "Progression is 0 so calm boards keep the rule 'higher values are less likely'.")]
        public ScoreWeightSettings LowPressureWeights = new ScoreWeightSettings(0.45f, 0.40f, 0.15f, 0f);

        [Tooltip("Weights at the moderate anchor. Less frequency, more congestion, progression starts.")]
        public ScoreWeightSettings ModeratePressureWeights = new ScoreWeightSettings(0.25f, 0.35f, 0.30f, 0.10f);

        [Tooltip("Weights at full normalized pressure. Congestion dominates, flexible pieces stay possible for recovery.")]
        public ScoreWeightSettings HighPressureWeights = new ScoreWeightSettings(0.10f, 0.20f, 0.50f, 0.20f);

        [Tooltip("Normalized pressure (0..1) where the moderate anchor applies. Low to moderate and moderate to high are interpolated linearly.")]
        [Range(0.05f, 0.95f)] public float ModeratePressurePoint = 0.5f;

        [Header("Selection")]
        [Tooltip("Softmax temperature. Lower values make choices sharper, higher values flatten them toward uniform.")]
        [Range(0.05f, 3f)] public float Temperature = 0.8f;

        [Tooltip("Uniform probability mixed into every eligible value. Keeps all legal values selectable. " +
                 "Capped at 1 / number of candidates.")]
        [Range(0f, 0.2f)] public float MinimumCandidateProbability = 0.01f;

        [Tooltip("Weight given to each power of two below the spawn limit that is not on the board, in units of one " +
                 "piece. 0 = only values already on the board can spawn (old behavior). 1 = as likely as one piece " +
                 "of that value on the board.")]
        [Range(0f, 5f)] public float AbsentValueWeight = 0.5f;

        [Header("Spawn ceiling")]
        [Tooltip("Highest value that can spawn, in levels relative to the highest piece on the board: " +
                 "highest * 2^level. -1 = half the highest piece, 0 = the highest piece itself, +1 = twice it. " +
                 "The result is capped at the max tile value (8192). Negative values can reduce the eligible " +
                 "values to 2 only.")]
        [Range(-MaxSpawnCeilingLevelOffset, MaxSpawnCeilingLevelOffset)]
        public int SpawnCeilingLevelOffset = -1;

        public const int MaxSpawnCeilingLevelOffset = 10;

        [Tooltip("Hard cap on generated pieces: no piece above this value is spawned. 0 = no cap. " +
                 "Must be a power of two from 2 to 8192. Merge results are not limited by this value.")]
        [Range(0, MergeMath.MaxTileValue)] public int SpawnHardCeiling = 0;

        [Header("Merges and congestion")]
        [Tooltip("Merge value by participant count: element 0 = 2 pieces, 1 = 3, 2 = 4, 3 = 5. Must have 4 entries.")]
        public float[] MergeParticipantValues = { 0.6f, 0.8f, 0.9f, 1.0f };

        [Tooltip("How much less a merge is worth near the bottom of a column. 0 = no height effect, 1 = bottom merges worth nothing.")]
        [Range(0f, 1f)] public float MergeHeightBias = 0.5f;

        [Tooltip("How much a useful merge reduces the placement risk of its column. 0 = none, 1 = cancels the risk.")]
        [Range(0f, 1f)] public float MergeReliefFactor = 0.6f;

        [Tooltip("Congestion = weight * least risky column + (1 - weight) * average column risk.")]
        [Range(0f, 1f)] public float CongestionBestColumnWeight = 0.6f;

        public ScoreWeights GetWeights(double normalizedPressure)
        {
            double t = ScoreMath.Clamp01(normalizedPressure);
            double moderatePoint = ModeratePressurePoint;
            ScoreWeights low = LowPressureWeights.ToWeights();
            ScoreWeights moderate = ModeratePressureWeights.ToWeights();
            ScoreWeights high = HighPressureWeights.ToWeights();

            if (t <= moderatePoint)
            {
                double segment = moderatePoint > 0d ? t / moderatePoint : 1d;
                return ScoreWeights.Lerp(low, moderate, segment).Normalized();
            }

            double remaining = 1d - moderatePoint;
            double highSegment = remaining > 0d ? (t - moderatePoint) / remaining : 1d;
            return ScoreWeights.Lerp(moderate, high, highSegment).Normalized();
        }

        public double GetMergeValue(int participantCount)
        {
            int index = participantCount - 2;
            if (index < 0 || index >= MergeParticipantValues.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(participantCount));
            }

            return MergeParticipantValues[index];
        }

        // Throws InvalidOperationException with a message naming the offending parameter.
        public void Validate()
        {
            if (!(Temperature > 0f) || ScoreMath.IsNotFinite(Temperature))
            {
                throw new InvalidOperationException("Temperature must be a positive finite number.");
            }

            if (MinimumCandidateProbability < 0f || MinimumCandidateProbability > 1f)
            {
                throw new InvalidOperationException("MinimumCandidateProbability must be within [0, 1].");
            }

            if (!(ModeratePressurePoint > 0f && ModeratePressurePoint < 1f))
            {
                throw new InvalidOperationException("ModeratePressurePoint must be within (0, 1).");
            }

            if (MergeParticipantValues == null || MergeParticipantValues.Length != MergeMath.MaxParticipants - 1)
            {
                throw new InvalidOperationException("MergeParticipantValues must have one entry per merge size.");
            }

            if (HeightPressureWeight < 0f || DensityPressureWeight < 0f || ImbalancePressureWeight < 0f ||
                HeightPressureWeight + DensityPressureWeight + ImbalancePressureWeight <= 0f)
            {
                throw new InvalidOperationException("Pressure weights must be non-negative and not all zero.");
            }

            ValidateAnchor(LowPressureWeights, nameof(LowPressureWeights));
            ValidateAnchor(ModeratePressureWeights, nameof(ModeratePressureWeights));
            ValidateAnchor(HighPressureWeights, nameof(HighPressureWeights));

            if (ScoreMath.IsNotFinite(AbsentValueWeight) || AbsentValueWeight < 0f)
            {
                throw new InvalidOperationException("AbsentValueWeight must be a finite, non-negative number.");
            }

            if (SpawnCeilingLevelOffset < -MaxSpawnCeilingLevelOffset || SpawnCeilingLevelOffset > MaxSpawnCeilingLevelOffset)
            {
                throw new InvalidOperationException(
                    "SpawnCeilingLevelOffset must be within [-" + MaxSpawnCeilingLevelOffset + ", " +
                    MaxSpawnCeilingLevelOffset + "].");
            }

            if (SpawnHardCeiling != 0 &&
                (SpawnHardCeiling < SpawnTable.MinimumValue || SpawnHardCeiling > MergeMath.MaxTileValue ||
                 (SpawnHardCeiling & (SpawnHardCeiling - 1)) != 0))
            {
                throw new InvalidOperationException(
                    "SpawnHardCeiling must be 0 (no cap) or a power of two from 2 to " + MergeMath.MaxTileValue + ".");
            }

            if (ScoreMath.IsNotFinite(MergeHeightBias) || ScoreMath.IsNotFinite(MergeReliefFactor) ||
                ScoreMath.IsNotFinite(CongestionBestColumnWeight))
            {
                throw new InvalidOperationException("Merge and congestion factors must be finite numbers.");
            }

            for (int i = 0; i < MergeParticipantValues.Length; i++)
            {
                if (ScoreMath.IsNotFinite(MergeParticipantValues[i]) || MergeParticipantValues[i] < 0f)
                {
                    throw new InvalidOperationException("MergeParticipantValues must be finite and non-negative.");
                }
            }
        }

        private static void ValidateAnchor(ScoreWeightSettings weights, string name)
        {
            if (weights == null)
            {
                throw new InvalidOperationException(name + " must not be null.");
            }

            if (weights.Frequency < 0f || weights.MergeOpportunity < 0f ||
                weights.Congestion < 0f || weights.Progression < 0f)
            {
                throw new InvalidOperationException(name + " must have non-negative weights.");
            }

            // Normalized() throws when the sum is zero or not finite.
            weights.ToWeights().Normalized();
        }
    }

    internal static class ScoreMath
    {
        public static double Clamp01(double value)
        {
            if (double.IsNaN(value))
            {
                return 0d;
            }

            return value < 0d ? 0d : value > 1d ? 1d : value;
        }

        public static bool IsNotFinite(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value);
        }
    }
}
