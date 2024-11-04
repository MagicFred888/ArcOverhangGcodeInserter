using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools;

public static class OverhangTools
{
    public static Region ComputeOverhangRegion(GraphicsPath lowerLayer, GraphicsPath upperLayer)
    {
        // Compute the region of the overhang
        using Region lowerRegion = new(lowerLayer);
        Region upperRegion = new(upperLayer);
        upperRegion.Exclude(lowerRegion);
        return upperRegion;
    }
}