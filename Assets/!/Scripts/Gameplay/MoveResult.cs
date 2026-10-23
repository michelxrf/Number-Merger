using System;

namespace NumberMerge.Gameplay
{
    public sealed class MoveResult
    {
        public MoveResult(
            BoardSnapshot before,
            BoardSnapshot afterInsertion,
            int column,
            int value,
            int insertedRow,
            CascadeResult cascade)
        {
            Before = before ?? throw new ArgumentNullException(nameof(before));
            AfterInsertion = afterInsertion ?? throw new ArgumentNullException(nameof(afterInsertion));
            Column = column;
            Value = value;
            InsertedRow = insertedRow;
            Cascade = cascade ?? throw new ArgumentNullException(nameof(cascade));
        }

        public BoardSnapshot Before { get; }
        public BoardSnapshot AfterInsertion { get; }
        public int Column { get; }
        public int Value { get; }
        public int InsertedRow { get; }
        public CascadeResult Cascade { get; }
        public BoardSnapshot FinalBoard => Cascade.FinalBoard;
    }
}
