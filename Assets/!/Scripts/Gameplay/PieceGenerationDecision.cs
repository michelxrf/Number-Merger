using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NumberMerge.Gameplay
{
    // One candidate as seen by a generation: component scores, selection weight and final probability.
    public readonly struct CandidateDecision
    {
        public CandidateDecision(CandidateScores scores, double weight, double probability)
        {
            Scores = scores;
            Weight = weight;
            Probability = probability;
        }

        public CandidateScores Scores { get; }
        public double Weight { get; }
        public double Probability { get; }
    }

    // Snapshot of why a value was generated. Built only when PieceGenerator.RecordDecisions is enabled,
    // so normal play does not allocate it.
    public sealed class PieceGenerationDecision
    {
        public PieceGenerationDecision(
            BoardPressure pressure,
            ScoreWeights weights,
            IReadOnlyList<CandidateDecision> candidates,
            int selectedValue)
        {
            Pressure = pressure;
            Weights = weights;
            Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
            SelectedValue = selectedValue;
        }

        public BoardPressure Pressure { get; }
        public ScoreWeights Weights { get; }
        public IReadOnlyList<CandidateDecision> Candidates { get; }
        public int SelectedValue { get; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            CultureInfo culture = CultureInfo.InvariantCulture;

            builder.AppendLine(string.Format(culture,
                "[PieceGenerator] pressure raw={0:F3} normalized={1:F3} (height={2:F3} density={3:F3} imbalance={4:F3})",
                Pressure.Raw, Pressure.Normalized, Pressure.Height, Pressure.Density, Pressure.Imbalance));

            builder.AppendLine(string.Format(culture,
                "weights freq={0:F3} merge={1:F3} congestion={2:F3} progression={3:F3}",
                Weights.Frequency, Weights.MergeOpportunity, Weights.Congestion, Weights.Progression));

            for (int i = 0; i < Candidates.Count; i++)
            {
                CandidateDecision candidate = Candidates[i];
                CandidateScores s = candidate.Scores;
                builder.AppendLine(string.Format(culture,
                    "  value={0,5} freq={1:F3} merge={2:F3} congestion={3:F3} progression={4:F3} score={5:F3} weight={6:F4} p={7:P1}",
                    s.Value, s.Frequency, s.MergeOpportunity, s.Congestion, s.Progression, s.Score,
                    candidate.Weight, candidate.Probability));
            }

            builder.Append("selected=").Append(SelectedValue);
            return builder.ToString();
        }
    }
}
