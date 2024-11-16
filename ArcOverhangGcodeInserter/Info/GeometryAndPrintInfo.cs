using ArcOverhangGcodeInserter.Tools;

namespace ArcOverhangGcodeInserter.Info
{
    public class GeometryAndPrintInfo
    {
        public SegmentType Type { get; internal set; }

        public PointF StartPosition { get; set; }

        public PointF EndPosition { get; set; }

        public PointF CenterPosition { get; set; } = PointF.Empty;

        public float Radius { get; set; } = float.NaN;

        public float StartAngle { get; set; } = float.NaN;

        public float EndAngle { get; set; } = float.NaN;

        public float SweepAngle => EndAngle - StartAngle;

        public int CoolingFanSpeedInPercent { get; private set; } = 0;

        public float PrintSpeedInMmPerSecond { get; private set; } = float.NaN;

        public float ExtrusionMultiplier { get; private set; } = float.NaN;

        public GeometryAndPrintInfo(PointF startPosition, PointF endPosition)
        {
            // Constructor for line
            StartPosition = startPosition;
            EndPosition = endPosition;
            Type = SegmentType.Line;
        }

        public GeometryAndPrintInfo(PointF startPosition, PointF endPosition, PointF centerPosition, float radius, ArcDirection arcDirection)
        {
            // Constructor for arc
            StartPosition = startPosition;
            EndPosition = endPosition;
            CenterPosition = centerPosition;
            Radius = radius;
            Type = arcDirection == ArcDirection.Clockwise ? SegmentType.ClockwiseArc : SegmentType.CounterClockwiseArc;
        }

        public GeometryAndPrintInfo(float startAngle, float endAngle, PointF centerPosition, float radius, ArcDirection arcDirection)
        {
            // Compute start and end position
            StartAngle = startAngle;
            EndAngle = endAngle;
            CenterPosition = centerPosition;
            Radius = radius;
            StartPosition = OverhangPathTools.GetPoint(centerPosition, radius, startAngle);
            EndPosition = OverhangPathTools.GetPoint(centerPosition, radius, endAngle);
            Type = arcDirection == ArcDirection.Clockwise ? SegmentType.ClockwiseArc : SegmentType.CounterClockwiseArc;
        }

        public bool InvertDirection()
        {
            if (Type != SegmentType.ClockwiseArc && Type != SegmentType.CounterClockwiseArc)
            {
                return false;
            }

            // Invert direction
            (EndPosition, StartPosition) = (StartPosition, EndPosition);
            (EndAngle, StartAngle) = (StartAngle, EndAngle);
            Type = Type == SegmentType.ClockwiseArc ? SegmentType.CounterClockwiseArc : SegmentType.ClockwiseArc;
            return true;
        }

        public bool SetPrintParameter(int coolingFanSpeedInPercent, float printSpeedInMmPerSecond, float extrusionMultiplier)
        {
            // Simple check
            if (coolingFanSpeedInPercent < 0 || coolingFanSpeedInPercent > 100 || printSpeedInMmPerSecond < 0.1 || extrusionMultiplier < 0)
            {
                return false;
            }
            CoolingFanSpeedInPercent = coolingFanSpeedInPercent;
            PrintSpeedInMmPerSecond = printSpeedInMmPerSecond;
            ExtrusionMultiplier = extrusionMultiplier;
            return true;
        }
    }
}