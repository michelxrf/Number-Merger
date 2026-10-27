namespace NumberMerge.Gameplay
{
    public static class MergeMath
    {
        public const int MaxTileValue = 8192;
        public const int MinParticipants = 2;
        public const int MaxParticipants = 5;

        public static bool CanCombine(int value)
        {
            return value > BoardModel.EmptyValue && value < MaxTileValue;
        }

        public static bool TryCalculateResult(int value, int participantCount, out int resultValue)
        {
            resultValue = BoardModel.EmptyValue;

            if (!CanCombine(value) || participantCount < MinParticipants || participantCount > MaxParticipants)
            {
                return false;
            }

            int calculatedValue = value << (participantCount - 1);
            if (calculatedValue > MaxTileValue)
            {
                return false;
            }

            resultValue = calculatedValue;
            return true;
        }
    }
}
