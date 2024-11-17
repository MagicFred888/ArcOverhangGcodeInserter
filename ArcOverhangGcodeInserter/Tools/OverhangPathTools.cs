using ArcOverhangGcodeInserter.Extensions;
using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools;

public partial class OverhangPathTools(float nozzleDiameter)
{
    private Region _overhang = new();
    private Region _startOverhang = new();

    private List<PathInfo> _overhangWallsPaths = [];

    private readonly Graphics gra = Graphics.FromHwnd(IntPtr.Zero);

    public List<PathInfo> ComputeNewOverhangPathInfo(List<(Region overhang, Region startOverhang)> overhangData, List<PathInfo> overhangInfillAndWallsPaths)
    {
        // Ini
        List<PathInfo> result = [];
        _overhangWallsPaths = [.. overhangInfillAndWallsPaths.Where(w => w.Type is PathType.OuterOverhangWall or PathType.InnerOverhangWall)];

        foreach ((Region overhang, Region startOverhang) in overhangData)
        {
            // Make current info availlable for all methods
            _overhang = overhang;
            _startOverhang = startOverhang;
            RectangleF overhangBounding = overhang.GetBounds(gra);
            RectangleF startOverhangBounding = startOverhang.GetBounds(gra);

            // Check if overhangBounding is fully inside the startOverhangBounding
            if (startOverhangBounding.Contains(overhangBounding))
            {
                // Find centerPoint
                result.AddRange(ComputeInnerCircleOverhang());
                continue;
            }

            // Check if overhang is fully inside the start overhang
            if (startOverhangBounding.Left > overhangBounding.Left && startOverhangBounding.Right < overhangBounding.Right &&
                startOverhangBounding.Top > overhangBounding.Top && startOverhangBounding.Bottom < overhangBounding.Bottom)
            {
                // Need OuterArc filling
                throw new NotImplementedException("OuterArc filling not implemented yet"); // TODO: Implement OuterArc filling
            }

            // If we arrive here, generic case
            result.AddRange(ComputeArcOverhang());
        }

        // Done
        return result;
    }

    private PointF GetCenterPointFromWalls()
    {
        // Extract wall within overhang
        List<PathInfo> wallWithinOverhang = [];
        foreach (PathInfo wall in _overhangWallsPaths)
        {
            Region testRegion = new(new Region(wall.FullPath).GetBounds(gra));
            testRegion.Exclude(_overhang.GetBounds(gra));
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

    public PointF GetCenterPointFromStartArea()
    {
        // Extract all points from start region
        List<PointF> startPoints = [];
        foreach (RectangleF rect in _startOverhang.GetRegionScans(new Matrix()))
        {
            // extract each point
            startPoints.Add(new PointF(rect.Left, rect.Top));
            startPoints.Add(new PointF(rect.Left, rect.Bottom));
            startPoints.Add(new PointF(rect.Right, rect.Top));
            startPoints.Add(new PointF(rect.Right, rect.Bottom));
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
        for (float y = startPoints.Min(pt => pt.Y); y < startPoints.Max(pt => pt.Y); y++)
        {
            // Check if point xCenter, y is inside the overhang region
            if (float.IsNaN(yMin) && _startOverhang.IsVisible(xCenter, y))
            {
                // Compute the arc
                yMin = y;
                yMax = y;
            }
            if (!float.IsNaN(yMax) && _startOverhang.IsVisible(xCenter, y))
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

    public List<PathInfo> LinkGeometryAndPrintInfoAsPathInfo(List<List<GeometryAndPrintInfo>> allArcsPerRadius, bool startEndTowardCenter, float maxDistanceToChain)
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
                newArcGAPI.SetPrintParameter(Constants.MaxFanSpeedInPercent, Constants.OverhangPrintSpeedInMmPerSecond, Constants.OverhangExtrusionMultiplier);

                // Add new segment
                if (currentPathInfo.NbrOfSegments == 0)
                {
                    // First segment
                    currentPathInfo.AddSegmentInfo(new(newArcGAPI, true));
                    continue;
                }

                // Get current end point and compute distance from start and end to see if we continue the path or start a new one
                PointF previousEnd = currentPathInfo.EndPosition;
                float distToStart = previousEnd.Distance(newArcGAPI.StartPosition);
                float distToEnd = previousEnd.Distance(newArcGAPI.EndPosition);
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
                tmpLineGAPI.SetPrintParameter(Constants.MaxFanSpeedInPercent, Constants.OverhangLinkPrintSpeedInMmPerSecond, Constants.OverhangExtrusionMultiplier);
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
            // Check if start is outside the startOverang region
            if (!_startOverhang.IsVisible(tmpPath.StartPosition.ScaleUp()))
            {
                PointF? correctedStart = CreatePointTowardCenter(tmpPath.StartPosition, center, (startEndTowardCenter ? 1f : -1f) * Constants.OverhangStartEndLength);
                if (correctedStart != null)
                {
                    GeometryAndPrintInfo tmpLineGAPI = new(
                        correctedStart.Value,
                        tmpPath.StartPosition);
                    tmpLineGAPI.SetPrintParameter(Constants.MaxFanSpeedInPercent, Constants.OverhangLinkPrintSpeedInMmPerSecond, Constants.OverhangStartEndExtrusionMultiplier);
                    tmpPath.InsertSegmentInfo(0, new(tmpLineGAPI, true));
                }
            }

            // Check if end is outside the startOverang region
            if (!_startOverhang.IsVisible(tmpPath.EndPosition.ScaleUp()))
            {
                PointF? correctedEnd = CreatePointTowardCenter(tmpPath.EndPosition, center, (startEndTowardCenter ? 1f : -1f) * Constants.OverhangStartEndLength);
                if (correctedEnd != null)
                {
                    GeometryAndPrintInfo tmpLineGAPI = new(
                        tmpPath.EndPosition,
                        correctedEnd.Value);
                    tmpLineGAPI.SetPrintParameter(Constants.MaxFanSpeedInPercent, Constants.OverhangLinkPrintSpeedInMmPerSecond, Constants.OverhangStartEndExtrusionMultiplier);
                    tmpPath.AddSegmentInfo(new(tmpLineGAPI, true));
                }
            }
        }

        // All done
        return result;
    }

    private static PointF? CreatePointTowardCenter(PointF startPosition, PointF centerPosition, float innerMoveDistance)
    {
        // Compute the vector of length v pointing to the center and starting from the start position
        PointF result = new();
        float distance = startPosition.Distance(centerPosition);
        if (distance < innerMoveDistance)
        {
            return null;
        }
        result.X = startPosition.X + innerMoveDistance * (centerPosition.X - startPosition.X) / distance;
        result.Y = startPosition.Y + innerMoveDistance * (centerPosition.Y - startPosition.Y) / distance;
        return result;
    }
}