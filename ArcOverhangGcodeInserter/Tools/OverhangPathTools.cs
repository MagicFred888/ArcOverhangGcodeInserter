using ArcOverhangGcodeInserter.Extensions;
using ArcOverhangGcodeInserter.Info;

namespace ArcOverhangGcodeInserter.Tools;

public partial class OverhangPathTools(float nozzleDiameter)
{
    public List<PathInfo> ComputeNewOverhangPathInfo(List<(Region overhang, Region startOverhang)> overhangRegions, List<PathInfo> overhangInfillAndWallsPaths)
    {
        // Choose how we generate arc
        List<PathInfo> result = [];
        using Graphics gra = Graphics.FromHwnd(IntPtr.Zero);
        foreach ((Region overhang, Region startOverhang) in overhangRegions)
        {
            // Check if overhang is fully inside the start overhang
            Region testRegion = new(overhang.GetBounds(gra));
            testRegion.Exclude(startOverhang.GetBounds(gra));
            if (testRegion.IsEmpty(gra))
            {
                // Find centerPoint
                PointF center = GetCenterPoint(overhang, overhangInfillAndWallsPaths);
                result.AddRange(ComputeInnerCircleOverhang(startOverhang, overhang, center));
                continue;
            }

            // If we arrive here, generaic case
            result.AddRange(ComputeArcOverhang(startOverhang, overhang));
        }

        // Done
        return result;
    }

    private static PointF GetCenterPoint(Region overhang, List<PathInfo> overhangInfillAndWallsPaths)
    {
        // Extract wall within overhang
        List<PathInfo> wallWithinOverhang = [];
        using Graphics gra = Graphics.FromHwnd(IntPtr.Zero);
        foreach (PathInfo wall in overhangInfillAndWallsPaths.Where(w => w.Type is PathType.OuterOverhangWall or PathType.InnerOverhangWall))
        {
            Region testRegion = new(new Region(wall.FullPath).GetBounds(gra));
            testRegion.Exclude(overhang.GetBounds(gra));
            if (testRegion.IsEmpty(gra))
            {
                wallWithinOverhang.Add(wall);
            }
        }

        // Get center of all arcs
        List<GeometryAndPrintInfo> allArcs = [.. wallWithinOverhang
            .SelectMany(wall => wall.AllSegments.Select(segment => segment.SegmentGeometryInfo))];
        allArcs = [.. allArcs.Where(gapi => gapi.Type is SegmentType.ClockwiseArc or SegmentType.CounterClockwiseArc)];
        List<PointF> allCenters = [.. allArcs.Select(gapi => gapi.CenterPosition)];
        if (allCenters.Count == 0)
        {
            return PointF.Empty;
        }

        // Done
        return new PointF(allCenters.Average(pt => pt.X), allCenters.Average(pt => pt.Y)).ScaleUp();
    }

    public static PointF GetPoint(PointF center, float radius, float angleDeg)
    {
        float x = center.X + (float)Math.Cos(angleDeg * Math.PI / 180) * radius;
        float y = center.Y + (float)Math.Sin(angleDeg * Math.PI / 180) * radius;
        return new(x, y);
    }

    public static List<PathInfo> GetArcsPathInfo(List<List<GeometryAndPrintInfo>> allArcsPerRadius, bool startEndTowardCenter, float maxDistanceToChain)
    {
        List<PathInfo> result = [];
        PathInfo currentPathInfo = new(PathType.Unknown);
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
                if (Math.Min(distToStart, distToEnd) > maxDistanceToChain)
                {
                    // We stop segment and redo a new one
                    result.Add(currentPathInfo);
                    currentPathInfo = new(PathType.Unknown);
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
            PointF? correctedStart = MoveStartAndEndTowardCenter(tmpPath.StartPosition, center, startEndTowardCenter ? 1.2f : -1.2f);
            if (correctedStart != null)
            {
                GeometryAndPrintInfo tmpLineGAPI = new(
                    correctedStart.Value,
                    tmpPath.StartPosition);
                tmpLineGAPI.SetPrintParameter(100, 3, 0.5f); //TODO: Use real values
                tmpPath.InsertSegmentInfo(0, new(tmpLineGAPI, true));
            }

            PointF? correctedEnd = MoveStartAndEndTowardCenter(tmpPath.EndPosition, center, startEndTowardCenter ? 1.2f : -1.2f);
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