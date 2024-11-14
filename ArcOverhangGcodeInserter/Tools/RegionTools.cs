using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools;

public static class RegionTools
{
    public static (Region overhangRegion, Region overhangStartRegion) ComputeOverhangRegion(LayerInfo previousLayer, LayerInfo currentLayer)
    {
        // Create a virtual inner region for CURRENT layer based on outer wall
        GraphicsPath currentInnerPath = (GraphicsPath)currentLayer.OuterWallGraphicsPath.Clone();
        Region currentInnerRegion = new(currentInnerPath);
        currentInnerPath.Widen(new(Color.Black, 30f));
        currentInnerRegion.Exclude(currentInnerPath);

        // Create a virtual inner region for PREVIOUS based on outer wall
        GraphicsPath previousInnerPath = (GraphicsPath)previousLayer.OuterWallGraphicsPath.Clone();
        Region previousInnerRegion = new(previousInnerPath);
        previousInnerPath.Widen(new(Color.Black, 30f));
        previousInnerRegion.Exclude(previousInnerPath);

        GraphicsPath previousOuterPath = (GraphicsPath)previousLayer.OuterWallGraphicsPath.Clone();
        Region previousOuterRegion = new(previousOuterPath);

        // Substarct previous layer from current layer
        Region overhangRegion = currentInnerRegion.Clone();
        overhangRegion.Exclude(previousInnerRegion);

        // Keep common between overhang and previous layer
        Region overhangStartRegion = overhangRegion.Clone();
        overhangStartRegion.Intersect(previousOuterRegion);

        // Done
        return (overhangRegion, overhangStartRegion);
    }
}