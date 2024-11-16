using ArcOverhangGcodeInserter.Extensions;
using ArcOverhangGcodeInserter.Info;
using System.Collections.Immutable;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools;

public class LayerImageTools
{
    private readonly float _scaleFactor;
    private readonly RectangleF _allLayersBound;
    private readonly Matrix _matrix = new();

    public LayerImageTools(ImmutableList<LayerInfo> allLayerInfo)
    {
        // Compute total bounding box
        _allLayersBound = allLayerInfo[0].OuterWallGraphicsPaths.GetBounds();
        for (int pos = 1; pos < allLayerInfo.Count; pos++)
        {
            RectangleF tmpBound = allLayerInfo[pos].OuterWallGraphicsPaths.GetBounds();
            float minX = Math.Min(_allLayersBound.Left, tmpBound.Left);
            float maxX = Math.Max(_allLayersBound.Right, tmpBound.Right);
            float minY = Math.Min(_allLayersBound.Top, tmpBound.Top);
            float maxY = Math.Max(_allLayersBound.Bottom, tmpBound.Bottom);
            _allLayersBound = new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        // Compute display matrix
        _scaleFactor = Constants.DisplayScaleFactor / Constants.InternalCalculationScaleFactor;
        _matrix.Scale(_scaleFactor, -_scaleFactor);  // Scale by requested amount and "minus" on y for vertical flip
        _matrix.Translate(_scaleFactor - _scaleFactor * _allLayersBound.Left, _scaleFactor + _scaleFactor * _allLayersBound.Bottom, MatrixOrder.Append);
    }

    public Image GetImageFromLayerGraphicsPath(LayerInfo LayerInfo)
    {
        // Pen size
        int penSize = 8;

        // Create image
        Bitmap layerImage = new((int)Math.Ceiling(_scaleFactor * (2 + _allLayersBound.Width)), 10 + (int)Math.Ceiling(_scaleFactor * (2 + _allLayersBound.Height)));

        // Draw layer
        using Graphics gra = Graphics.FromImage(layerImage);
        gra.Clear(Color.Transparent);
        gra.SmoothingMode = SmoothingMode.HighQuality;
        gra.InterpolationMode = InterpolationMode.HighQualityBicubic;
        Region partRegion = new(CloneScaleAndFlip(LayerInfo.OuterWallGraphicsPaths));
        gra.FillRegion(new SolidBrush(Color.LightGray), partRegion);

        // Draw all path with color depending if it's overhang or not
        for (int i = 0; i < 2; i++)
        {
            // Define data and color
            List<PathInfo> referenceWalls = new[] { LayerInfo.OuterWalls, LayerInfo.InnerWalls }[i];
            Color wallColor = new[] { Color.DarkBlue, Color.Blue }[i];
            Color overhangeColor = new[] { Color.DarkRed, Color.Red }[i];

            // Draw overhang region
            foreach (Region overhang in LayerInfo.OverhangRegions.Select(x => x.overhang))
            {
                gra.FillRegion(new SolidBrush(Color.FromArgb(100, overhangeColor)), CloneScaleAndFlip(overhang));
            }

            // Draw overhang start region
            foreach (Region startOverhang in LayerInfo.OverhangRegions.Select(x => x.startOverhang))
            {
                gra.FillRegion(new SolidBrush(Color.Yellow), CloneScaleAndFlip(startOverhang));
            }

            // Draw each walls
            foreach (PathInfo wall in referenceWalls)
            {
                foreach (SegmentInfo gCode in wall.AllSegments.Where(g => g.GraphicsPath != null))
                {
                    gra.DrawPath(new Pen(gCode.IsOverhang ? overhangeColor : wallColor, penSize), CloneScaleAndFlip(gCode.GraphicsPath!));
                }
            }
        }

        // Draw new path
        foreach (PathInfo path in LayerInfo.OverhangInfillAndWallsPaths)
        {
            foreach (GraphicsPath? graphicsPath in path.AllSegments.Where(g => g.GraphicsPath != null).Select(g => g.GraphicsPath))
            {
                gra.DrawPath(new Pen(Color.Black, penSize), CloneScaleAndFlip(graphicsPath!));
            }
        }
        foreach (PathInfo path in LayerInfo.NewOverhangArcsWalls)
        {
            foreach (GraphicsPath? graphicsPath in path.AllSegments.Where(g => g.GraphicsPath != null).Select(g => g.GraphicsPath))
            {
                gra.DrawPath(new Pen(Color.Cyan, penSize), CloneScaleAndFlip(graphicsPath!));
            }
        }

        // Done
        return layerImage;
    }

    private GraphicsPath CloneScaleAndFlip(GraphicsPath graphicsPath)
    {
        // Clone, scale and flip
        GraphicsPath scaledLayerGraphicsPath = (GraphicsPath)graphicsPath.Clone();
        scaledLayerGraphicsPath.Transform(_matrix);
        return scaledLayerGraphicsPath;
    }

    private Region CloneScaleAndFlip(Region region)
    {
        // Clone, scale and flip
        Region scaledRegion = region.Clone();
        scaledRegion.Transform(_matrix);
        return scaledRegion;
    }
}