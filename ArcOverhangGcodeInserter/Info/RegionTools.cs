using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Info
{
    public static class RegionTools
    {
        public static (Region overhangRegion, Region overhangStartRegion) ComputeOverhangRegion(LayerInfo previousLayer, LayerInfo currentLayer)
        {
            // Extract data
            GraphicsPath currentPath = currentLayer.InnerWallGraphicsPath ?? currentLayer.OuterWallGraphicsPath;
            GraphicsPath previousPath = previousLayer.InnerWallGraphicsPath ?? previousLayer.OuterWallGraphicsPath;
            GraphicsPath previousOuterPath = previousLayer.OuterWallGraphicsPath;

            // Substarct previous layer from current layer
            Region overhangRegion = new(currentPath);
            overhangRegion.Exclude(previousPath);

            // Keep common between overhang and previous layer
            Region overhangStartRegion = overhangRegion.Clone();
            overhangStartRegion.Intersect(previousOuterPath);

            // Done
            return (overhangRegion, overhangStartRegion);
        }
    }
}