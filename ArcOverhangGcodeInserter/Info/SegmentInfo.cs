using ArcOverhangGcodeInserter.Tools;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Info;

public class SegmentInfo
{
    public int OriginalGCodeLineNbr { get; private set; } = -1;

    public string GCodeCommand { get; private set; } = string.Empty;

    public SegmentType SegmentType { get; private set; } = SegmentType.Unknown;

    public bool IsOverhang { get; private set; } = false;

    public GeometryAndPrintInfo SegmentGeometryInfo { get; private set; }

    public GraphicsPath GraphicsPath { get; private set; }

    public SegmentInfo(int originalGCodeLineNbr, PointF startPosition, string gCodeCommand, bool isOverhang)
    {
        // For this constructor gCodeCommand is not an option
        if (string.IsNullOrEmpty(gCodeCommand))
        {
            throw new ArgumentException("gCodeCommand cannot be null or empty");
        }

        // Save parameters
        OriginalGCodeLineNbr = originalGCodeLineNbr;
        GCodeCommand = gCodeCommand;
        IsOverhang = isOverhang;

        // Compute graphics path
        SegmentGeometryInfo = GCodeTools.GetSegmentGeometryInfoFromGCode(startPosition, gCodeCommand);
        SegmentType = SegmentGeometryInfo.Type;
        GraphicsPath = GCodeTools.GetGraphicsPathFromSegmentGeometryInfo(SegmentGeometryInfo);
    }

    public SegmentInfo(GeometryAndPrintInfo segmentGeometryInfo, bool isOverhang)
    {
        // Save parameters
        SegmentGeometryInfo = segmentGeometryInfo;
        IsOverhang = isOverhang;

        // Compute graphics path
        GraphicsPath = GCodeTools.GetGraphicsPathFromSegmentGeometryInfo(SegmentGeometryInfo);
    }

    public override string ToString()
    {
        return $"{OriginalGCodeLineNbr} -> GCodeCommand: {GCodeCommand} (IsOverhang={IsOverhang})";
    }
}