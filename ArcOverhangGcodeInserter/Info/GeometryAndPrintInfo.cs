using ArcOverhangGcodeInserter.Extensions;

namespace ArcOverhangGcodeInserter.Info
{
    public class GeometryAndPrintInfo
    {
        public SegmentType Type { get; internal set; }

        public PointF StartPosition { get; private set; }

        public PointF EndPosition { get; private set; }

        public PointF CenterPosition { get; private set; } = PointF.Empty;

        public float Radius { get; private set; } = float.NaN;

        private float _startAngle;

        public float StartAngle
        {
            get => _startAngle;
            set
            {
                _startAngle = value;
                StartPosition = CenterPosition.GetPoint(Radius, value);
            }
        }

        private float _endAngle;

        public float EndAngle
        {
            get => _endAngle;
            set
            {
                _endAngle = value;
                EndPosition = CenterPosition.GetPoint(Radius, value);
            }
        }

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
            StartPosition = centerPosition.GetPoint(radius, startAngle);
            EndPosition = centerPosition.GetPoint(radius, endAngle);
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

        public float EvalSweepAngle()
        {
            return CenterPosition.Angle(EndPosition) - CenterPosition.Angle(StartPosition);
        }

        public float AngleFromArcLength(float arcLength)
        {
            // Compute the angle in degree that give requested arc length based on the radius
            return (180f * arcLength) / ((float)Math.PI * Radius);
        }
    }
}