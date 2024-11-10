namespace ArcOverhangGcodeInserter
{
    public enum SegmentType
    {
        Unknown,
        Line,
        ClockwiseArc,
        CounterClockwiseArc,
    }

    public enum ArcDirection
    {
        Clockwise,
        CounterClockwise
    }
}