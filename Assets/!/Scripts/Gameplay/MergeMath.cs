namespace NumberMerge.Gameplay
{
    public static class MergeMath
    {
        public const int MaxTileValue = 8192;
        public const int MinParticipants = 2;
        public const int MaxParticipants = 5;

        // A tile at or above the ceiling can no longer be combined.
        public static bool CanCombine(int value, int ceilingValue)
        {
            return value > BoardModel.EmptyValue && value < ceilingValue;
        }

        // A merge is valid only when its input tile is below the ceiling and its result does not exceed it.
        public static bool TryCalculateResult(int value, int participantCount, int ceilingValue, out int resultValue)
        {
            resultValue = BoardModel.EmptyValue;

            if (!CanCombine(value, ceilingValue) || participantCount < MinParticipants || participantCount > MaxParticipants)
            {
                return false;
            }

            int calculatedValue = value << (participantCount - 1);
            if (calculatedValue > ceilingValue)
            {
                return false;
            }

            resultValue = calculatedValue;
            return true;
        }
    }
}
