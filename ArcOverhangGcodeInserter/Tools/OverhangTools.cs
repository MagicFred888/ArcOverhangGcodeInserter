using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools;

public static class OverhangTools
{
    public static PointF GetArcsCenter(Region? overhangRegion, Region? overhangStartRegion)
    {
        if (overhangRegion == null || overhangStartRegion == null)
        {
            return new PointF();
        }

        // Extract all points from start region
        float scaleFactor = 100f; // to get 100th of a millimeter
        Matrix scaleMatrix = new();
        scaleMatrix.Scale(scaleFactor, scaleFactor);
        Region scaledOverhangStartRegion = overhangStartRegion.Clone();
        scaledOverhangStartRegion.Transform(scaleMatrix);
        List<PointF> startPoints = [];
        foreach (RectangleF rect in scaledOverhangStartRegion.GetRegionScans(new Matrix()))
        {
            // extract each point
            startPoints.Add(new PointF(rect.Left / scaleFactor, rect.Top / scaleFactor));
            startPoints.Add(new PointF(rect.Left / scaleFactor, rect.Bottom / scaleFactor));
            startPoints.Add(new PointF(rect.Right / scaleFactor, rect.Top / scaleFactor));
            startPoints.Add(new PointF(rect.Right / scaleFactor, rect.Bottom / scaleFactor));
        }

        // Compute center of the arc x
        float xCenter = (startPoints.Min(pt => pt.X) + startPoints.Max(pt => pt.X)) / 2;

        // Compute center of the arc y
        bool centerFound = false;
        float yMin = float.NaN;
        float yMax = float.NaN;
        for (float y = startPoints.Min(pt => pt.Y); y < startPoints.Max(pt => pt.Y); y += 1f / scaleFactor)
        {
            // Check if point xCenter, y is inside the overhang region
            if (float.IsNaN(yMin) && overhangRegion.IsVisible(xCenter, y))
            {
                // Compute the arc
                yMin = y;
                yMax = y;
            }
            else if (float.IsNaN(yMax) && overhangRegion.IsVisible(xCenter, y))
            {
                yMax = y;
                centerFound = true;
            }
        }

        // Check if found
        if (!centerFound || float.IsNaN(yMin) || float.IsNaN(yMax))
        {
            return new();
        }

        // Done
        return new(xCenter, (yMin + yMax) / 2f);
    }

    public static List<GraphicsPath> GetArcs(Region overhangRegion, PointF center)
    {
        throw new NotImplementedException();
    }
}