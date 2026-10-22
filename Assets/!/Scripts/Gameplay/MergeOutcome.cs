using System;
using System.Collections.Generic;

namespace NumberMerge.Gameplay
{
    public sealed class MergeOutcome
    {
        private readonly BoardPosition[] _neighbors;

        public MergeOutcome(BoardPosition origin, int value, int resultValue, IReadOnlyList<BoardPosition> neighbors)
        {
            Origin = origin;
            Value = value;
            ResultValue = resultValue;
            _neighbors = new List<BoardPosition>(neighbors).ToArray();
        }

        public BoardPosition Origin { get; }
        public int Value { get; }
        public int ResultValue { get; }
        public IReadOnlyList<BoardPosition> Neighbors => _neighbors;
        public int NeighborCount => _neighbors.Length;
        public int ParticipantCount => _neighbors.Length + 1;
    }
}
