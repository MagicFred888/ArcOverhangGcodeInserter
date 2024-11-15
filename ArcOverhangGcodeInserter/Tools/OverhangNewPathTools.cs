using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools;

public static class OverhangNewPathTools
{
    public static List<PathInfo> ComputeNewOverhangArcsWalls(List<(Region overhang, Region startOverhang)> overhangRegions)
    {
        throw new NotImplementedException();
    }

    public static PointF GetArcsCenter(Region? overhangRegion, Region? overhangStartRegion)
    {
        if (overhangRegion == null || overhangStartRegion == null)
        {
            return PointF.Empty;
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

        if (startPoints.Count == 0)
        {
            return PointF.Empty;
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
            if (float.IsNaN(yMin) && overhangStartRegion.IsVisible(xCenter, y))
            {
                // Compute the arc
                yMin = y;
                yMax = y;
            }
            if (!float.IsNaN(yMax) && overhangStartRegion.IsVisible(xCenter, y))
            {
                yMax = y;
                centerFound = true;
            }
        }

        // Check if not found
        if (!centerFound || float.IsNaN(yMin) || float.IsNaN(yMax))
        {
            // We make the start point in the center of the scaledOverhangStartRegion
            float yCenter = (startPoints.Min(pt => pt.Y) + startPoints.Max(pt => pt.Y)) / 2;
            return new PointF(xCenter, yCenter);
        }

        // Done
        return new(xCenter, (yMin + yMax) / 2f);
    }

    public static List<List<GeometryAndPrintInfo>> GetArcsGeometryInfo(Region overhangRegion, PointF center)
    {
        // For result
        List<List<GeometryAndPrintInfo>> result = [];

        // Create list of arcs by increasing radius step by step
        float radiusIncreaseStep = 0.4f * 0.89f; // Based on 0.4mm nozzle and https://fullcontrol.xyz/#/models/b70938 //TODO: Use real values
        float radius = 0.2f - radiusIncreaseStep;
        float startAngle;
        int nbrOfFailure = 0;
        bool needOneMoreRun;
        do
        {
            // Increase radius, compute angle step to have point on circle moving 0.01mm and reset
            List<GeometryAndPrintInfo> currentRadiusArcs = [];
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
                GeometryAndPrintInfo fullCircle = new(
                    new PointF(center.X + radius, center.Y),
                    new PointF(center.X + radius, center.Y),
                    center,
                    radius,
                    ArcDirection.CounterClockwise);
                currentRadiusArcs.Add(fullCircle);
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
                    float arcLength = (stopAngle - startAngle) * radius * (float)Math.PI / 180;
                    if (arcLength >= 0.6)
                    {
                        // We make sure it make sense to start an extrusion
                        GeometryAndPrintInfo arc = new(
                            GetPoint(center, radius, startAngle),
                            GetPoint(center, radius, stopAngle),
                            center,
                            radius,
                            ArcDirection.CounterClockwise);
                        currentRadiusArcs.Add(arc);
                    }
                    needOneMoreRun = true;
                    startAngle = float.NaN;
                }
            }

            // Full circle
            if (!float.IsNaN(startAngle))
            {
                GeometryAndPrintInfo fullCircle = new(
                    new PointF(center.X + radius, center.Y),
                    new PointF(center.X + radius, center.Y),
                    center,
                    radius,
                    ArcDirection.CounterClockwise);
                currentRadiusArcs.Add(fullCircle);
                needOneMoreRun = true;
            }

            // Save current radius
            if (currentRadiusArcs.Count > 0)
            {
                result.Add(currentRadiusArcs);
                nbrOfFailure = 0;
            }
            else
            {
                needOneMoreRun = true;
                nbrOfFailure++;
            }
        } while (needOneMoreRun && nbrOfFailure < 100);

