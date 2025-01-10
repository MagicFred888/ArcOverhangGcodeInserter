namespace ArcOverhangGcodeInserter.Extensions
{
    public static class Constants
    {
        // Main calculation parameters
        public const float InternalCalculationScaleFactor = 10f;

        public const float DisplayScaleFactor = 100f;

        public const float ArcIntersection = 0.11f; // Based on https://fullcontrol.xyz/#/models/b70938

        public const float FilamentDiameter = 1.75f;

        // Fan speed
        public const int MaxFanSpeedInPercent = 100;

        // Overhang speed
        public const float OverhangPrintSpeedInMmPerSecond = 10f;

        public const float OverhangLinkPrintSpeedInMmPerSecond = 6f;

        // Overhang extrusion multiplier
        public const float OverhangExtrusionMultiplier = 1.05f;

        public const float OverhangStartEndExtrusionMultiplier = 0.5f;

        // Overhang start/end length
        public const float OverhangStartEndLength = 1.2f;
    }
}