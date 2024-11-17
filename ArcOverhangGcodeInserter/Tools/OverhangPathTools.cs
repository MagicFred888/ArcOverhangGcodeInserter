using ArcOverhangGcodeInserter.Extensions;
using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools;

public partial class OverhangPathTools(float nozzleDiameter)
{
    private Region _overhang = new();
    private Region _startOverhang = new();
    private List<PathInfo> _overhangWallsPaths = [];

    private PointF _center = PointF.Empty;
    private float _startRadius = 0;
    private float _stopRadius = 0;
    private float _absRadiusChangeStep = 0;

    private readonly Graphics _gra = Graphics.FromHwnd(IntPtr.Zero);

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
            RectangleF overhangBounding = overhang.GetBounds(_gra);
            RectangleF startOverhangBounding = startOverhang.GetBounds(_gra);

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
            Region testRegion = new(new Region(wall.FullPath).GetBounds(_gra));
            testRegion.Exclude(_overhang.GetBounds(_gra));
            if (testRegion.IsEmpty(_gra))
            {
                wallWithinOverhang.Add(wall);
            }
        }

        // Get center of all arcs
        List<GeometryAndPrintInfo> allArcs = [.. wallWithinOverhang
            .SelectMany(wall => wall.AllSegments.Select(segment => segment.SegmentGeometryInfo))];
        allArcs = [.. allArcs.Where(gapi => gapi.Type is SegmentType.ClockwiseArc or SegmentType.CounterClockwiseArc)];
        allArcs = [.. allArcs.Where(gapi => gapi.EvalSweepAngle() > 25)];
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

    private List<PathInfo> LinkGeometryAndPrintInfoAsPathInfo(List<List<GeometryAndPrintInfo>> allArcsPerRadius, bool startEndTowardCenter, float maxDistanceToChain)
    {
        List<PathInfo> result = [];
        while (allArcsPerRadius.Count > 0)
        {
            (allArcsPerRadius, PathInfo path) = ExtractAsFarAsPossible(allArcsPerRadius, startEndTowardCenter, maxDistanceToChain);
            result.Add(path);
        }
        return result;
    }

    private (List<List<GeometryAndPrintInfo>> allArcsPerRadius, PathInfo path) ExtractAsFarAsPossible(List<List<GeometryAndPrintInfo>> allArcsPerRadius, bool startEndTowardCenter, float maxDistanceToChain)
    {
        List<GeometryAndPrintInfo> pathElements = [];

        // Initiate a loop
        GeometryAndPrintInfo info = allArcsPerRadius[0][0];
        pathElements.Add(info);
        allArcsPerRadius[0].Remove(info);
        int scanPos = 1;

        // Loop as much as possible
        while (scanPos < allArcsPerRadius.Count)
        {
            // Check if we can continue
            int newInfoIndex = GetBest(pathElements[^1], allArcsPerRadius[scanPos], allArcsPerRadius.GetRange(0, scanPos - 1).Find(i => i.Count > 0) != null);
            if (newInfoIndex < 0)
            {
                // No more arc to chain
                break;
            }

            // Add and continue
            info = allArcsPerRadius[scanPos][newInfoIndex];
            pathElements.Add(info);
            allArcsPerRadius[scanPos].Remove(info);
            scanPos++;
        }

        // Merge extraction as path
        PathInfo path = ConvertAsPath(pathElements, startEndTowardCenter);
        allArcsPerRadius = allArcsPerRadius.FindAll(i => i.Count > 0);

        // Done
        return (allArcsPerRadius, path);
    }

    private static int GetBest(GeometryAndPrintInfo previousGAPI, List<GeometryAndPrintInfo> possibleGAPI, bool remainInPrevious)
    {
        // To continue, with a full arc, all previous must be done
        if (possibleGAPI.Count == 1 && possibleGAPI[0].SweepAngle >= 360 && remainInPrevious)
        {
            return -1;
        }

        // search item with start or end as close as current end
        int result = -1;
        float minDist = float.MaxValue;
        for (int i = 0; i < possibleGAPI.Count; i++)
        {
            GeometryAndPrintInfo geometryAndPrintInfo = possibleGAPI[i];
            float dist = previousGAPI.EndPosition.Distance(geometryAndPrintInfo.StartPosition);
            if (dist < minDist)
            {
                minDist = dist;
                result = i;
            }
            dist = previousGAPI.EndPosition.Distance(geometryAndPrintInfo.EndPosition);
            if (dist < minDist)
            {
                minDist = dist;
                result = i;
            }
        }
        return result;
    }

    private PathInfo ConvertAsPath(List<GeometryAndPrintInfo> GeometryAndPrintInfoElements, bool startEndTowardCenter)
    {
        PathInfo result = new(PathType.Unknown);
        foreach (GeometryAndPrintInfo gi in GeometryAndPrintInfoElements)
        {
            // Compute segment geometry
            GeometryAndPrintInfo newArcGAPI = new(
                gi.StartPosition,
                gi.EndPosition,
                gi.CenterPosition,
                gi.Radius,
                ArcDirection.CounterClockwise);
            newArcGAPI.SetPrintParameter(Constants.MaxFanSpeedInPercent, Constants.OverhangPrintSpeedInMmPerSecond, Constants.OverhangExtrusionMultiplier);

            // Add new segment and jump to next
            if (result.NbrOfSegments == 0)
            {
                // First segment
                result.AddSegmentInfo(new(newArcGAPI, true));
                continue;
            }

            // Get current end point and compute distance from start and end to see if we continue the path or start a new one
            PointF previousEnd = result.EndPosition;
            float distToStart = previousEnd.Distance(newArcGAPI.StartPosition);
            float distToEnd = previousEnd.Distance(newArcGAPI.EndPosition);

            // Need invert direction ?
            if (distToEnd < distToStart)
            {
                newArcGAPI.InvertDirection();
            }

            // Add line to link
            GeometryAndPrintInfo tmpLineGAPI = new(
                previousEnd,
                newArcGAPI.StartPosition);
            tmpLineGAPI.SetPrintParameter(Constants.MaxFanSpeedInPercent, Constants.OverhangLinkPrintSpeedInMmPerSecond, Constants.OverhangExtrusionMultiplier);
            result.AddSegmentInfo(new(tmpLineGAPI, true));

            // Add segment
            result.AddSegmentInfo(new(newArcGAPI, true));
        }

        // Check if start is outside the startOverang region
        if (!_startOverhang.IsVisible(result.StartPosition.ScaleUp()))
        {
            PointF? correctedStart = CreatePointTowardCenter(result.StartPosition, _center.ScaleDown(), (startEndTowardCenter ? 1f : -1f) * Constants.OverhangStartEndLength);
            if (correctedStart != null)
            {
                GeometryAndPrintInfo tmpLineGAPI = new(
                    correctedStart.Value,
                    result.StartPosition);
                tmpLineGAPI.SetPrintParameter(Constants.MaxFanSpeedInPercent, Constants.OverhangLinkPrintSpeedInMmPerSecond, Constants.OverhangStartEndExtrusionMultiplier);
                result.InsertSegmentInfo(0, new(tmpLineGAPI, true));
            }
        }

        // Check if end is outside the startOverang region
        if (!_startOverhang.IsVisible(result.EndPosition.ScaleUp()))
        {
            PointF? correctedEnd = CreatePointTowardCenter(result.EndPosition, _center.ScaleDown(), (startEndTowardCenter ? 1f : -1f) * Constants.OverhangStartEndLength);
            if (correctedEnd != null)
            {
                GeometryAndPrintInfo tmpLineGAPI = new(
                    result.EndPosition,
                    correctedEnd.Value);
                tmpLineGAPI.SetPrintParameter(Constants.MaxFanSpeedInPercent, Constants.OverhangLinkPrintSpeedInMmPerSecond, Constants.OverhangStartEndExtrusionMultiplier);
                result.AddSegmentInfo(new(tmpLineGAPI, true));
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