namespace ArcOverhangGcodeInserter.Extensions
{
    public static class FloatExtensions
    {
        public static float ScaleUp(this float value)
        {
            return value * Constants.InternalCalculationScaleFactor;
        }

        public static float ScaleDown(this float value)
        {
            return value / Constants.InternalCalculationScaleFactor;
        }
    }
}