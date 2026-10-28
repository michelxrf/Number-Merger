using System;
using System.Collections.Generic;

namespace NumberMerge.Gameplay
{
    // Generates the Current and Next pieces from the board state.
    //
    // Pipeline per piece (read-only on the board, one random roll):
    //   1. Eligibility: SpawnTable.Build gives the legal values (including values not yet on the board)
    //      and their base frequency prior.
    //   2. Pressure:    BoardPressureCalculator measures how close the board is to the danger threshold.
    //   3. Scoring:     PieceDifficultyEvaluator scores each legal value with pressure-dependent weights.
    //   4. Selection:   CandidateSelection applies softmax; SpawnTable.Pick draws with the injected random source.
    public sealed class PieceGenerator
    {
        private readonly IRandomSource _random;
        private readonly PieceGenerationSettings _settings;
        private readonly BoardPressureCalculator _pressureCalculator;
        private readonly PieceDifficultyEvaluator _evaluator;

        // Scratch buffers reused every turn to avoid per-piece allocations.
        private readonly List<CandidateScores> _scores = new List<CandidateScores>();
        private readonly List<double> _weights = new List<double>();
        private readonly List<SpawnChance> _chances = new List<SpawnChance>();

        public PieceGenerator()
            : this(new SystemRandomSource(), new PieceGenerationSettings())
        {
        }

        public PieceGenerator(IRandomSource random)
            : this(random, new PieceGenerationSettings())
        {
        }

        public PieceGenerator(IRandomSource random, PieceGenerationSettings settings)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settings.Validate();

            _pressureCalculator = new BoardPressureCalculator(_settings);
            _evaluator = new PieceDifficultyEvaluator(_settings);
        }

        public int Current { get; private set; }
        public int Next { get; private set; }

        // When true, every generation stores its decision in LastDecision for inspection and tuning.
        public bool RecordDecisions { get; set; }

        // Decision behind the most recently generated piece. Null unless RecordDecisions (or LogDecisions) is on.
        public PieceGenerationDecision LastDecision { get; private set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Development-only: logs every decision to the Unity console. Off by default to keep logs quiet.
        public bool LogDecisions { get; set; }
#endif

        // Generates both pieces of the queue from the current board state.
        public void Reset(BoardModel board)
        {
            Current = GenerateValue(board);
            Next = GenerateValue(board);
        }

        // Restores a saved queue instead of generating a new one.
        public void Restore(int current, int next)
        {
            Current = current;
            Next = next;
        }

        // Promotes Next to Current and generates a new Next from the current board state.
        public void Advance(BoardModel board)
        {
            Current = Next;
            Next = GenerateValue(board);
        }

        private int GenerateValue(BoardModel board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            IReadOnlyList<SpawnChance> eligible = SpawnTable.Build(
                board, _settings.AbsentValueWeight, _settings.SpawnCeilingLevelOffset, _settings.SpawnHardCeiling);
            BoardPressure pressure = _pressureCalculator.Calculate(board);
            _evaluator.Evaluate(board, eligible, pressure.Normalized, _scores, out ScoreWeights weights);

            // SpawnTable.Build always contains at least the minimum value, so this is a defensive fallback
            // that uses the original frequency-only behavior instead of an unscored value.
            if (_scores.Count == 0)
            {
                LastDecision = null;
                return SpawnTable.Pick(eligible, _random);
            }

            CandidateSelection.Compute(_scores, _settings.Temperature, _settings.MinimumCandidateProbability, _weights, _chances);
            int value = SpawnTable.Pick(_chances, _random);

            bool record = RecordDecisions;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            record |= LogDecisions;
#endif

            if (record)
            {
                LastDecision = BuildDecision(pressure, weights, value);
            }
            else
            {
                LastDecision = null;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (LogDecisions && LastDecision != null)
            {
                UnityEngine.Debug.Log(LastDecision.ToString());
            }
#endif

            return value;
        }

        private PieceGenerationDecision BuildDecision(BoardPressure pressure, ScoreWeights weights, int selectedValue)
        {
            var candidates = new CandidateDecision[_scores.Count];
            for (int i = 0; i < candidates.Length; i++)
            {
                candidates[i] = new CandidateDecision(_scores[i], _weights[i], _chances[i].Probability);
            }

            return new PieceGenerationDecision(pressure, weights, candidates, selectedValue);
        }
    }
}
