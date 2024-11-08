using ArcOverhangGcodeInserter.Tools;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Info;

public enum ArcDirection
{
    Unknown,
    Clockwise,
    CounterClockwise
}

public class SegmentInfo
{
    public int OriginalGCodeLineNbr { get; private set; } = -1;

    public PointF StartPoint { get; private set; }

    public PointF EndPoint { get; private set; }

    public PointF CenterPoint { get; private set; } = PointF.Empty;

    public float Radius { get; private set; } = float.NaN;

    public ArcDirection ArcDirection { get; private set; } = ArcDirection.Unknown;

    public string GCodeCommand { get; private set; }

    public bool IsOverhang { get; private set; } = false;

    public GraphicsPath GraphicsPath { get; private set; }

    public SegmentInfo(int originalGCodeLineNbr, PointF startPoint, string gCodeCommand, bool isOverhang)
    {
        // Save parameters
        OriginalGCodeLineNbr = originalGCodeLineNbr;
        StartPoint = startPoint;
        GCodeCommand = gCodeCommand;
        IsOverhang = isOverhang;

        // Compute graphics path
        EndPoint = GCodeTools.GetXYFromGCode(gCodeCommand);
        GraphicsPath = GCodeTools.ConvertGcodeIntoGraphicsPath(startPoint, gCodeCommand);
    }

    public SegmentInfo(PointF startPoint, PointF endPoint, bool isOverhang, GraphicsPath linePath)
    {
        // Save parameters
        StartPoint = startPoint;
        EndPoint = endPoint;
        IsOverhang = isOverhang;
        GraphicsPath = linePath;

        // Compute GCode command
        GCodeCommand = GCodeTools.GetGCodeLine(startPoint, endPoint);
    }

    public SegmentInfo(PointF startPoint, PointF endPoint, PointF centerPoint, float radius, ArcDirection arcDirection, bool isOverhang, GraphicsPath arcPath)
    {
        // Save parameters
        StartPoint = startPoint;
        EndPoint = endPoint;
        CenterPoint = centerPoint;
        Radius = radius;
        ArcDirection = arcDirection;
        IsOverhang = isOverhang;
        GraphicsPath = arcPath;

        // Compute GCode command
        GCodeCommand = GCodeTools.GetGCodeArc(startPoint, endPoint, centerPoint, ArcDirection == ArcDirection.Clockwise);
    }

    public override string ToString()
    {
        return $"{OriginalGCodeLineNbr}: {GCodeCommand} (IsOverhang={IsOverhang})";
    }
}