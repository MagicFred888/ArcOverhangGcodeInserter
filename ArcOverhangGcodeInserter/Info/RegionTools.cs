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

            // Substarct previous layer from current layer
            Region overhangRegion = new(currentPath);
            overhangRegion.Exclude(previousPath);
            Region overhangStartRegion = overhangRegion.Clone();
            overhangStartRegion.Intersect(previousLayer.OuterWallGraphicsPath);

            // Done
            return (overhangRegion, overhangStartRegion);
        }
    }
}