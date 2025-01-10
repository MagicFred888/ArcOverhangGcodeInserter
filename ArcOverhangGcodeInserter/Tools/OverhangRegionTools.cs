using ArcOverhangGcodeInserter.Extensions;
using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;
using System.Numerics;

namespace ArcOverhangGcodeInserter.Tools;

public class OverhangRegionTools(float nozzleDiameter)
{
    public List<(Region overhang, Region startOverhang)> ComputeOverhangRegion(LayerInfo previousLayer, LayerInfo currentLayer)
    {
        // Create new Graphic for region emptiness check
        using Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);

        // Create a virtual inner region for CURRENT layer based on outer wall
        List<Region> overhangs = [];
        List<PathInfo> PathInfoToRemove = [];
        foreach (PathInfo path in currentLayer.OverhangInfillAndWallsPaths.FindAll(p => p.Type == PathType.OverhangArea))
        {
            // Make region
            Region region = MakeRegionFromPath(path.AllSegments, 1.25f);

            // Check if real overhang
            using Region mergedOverhangCheck = region.Clone();
            mergedOverhangCheck.Exclude(previousLayer.OuterWallGraphicsPaths);
            if (mergedOverhangCheck.IsEmpty(graphics))
            {
                PathInfoToRemove.Add(path);
            }
            else
            {
                overhangs.Add(region);
            }
        }

        // Remove overhang who are not real overhang
        foreach (PathInfo path in PathInfoToRemove)
        {
            currentLayer.OverhangInfillAndWallsPaths.Remove(path);
        }

        // Join all regions who are intersecting
        List<Region> mergedOverhangs = [];
        for (int pos = 0; pos < overhangs.Count; pos++)
        {
            // Ini
            List<Region> regionToRemove = [];
            Region mergedOverhang = overhangs[pos];

            // Scan
            for (int pos2 = pos + 1; pos2 < overhangs.Count; pos2++)
            {
                if (mergedOverhang.IntersectWith(overhangs[pos2]))
                {
                    mergedOverhang.Union(overhangs[pos2]);
                    regionToRemove.Add(overhangs[pos2]);
                }
            }

            // Remove overhang who have been considered
            foreach (Region region in regionToRemove)
            {
                overhangs.Remove(region);
            }

            // Add if real overhang
            mergedOverhangs.Add(mergedOverhang);
        }

        // Compute overhang wall
        List<Region> overhangWallRegion = [];
        overhangWallRegion.AddRange(currentLayer.OverhangInfillAndWallsPaths
                .Where(p => p.Type is PathType.OuterOverhangWall or PathType.InnerOverhangWall)
                .SelectMany(path => new[] { MakeRegionFromPath(path.AllSegments, 0.8f), new Region(path.FullPath) })
                );

        // Compute final result
        List<(Region overhang, Region startOverhang)> result = [];
        foreach (Region overhang in mergedOverhangs)
        {
            // Remove Overhang wall
            foreach (Region wall in overhangWallRegion)
            {
                overhang.Exclude(wall);
            }
            Region startOverhang = overhang.Clone();
            startOverhang.Intersect(previousLayer.OuterWallGraphicsPaths);
            result.Add((overhang, startOverhang));
        }

        // Done
        return result;
    }

    private Region MakeRegionFromPath(List<SegmentInfo> allSegments, float nozzleDiameterRatio)
    {
        // For each segment, we must define a region composed of 4 points distanced by the nozzle diameter
        // This region will be used to check if the path is overhanging or not

        List<Region> allRegions = [];
        foreach (SegmentInfo segment in allSegments)
        {
            if (segment.SegmentType == SegmentType.Line)
            {
                // Segment
                GraphicsPath path = GetSegmentBoundingBox(segment.SegmentGeometryInfo.StartPosition, segment.SegmentGeometryInfo.EndPosition,
                                                          (nozzleDiameterRatio * nozzleDiameter) / 2);
                allRegions.Add(new(path));
            }
            else
            {
                // Arc
                GraphicsPath path = GetArcBoundingBox(segment.GraphicsPath,
                                                      (Constants.InternalCalculationScaleFactor * nozzleDiameterRatio * nozzleDiameter) / 2);
                allRegions.Add(new(path));
            }
        }

        // Merge all regions
        Region fullPath = allRegions[0];
        for (int i = 1; i < allRegions.Count; i++)
        {
            fullPath.Union(allRegions[i]);
        }
        return fullPath;
    }

    private static GraphicsPath GetArcBoundingBox(GraphicsPath arc, float offset)
    {
        // PointF startPosition, PointF endPosition, PointF centerPosition, float offset
        GraphicsPath widenedPath = (GraphicsPath)arc.Clone();
        widenedPath.Widen(new Pen(Color.Black, 2 * offset));
        return widenedPath;
    }

    private static GraphicsPath GetSegmentBoundingBox(PointF startPosition, PointF endPosition, float offset)
    {
        //       vA1(1) <-- + -------------- + --> vB1(2)
        //         |        ^                ^        |
        //         |        |                |        |
        //         |        A ============== B        |
        //         |        |                |        |
        //         |        v                v        |
        //       vA2(4) <-- + -------------- + --> vB2(3)

        // Calculate vector AB
        Vector2 PointA = new(startPosition.X, startPosition.Y);
        Vector2 PointB = new(endPosition.X, endPosition.Y);
        Vector2 AB = PointB - PointA;

        // Normalize AB to unit vector
        Vector2 unitAB = Vector2.Normalize(AB);

        // Calculate perpendicular vector to AB (rotated 90 degrees counter-clockwise in 2D)
        Vector2 perpAB = new(-unitAB.Y, unitAB.X);

        // Calculate v1 and v2 (parallel to AB and at distance d on both sides) (+ points)
        Vector2 vA1 = PointA + perpAB * offset;
        Vector2 vB1 = PointB + perpAB * offset;
        Vector2 vA2 = PointA - perpAB * offset;
        Vector2 vB2 = PointB - perpAB * offset;

        // Move them outside to add extra space on the extremities
        vA1 -= unitAB * offset;
        vB1 += unitAB * offset;
        vA2 -= unitAB * offset;
        vB2 += unitAB * offset;

        // Create a region from the 4 points
        GraphicsPath path = new();
        PointF[] points = new Vector2[] { vA1, vB1, vB2, vA2 }.Select(p => new PointF(Constants.InternalCalculationScaleFactor * p.X, Constants.InternalCalculationScaleFactor * p.Y)).ToArray();
        path.AddPolygon(points);
        path.CloseFigure();

        // Done
        return path;
    }
}