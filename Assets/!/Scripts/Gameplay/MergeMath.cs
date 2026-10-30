namespace NumberMerge.Gameplay
{
    public static class MergeMath
    {
        public const int MinParticipants = 2;
        public const int MaxParticipants = 5;

        // Largest merge ceiling accepted by settings validation: the biggest power of two that fits in an int.
        // This is a validation bound only. The gameplay limit is always the configured merge ceiling.
        public const int MaxSupportedCeiling = 1 << 30;

        // True when the value can be used as a merge ceiling: a power of two from 2 to MaxSupportedCeiling.
        public static bool IsValidCeiling(int value)
        {
            return value >= 2 && value <= MaxSupportedCeiling && (value & (value - 1)) == 0;
        }

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

            // Long arithmetic so the shift cannot overflow before the ceiling check.
            long calculatedValue = (long)value << (participantCount - 1);
            if (calculatedValue > ceilingValue)
            {
                return false;
            }

            resultValue = (int)calculatedValue;
            return true;
        }
    }
}
