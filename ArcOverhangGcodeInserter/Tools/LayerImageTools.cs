using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools;

public class LayerImageTools
{
    private readonly RectangleF allLayersBound;
    private const float scaleFactor = 10;
    private readonly Matrix matrix = new();

    public LayerImageTools(List<LayerInfos> allLayerInfos)
    {
        // Compute total bounding box
        allLayersBound = allLayerInfos[0].OuterWallGraphicsPath.GetBounds();
        for (int pos = 1; pos < allLayerInfos.Count; pos++)
        {
            RectangleF tmpBound = allLayerInfos[pos].OuterWallGraphicsPath.GetBounds();
            float minX = Math.Min(allLayersBound.Left, tmpBound.Left);
            float maxX = Math.Max(allLayersBound.Right, tmpBound.Right);
            float minY = Math.Min(allLayersBound.Top, tmpBound.Top);
            float maxY = Math.Max(allLayersBound.Bottom, tmpBound.Bottom);
            allLayersBound = new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        // Compute transformation matrix
        matrix.Scale(scaleFactor, -scaleFactor);  // Scale by 10 on x and -10 on y for flip and scaling
        matrix.Translate(scaleFactor - scaleFactor * allLayersBound.Left, scaleFactor + scaleFactor * allLayersBound.Bottom, MatrixOrder.Append);
    }

    public Image GetImageFromLayerGraphicsPath(LayerInfos layerInfos)
    {
        // Create image
        Bitmap layerImage = new((int)Math.Ceiling(scaleFactor * (2 + allLayersBound.Width)), 10 + (int)Math.Ceiling(scaleFactor * (2 + allLayersBound.Height)));

        // Draw layer
        using Graphics gra = Graphics.FromImage(layerImage);
        gra.Clear(Color.Transparent);
        gra.SmoothingMode = SmoothingMode.HighQuality;
        gra.InterpolationMode = InterpolationMode.HighQualityBicubic;
        Region partRegion = new(CloneScaleAndFlip(layerInfos.OuterWallGraphicsPath));
        gra.FillRegion(new SolidBrush(Color.LightGray), partRegion);

        // Draw all path with color depending if it's overhang or not
        for (int i = 0; i < 2; i++)
        {
            // Define data and color
            List<WallInfo> referenceWalls = new[] { layerInfos.OuterWalls, layerInfos.InnerWalls }[i];
            Color wallColor = new[] { Color.DarkBlue, Color.Blue }[i];
            Color overhangeColor = new[] { Color.DarkRed, Color.Red }[i];

            // Draw overhang region
            if (layerInfos.OverhangRegion != null && layerInfos.OverhangStartRegion != null)
            {
                gra.FillRegion(new SolidBrush(Color.FromArgb(20, overhangeColor)), CloneScaleAndFlip(layerInfos.OverhangRegion));
                gra.FillRegion(new SolidBrush(Color.Black), CloneScaleAndFlip(layerInfos.OverhangStartRegion));
            }

            // Draw each walls
            foreach (WallInfo wall in referenceWalls)
            {
                foreach (GCodeInfo gCode in wall.WallGCodeContent)
                {
                    if (gCode.GraphicsPath == null)
                    {
                        continue;
                    }
                    gra.DrawPath(new Pen(gCode.IsOverhang ? overhangeColor : wallColor, 2), CloneScaleAndFlip(gCode.GraphicsPath));
                }
            }
        }

        // Done
        return layerImage;
    }

    private GraphicsPath CloneScaleAndFlip(GraphicsPath graphicsPath)
    {
        // Clone, scale and flip
        GraphicsPath scaledLayerGraphicsPath = (GraphicsPath)graphicsPath.Clone();
        scaledLayerGraphicsPath.Transform(matrix);
        return scaledLayerGraphicsPath;
    }

    private Region CloneScaleAndFlip(Region region)
    {
        // Clone, scale and flip
        Region scaledRegion = region.Clone();
        scaledRegion.Transform(matrix);
        return scaledRegion;
    }
}