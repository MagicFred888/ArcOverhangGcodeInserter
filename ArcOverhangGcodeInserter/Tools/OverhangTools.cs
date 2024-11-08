using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools;

public class SegmentGeometryInfo
{
    public PointF StartPoint { get; set; } = PointF.Empty;
    public PointF EndPoint { get; set; } = PointF.Empty;
    public PointF CenterPt { get; set; } = PointF.Empty;
    public float Radius { get; set; } = float.NaN;
    public float StartAngle { get; set; } = float.NaN;
    public float EndAngle { get; set; } = float.NaN;
}

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
            if (!float.IsNaN(yMax) && overhangRegion.IsVisible(xCenter, y))
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

    public static List<List<SegmentGeometryInfo>> GetArcsGeometryInfo(Region overhangRegion, PointF center)
    {
        // For result
        List<List<SegmentGeometryInfo>> result = [];

        // Create list of arcs by increasing radius step by step
        float radiusIncreaseStep = 0.4f * 0.89f; // Based on 0.4mm nozzle and https://fullcontrol.xyz/#/models/b70938
        float radius = 0.2f - radiusIncreaseStep;
        float startAngle;
        bool needOneMoreRun;
        do
        {
            // Increase radius, compute angle step to have point on circle moving 0.01mm and reset
            List<SegmentGeometryInfo> currentRadiusArcs = [];
            needOneMoreRun = false;
            radius += radiusIncreaseStep;
            float angleStep = 360f / ((float)Math.PI * 2f * radius / 0.01f);
            float startScanAngle = 0;
            PointF testPos = GetPoint(center, radius, startScanAngle);

            // Search a point out of the overhang area
            while (overhangRegion.IsVisible(testPos) && startScanAngle < 360)
            {
                startScanAngle += angleStep;
                testPos = GetPoint(center, radius, startScanAngle);
            }

            // Full circle ?
            if (startScanAngle > 360)
            {
                currentRadiusArcs.Add(new SegmentGeometryInfo()
                {
                    StartPoint = new PointF(center.X + radius, center.Y),
                    EndPoint = new PointF(center.X + radius, center.Y),
                    CenterPt = center,
                    Radius = radius,
                    StartAngle = 0,
                    EndAngle = 360,
                });
                continue;
            }

            // Perform a 360° scan from current angle to find all arc segment within the overhang area
            startAngle = float.NaN;
            for (float angle = startScanAngle; angle <= startScanAngle + 360f + angleStep; angle += angleStep)
            {
                if (float.IsNaN(startAngle) && overhangRegion.IsVisible(GetPoint(center, radius, angle)))
                {
                    // Start new arc
                    startAngle = angle;
                    continue;
                }
                if (!float.IsNaN(startAngle) && !overhangRegion.IsVisible(GetPoint(center, radius, angle)))
                {
                    // End arc
                    float stopAngle = angle - angleStep;
                    currentRadiusArcs.Add(new SegmentGeometryInfo()
                    {
                        StartPoint = GetPoint(center, radius, startAngle),
                        EndPoint = GetPoint(center, radius, stopAngle),
                        CenterPt = center,
                        Radius = radius,
                        StartAngle = startAngle,
                        EndAngle = stopAngle,
                    });

                    needOneMoreRun = true;
                    startAngle = float.NaN;
                }
            }

            // Full circle
            if (!float.IsNaN(startAngle))
            {
                currentRadiusArcs.Add(new SegmentGeometryInfo()
                {
                    StartPoint = new PointF(center.X + radius, center.Y),
                    EndPoint = new PointF(center.X + radius, center.Y),
                    CenterPt = center,
                    Radius = radius,
                    StartAngle = 0,
                    EndAngle = 360,
                });
                needOneMoreRun = true;
            }

            // Save current radius
            result.Add(currentRadiusArcs);
        } while (needOneMoreRun);

        return result;
    }

    private static PointF GetPoint(PointF center, float radius, float angleDeg)
    {
        float x = center.X + (float)Math.Cos(angleDeg * Math.PI / 180) * radius;
        float y = center.Y + (float)Math.Sin(angleDeg * Math.PI / 180) * radius;
        return new(x, y);
    }

    public static List<PathInfo> GetArcsPathInfo(List<List<SegmentGeometryInfo>> allArcsPerRadius)
    {
        List<PathInfo> result = [];
        foreach (List<SegmentGeometryInfo> infoPerRadius in allArcsPerRadius)
        {
            foreach (SegmentGeometryInfo gi in infoPerRadius)
            {
                GraphicsPath newPath = new();
                if (Math.Abs(gi.StartAngle % 360 - gi.EndAngle % 360) < 0.0001)
                {
                    newPath.AddEllipse(gi.CenterPt.X - gi.Radius, gi.CenterPt.Y - gi.Radius, gi.Radius * 2, gi.Radius * 2);
                }
                else
                {
                    newPath.AddArc(gi.CenterPt.X - gi.Radius, gi.CenterPt.Y - gi.Radius, gi.Radius * 2, gi.Radius * 2, gi.StartAngle, gi.EndAngle - gi.StartAngle);
                }
                SegmentInfo newSegment = new(gi.StartPoint, gi.EndPoint, gi.CenterPt, gi.Radius, ArcDirection.CounterClockwise, true, newPath);
                PathInfo newPathInfo = new();
                newPathInfo.AddSegmentInfo(newSegment);
                result.Add(newPathInfo);
            }
        }
        return result;
    }
}