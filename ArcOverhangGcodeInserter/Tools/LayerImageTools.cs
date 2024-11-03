using System.Drawing.Drawing2D;
using ArcOverhangGcodeInserter.Info;

namespace ArcOverhangGcodeInserter.Tools;

public class LayerImageTools
{
    private readonly RectangleF allLayersBound;
    private const float scaleFactor = 10;

    public LayerImageTools(List<LayerInfos> allLayerInfos)
    {
        // Compute total bounding box
        allLayersBound = allLayerInfos[0].LayerGraphicsPath.GetBounds();
        for (int pos = 1; pos < allLayerInfos.Count; pos++)
        {
            RectangleF tmpBound = allLayerInfos[pos].LayerGraphicsPath.GetBounds();
            float minX = Math.Min(allLayersBound.Left, tmpBound.Left);
            float maxX = Math.Max(allLayersBound.Right, tmpBound.Right);
            float minY = Math.Min(allLayersBound.Top, tmpBound.Top);
            float maxY = Math.Max(allLayersBound.Bottom, tmpBound.Bottom);
            allLayersBound = new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }
    }

    public Image GetImageFromLayerGraphicsPath(GraphicsPath layerGraphicsPath)
    {
        // Prepare transformation matrix
        Matrix matrix = new();
        matrix.Scale(scaleFactor, -scaleFactor);  // Scale by 10 on x and -10 on y for flip and scaling
        matrix.Translate(scaleFactor - scaleFactor * allLayersBound.Left, scaleFactor + scaleFactor * allLayersBound.Bottom, MatrixOrder.Append);

        // Scale and move layer GraphicsPath
        GraphicsPath scaledLayerGraphicsPath = (GraphicsPath)layerGraphicsPath.Clone();
        scaledLayerGraphicsPath.Transform(matrix);

        // Create image
        Bitmap layerImage = new((int)Math.Ceiling(scaleFactor * (2 + allLayersBound.Width)), 10 + (int)Math.Ceiling(scaleFactor * (2 + allLayersBound.Height)));

        // Draw layer
        using Graphics gra = Graphics.FromImage(layerImage);
        gra.Clear(Color.Transparent);
        gra.SmoothingMode = SmoothingMode.HighQuality;
        gra.InterpolationMode = InterpolationMode.HighQualityBicubic;
        Region partRegion = new(scaledLayerGraphicsPath);
        gra.FillRegion(new SolidBrush(Color.LightGray), partRegion);
        gra.DrawPath(new Pen(Color.Black, 2), scaledLayerGraphicsPath);

        // Done
        return layerImage;
    }
}