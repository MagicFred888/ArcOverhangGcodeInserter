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

    private const string newLayerStart = "; CHANGE_LAYER";
    private const string lastLayerEnd = "; close powerlost recovery";
    private const string startOverhangWall = "; FEATURE: Overhang wall";
    private const string startFeature = "; FEATURE:";
    private const string startWipe = "; WIPE_START";

    public static Dictionary<int, (List<string> gCode, List<PathInfo> paths)> ExtractAllLayerInfoFromGCode(List<string> fullGCode)
    {
        // For result
        Dictionary<int, (List<string> gCode, List<PathInfo>)> result = [];

        // Configuration
        Dictionary<string, PathType> objectToExtract = new()
        {
            { "; FEATURE: Outer wall", PathType.OuterWall },
            { "; FEATURE: Inner wall", PathType.InnerWall },
            { "; FEATURE: Bridge", PathType.OverhangArea },
        };

        // Variables used to extract layers
        int layerNumber = 0;
        int startLayerPos = 0;

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
            if (line.Equals(newLayerStart) || line.Equals(lastLayerEnd))
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
                if (line.Equals(lastLayerEnd))
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

            // Valid layer
            if (layerNumber <= 0)
            {
                continue;
            }

            // Check if overhang
            if (line.StartsWith(startFeature))
            {
                isOverhang = line.Equals(startOverhangWall) || currentPathType == PathType.OverhangArea;
            }

            // Switch type except if we get an overhang wall
            if (line.StartsWith(startFeature))
            {
                if (line.Equals(startOverhangWall))
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
                if (currentPath.Type == PathType.Unknown)
                {
                    currentPath.Type = currentPathType;
                    currentPath.FullGCodeStartLine = lineNbr;
                }
                currentPath.AddSegmentInfo(new SegmentInfo(lineNbr, startPosition, line, isOverhang));
                startPosition = GCodeTools.GetXYFromGCode(line);
                continue;
            }

            if (currentPath.Type == PathType.Unknown)
            {
                continue;
            }

            // End move
            // Two cases:
            // 1. Overhang mode and we have non extrusion move
            // 2. We have a new feature except ; FEATURE: Overhang wall

            if ((isValidMove && !isValidMoveWithExtrusion)
                || (currentPath.Type != PathType.OverhangArea && (line.StartsWith(startWipe) || line.StartsWith(startFeature) && !line.Equals(startOverhangWall) && currentPath.Type != currentPathType)))
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