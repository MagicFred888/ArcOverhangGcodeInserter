namespace ArcOverhangGcodeInserter.Info
{
    public class SegmentGeometryInfo
    {
        public SegmentType Type { get; internal set; }

        public PointF StartPosition { get; set; }

        public PointF EndPosition { get; set; }

        public PointF CenterPosition { get; set; } = PointF.Empty;

        public float Radius { get; set; } = float.NaN;

        public float StartAngle { get; set; } = float.NaN;

        public float EndAngle { get; set; } = float.NaN;

        public float SweepAngle => EndAngle - StartAngle;

        public SegmentGeometryInfo(PointF startPosition, PointF endPosition)
        {
            // Constructor for line
            StartPosition = startPosition;
            EndPosition = endPosition;
            Type = SegmentType.Line;
        }

        public SegmentGeometryInfo(PointF startPosition, PointF endPosition, PointF centerPosition, float radius, ArcDirection arcDirection)
        {
            // Constructor for arc
            StartPosition = startPosition;
            EndPosition = endPosition;
            CenterPosition = centerPosition;
            Radius = radius;
            Type = arcDirection == ArcDirection.Clockwise ? SegmentType.ClockwiseArc : SegmentType.CounterClockwiseArc;
        }
    }
}