        return result;
    }

    private static PointF GetPoint(PointF center, float radius, float angleDeg)
    {
        float x = center.X + (float)Math.Cos(angleDeg * Math.PI / 180) * radius;
        float y = center.Y + (float)Math.Sin(angleDeg * Math.PI / 180) * radius;
        return new(x, y);
    }

    public static List<PathInfo> GetArcsPathInfo(List<List<GeometryAndPrintInfo>> allArcsPerRadius)
    {
        List<PathInfo> result = [];
        PathInfo currentPathInfo = new(PathType.Unknown); //TODO:Check value
        foreach (List<GeometryAndPrintInfo> infoPerRadius in allArcsPerRadius)
        {
            foreach (GeometryAndPrintInfo gi in infoPerRadius)
            {
                // Compute segment geometry
                GeometryAndPrintInfo newArcGAPI = new(
                    gi.StartPosition,
                    gi.EndPosition,
                    gi.CenterPosition,
                    gi.Radius,
                    ArcDirection.CounterClockwise);
                newArcGAPI.SetPrintParameter(100, 5, 2); //TODO: Use real values

                // Add new segment
                if (currentPathInfo.NbrOfSegments == 0)
                {
                    // First segment
                    currentPathInfo.AddSegmentInfo(new(newArcGAPI, true));
                    continue;
                }

                // Get current end point and compute distance from start and end to see if we continue the path or start a new one
                PointF previousEnd = currentPathInfo.EndPosition;
                float distToStart = Distance(previousEnd, newArcGAPI.StartPosition);
                float distToEnd = Distance(previousEnd, newArcGAPI.EndPosition);
                if (Math.Min(distToStart, distToEnd) > 1.2f)
                {
                    // We stop segment and redo a new one
                    result.Add(currentPathInfo);
                    currentPathInfo = new(PathType.Unknown);//TODO:Check value
                    currentPathInfo.AddSegmentInfo(new(newArcGAPI, true));
                    continue;
                }

                // Continue...
                if (distToEnd < distToStart)
                {
                    newArcGAPI.InvertDirection();
                }

                // Add line to link
                GeometryAndPrintInfo tmpLineGAPI = new(
                    previousEnd,
                    newArcGAPI.StartPosition);
                tmpLineGAPI.SetPrintParameter(100, 3, 2); //TODO: Use real values
                currentPathInfo.AddSegmentInfo(new(tmpLineGAPI, true));

                // Add segment
                currentPathInfo.AddSegmentInfo(new(newArcGAPI, true));
            }
        }

        // Add current path if not empty
        if (currentPathInfo.NbrOfSegments > 0)
        {
            result.Add(currentPathInfo);
        }

        // Check if we have result
        if (result.Count == 0)
        {
            return result;
        }

        // Add move toward center for each segment
        PointF center = result[0].AllSegments[0].SegmentGeometryInfo.CenterPosition;
        foreach (PathInfo tmpPath in result)
        {
            PointF? correctedStart = MoveStartAndEndTowardCenter(tmpPath.StartPosition, center, 1.2f);
            if (correctedStart != null)
            {
                GeometryAndPrintInfo tmpLineGAPI = new(
                    correctedStart.Value,
                    tmpPath.StartPosition);
                tmpLineGAPI.SetPrintParameter(100, 3, 0.5f); //TODO: Use real values
                tmpPath.InsertSegmentInfo(0, new(tmpLineGAPI, true));
            }

            PointF? correctedEnd = MoveStartAndEndTowardCenter(tmpPath.EndPosition, center, 1.2f);
            if (correctedEnd != null)
            {
                GeometryAndPrintInfo tmpLineGAPI = new(
                    tmpPath.EndPosition,
                    correctedEnd.Value);
                tmpLineGAPI.SetPrintParameter(100, 3, 0.5f); //TODO: Use real values
                tmpPath.AddSegmentInfo(new(tmpLineGAPI, true));
            }
        }

        // All done
        return result;
    }

    private static PointF? MoveStartAndEndTowardCenter(PointF startPosition, PointF centerPosition, float innerMoveDistance)
    {
        // Compute the vector of length v pointing to the center and starting from the start position
        PointF result = new();
        float distance = Distance(startPosition, centerPosition);
        if (distance < innerMoveDistance)
        {
            return null;
        }
        result.X = startPosition.X + innerMoveDistance * (centerPosition.X - startPosition.X) / distance;
        result.Y = startPosition.Y + innerMoveDistance * (centerPosition.Y - startPosition.Y) / distance;
        return result;
    }

    private static float Distance(PointF p1, PointF p2)
    {
        return (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
    }
}