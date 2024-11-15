using ArcOverhangGcodeInserter.Extensions;
using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools;

public static class OverhangRegionTools
{
    public static List<(Region overhang, Region startOverhang)> ComputeOverhangRegion(LayerInfo previousLayer, LayerInfo currentLayer)
    {
        // Create new Graphic for region emptiness check
        using Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);

        // Create a virtual inner region for CURRENT layer based on outer wall
        List<Region> overhangs = [];
        List<PathInfo> PathInfoToRemove = [];
        foreach (PathInfo path in currentLayer.OverhangInfillAndWallsPaths.FindAll(p => p.Type == PathType.OverhangArea))
        {
            // Make region
            Region region = MakeRegionFromPath(path.FullPath, 50f);

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
                .SelectMany(path => new[] { MakeRegionFromPath(path.FullPath, 40f), new Region(path.FullPath) })
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

    private static Region MakeRegionFromPath(GraphicsPath fullPath, float extrusionDiameter)
    {
        // Convert path as a region
        using GraphicsPath widenedPath = (GraphicsPath)fullPath.Clone();
        widenedPath.Widen(new Pen(Color.Black, extrusionDiameter));
        return new(widenedPath);
    }
}