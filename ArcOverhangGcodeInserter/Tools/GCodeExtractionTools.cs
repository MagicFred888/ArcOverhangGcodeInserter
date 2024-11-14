using ArcOverhangGcodeInserter.Info;
using ArcOverhangGcodeInserter.Tools;
using System.Text.RegularExpressions;

namespace ArcOverhangGcodeInserter.Class;

public static partial class GCodeExtractionTools
{
    [GeneratedRegex(@"^G[123] X.+?Y")]
    private static partial Regex ValidGmoveRegex();

    [GeneratedRegex(@"^G[123] X.+?Y.+?E\d*\.\d+$")]
    private static partial Regex ValidGmoveWithExtrusionRegex();

    private static readonly Dictionary<string, PathType> objectToExtract = new()
        {
            { "; FEATURE: Outer wall", PathType.OuterWall },
            { "; FEATURE: Inner wall", PathType.InnerWall },
            { "; FEATURE: Bridge", PathType.OverhangArea },
            { "; FEATURE: Overhang wall", PathType.UnknownOverhangWall },
        };

    private static readonly string newLayerStartComment = "; CHANGE_LAYER";
    private static readonly string lastLayerEndComment = "; close powerlost recovery";
    private static readonly string startOverhangWallComment = "; FEATURE: Overhang wall";
    private static readonly string startFeatureCommentStart = "; FEATURE:";
    private static readonly string startWipeComment = "; WIPE_START";
    private static readonly string newZHeightCommentStart = "; Z_HEIGHT: ";

    public static List<LayerInfo> ExtractAllLayerInfoFromGCode(List<string> fullGCode)
    {
        Dictionary<int, (List<string> gCode, List<PathInfo> paths)> extraction = ExtractRawDataFromGCode(fullGCode);

        // Create all layer objects
        List<LayerInfo> result = [];
        for (int layerId = 1; layerId <= extraction.Count; layerId++)
        {
            // Fix path type
            List<PathInfo> fixedPaths = FixUnknownOverhangWall(extraction[layerId].paths);

            // Create new layer and add in list
            LayerInfo newLayer = new(layerId, extraction[layerId].gCode, fixedPaths, result.Count != 0 ? result[^1] : null);
            result.Add(newLayer);
        }

        // Clean path by removing all extrusion not made at current reference layer
        return result;
    }

    private static List<PathInfo> FixUnknownOverhangWall(List<PathInfo> paths)
    {
        for (int pos = 0; pos < paths.Count; pos++)
        {
            PathInfo path = paths[pos];
            if (path.Type == PathType.UnknownOverhangWall)
            {
                if (pos < paths.Count - 1 && paths[pos + 1].Type == PathType.UnknownOverhangWall)
                {
                    path.Type = PathType.InnerOverhangWall;
                    paths[pos + 1].Type = PathType.OuterOverhangWall;
                }
                else
                {
                    paths[pos].Type = PathType.OuterOverhangWall;
                }
            }
        }
        return paths;
    }

    private static Dictionary<int, (List<string> gCode, List<PathInfo> paths)> ExtractRawDataFromGCode(List<string> fullGCode)
    {
        // For result
        Dictionary<int, (List<string> gCode, List<PathInfo>)> result = [];

        // Variables used to extract layers
        int layerNumber = 0;
        int startLayerPos = 0;
        string zHeight = string.Empty;

        // Cariables used to extract features
        PathType currentPathType = PathType.Unknown;
        bool isOverhang = false;
        PathInfo currentPath = new(PathType.Unknown);
        List<PathInfo> currentLayerData = [];
        PointF startPosition = PointF.Empty;

        // Full G-code scan
        for (int lineNbr = 1; lineNbr < fullGCode.Count; lineNbr++)
        {
            // Extract line
            string line = fullGCode[lineNbr - 1];

            // Check if new layer
            if (line.Equals(newLayerStartComment) || line.Equals(lastLayerEndComment))
            {
                // Check if we have an open path
                if (currentPath.AllSegments.Count > 0)
                {
                    currentPath.FullGCodeEndLine = lineNbr - 1;
                    currentLayerData.Add(currentPath);
                }

                // Save current layer
                if (currentLayerData.Count > 0)
                {
                    result.Add(layerNumber, (fullGCode.GetRange(startLayerPos - 1, lineNbr - startLayerPos), currentLayerData));
                }

                // End of the part
                if (line.Equals(lastLayerEndComment))
                {
                    break;
                }

                // New layer
                layerNumber = result.Count + 1;
                startLayerPos = lineNbr;
                currentPath = new(PathType.Unknown);
                currentLayerData = [];
                currentPathType = PathType.Unknown;
                continue;
            }

            if (line.StartsWith(newZHeightCommentStart))
            {
                zHeight = $" Z{float.Parse(line.Replace(newZHeightCommentStart, "").Trim()):#.#} ";
            }

            // Valid layer
            if (layerNumber <= 0)
            {
                continue;
            }

            // Check if overhang
            if (line.StartsWith(startFeatureCommentStart))
            {
                isOverhang = line.Equals(startOverhangWallComment) || currentPathType == PathType.OverhangArea;
            }

            // Switch type except if we get an overhang wall within an inner or outer wall
            if (line.StartsWith(startFeatureCommentStart))
            {
                if (line.Equals(startOverhangWallComment) && (currentPathType == PathType.InnerWall || currentPathType == PathType.OuterWall))
                {
                    continue;
                }
                if (objectToExtract.TryGetValue(line, out PathType newPathType))
                {
                    currentPathType = newPathType;
                }
                else
                {
                    currentPathType = PathType.Unknown;
                }
            }

            // Keep last valid G-code move to get starting point
            bool isValidMove = ValidGmoveRegex().IsMatch(line);
            bool isValidMoveWithExtrusion = ValidGmoveWithExtrusionRegex().IsMatch(line);
            if (isValidMove && !isValidMoveWithExtrusion)
            {
                startPosition = GCodeTools.GetXYFromGCode(line);
            }

            // Add valid move
            if (isValidMoveWithExtrusion && currentPathType != PathType.Unknown)
            {
                if (!line.Contains(" Z") || line.Contains(zHeight))
                {
                    if (currentPath.Type == PathType.Unknown)
                    {
                        currentPath.Type = currentPathType;
                        currentPath.FullGCodeStartLine = lineNbr;
                    }
                    currentPath.AddSegmentInfo(new SegmentInfo(lineNbr, startPosition, line, isOverhang));
                }
                startPosition = GCodeTools.GetXYFromGCode(line);
                continue;
            }

            // Unknown path ?
            if (currentPath.Type == PathType.Unknown)
            {
                continue;
            }

            // End of a path?
            if ((isValidMove && !isValidMoveWithExtrusion)
                || (currentPath.Type != PathType.OverhangArea
                   && (line.StartsWith(startWipeComment) || line.StartsWith(startFeatureCommentStart) && !line.Equals(startOverhangWallComment) && currentPath.Type != currentPathType)))
            {
                currentPath.FullGCodeEndLine = lineNbr - 1;
                currentLayerData.Add(currentPath);
                currentPath = new(PathType.Unknown);
            }
        }

        // Done
        return result;
    }
}