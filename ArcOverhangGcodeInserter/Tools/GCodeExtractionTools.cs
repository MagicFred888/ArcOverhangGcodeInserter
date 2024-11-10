using ArcOverhangGcodeInserter.Info;
using ArcOverhangGcodeInserter.Tools;
using System.Text.RegularExpressions;

namespace ArcOverhangGcodeInserter.Class;

public static partial class GCodeExtractionTools
{
    public enum ExtractionType
    {
        OuterWall = 0,
        InnerWall = 1,
        OverhangArea = 2,
    }

    [GeneratedRegex(@"^; layer num/total_layer_count: (?<layerNbr>\d+)/\d+$")]
    private static partial Regex LayerStartRegex();

    [GeneratedRegex(@"^G[123] X.+?Y")]
    private static partial Regex ValidGmoveRegex();

    [GeneratedRegex(@"^G[123] X.+?Y.+?E\d*\.\d+$")]
    private static partial Regex ValidGmoveWithExtrusionRegex();

    private const string lastLayerEnd = "; close powerlost recovery";
    private const string startOverhangWall = "; FEATURE: Overhang wall";
    private const string startFeature = "; FEATURE:";
    private const string startWipe = "; WIPE_START";
    private static string _featureToExtract = "";

    private enum SearchMode
    {
        SearchFeatureStartComment = 0,
        SearchFeatureStartCommentOrExtrusion = 1,
        RecordGCodeAndLookForStartWipe = 2,
    }

    public static Dictionary<int, (List<PathInfo> paths, List<string> gCode)> ExtractAllLayerInfoFromGCode(List<string> fullGCode, ExtractionType extractionType)
    {
        // For result
        Dictionary<int, (List<PathInfo> paths, List<string> gCode)> result = [];

        // Configuration
        _featureToExtract = extractionType switch
        {
            ExtractionType.OuterWall => "; FEATURE: Outer wall",
            ExtractionType.InnerWall => "; FEATURE: Inner wall",
            ExtractionType.OverhangArea => "; FEATURE: Bridge",
            _ => throw new NotImplementedException($"Extraction type \"{extractionType}\" not implemented"),
        };

        // Variables used to extract layers
        int layerNumber = 0;
        int startLayerPos = 0;
        int startFeaturePos = 0;
        bool isOverhang = false;
        PathInfo currentPath = new();
        List<PathInfo> currentLayerPaths = [];
        PointF startPosition = PointF.Empty;
        SearchMode searchMode = SearchMode.SearchFeatureStartComment;

        // Full G-code scan
        for (int lineNbr = 1; lineNbr < fullGCode.Count; lineNbr++)
        {
            // Extract line
            string line = fullGCode[lineNbr - 1];

            // Check if overhang
            if (line.StartsWith(startFeature))
            {
                isOverhang = line.Equals(startOverhangWall) || extractionType == ExtractionType.OverhangArea;
            }

            // Check if new layer
            if (LayerStartRegex().IsMatch(line) || line.Equals(lastLayerEnd))
            {
                // Save current layer
                if (currentLayerPaths.Count > 0)
                {
                    result.Add(layerNumber, (currentLayerPaths, fullGCode.GetRange(startLayerPos - 1, lineNbr - startLayerPos)));
                }

                // End of the part
                if (line.Equals(lastLayerEnd))
                {
                    break;
                }

                // New layer
                layerNumber = int.Parse(LayerStartRegex().Match(line).Groups["layerNbr"].Value);
                startLayerPos = lineNbr;
                startFeaturePos = -1;
                currentPath = new();
                currentLayerPaths = [];
                searchMode = SearchMode.SearchFeatureStartComment;
                continue;
            }

            // Valid layer
            if (layerNumber <= 0)
            {
                continue;
            }

            // Keep last valid G-code move to get starting point
            if (ValidGmoveRegex().IsMatch(line) && !ValidGmoveWithExtrusionRegex().IsMatch(line))
            {
                startPosition = GCodeTools.GetXYFromGCode(line);
            }

            // Action based on search mode
            switch (searchMode)
            {
                case SearchMode.SearchFeatureStartComment:
                    // Search start of an outer wall
                    if (line.Equals(_featureToExtract))
                    {
                        startFeaturePos = lineNbr;
                        searchMode = SearchMode.RecordGCodeAndLookForStartWipe;
                    }
                    break;

                case SearchMode.SearchFeatureStartCommentOrExtrusion:
                    if (line.StartsWith(startFeature))
                    {
                        startFeaturePos = lineNbr;
                        if (line.Equals(_featureToExtract))
                        {
                            // Same case than when SearchStartOuterWall mode
                            searchMode = SearchMode.RecordGCodeAndLookForStartWipe;
                        }
                        else
                        {
                            // Another type of feature so we must find another SearchStartOuterWall case
                            searchMode = SearchMode.SearchFeatureStartComment;
                        }
                    }
                    else if (ValidGmoveWithExtrusionRegex().IsMatch(line))
                    {
                        // New feature without comment in G-code... Bug from Bambu Studio???
                        startFeaturePos = lineNbr;
                        currentPath.AddSegmentInfo(new SegmentInfo(lineNbr, startPosition, line, isOverhang));
                        startPosition = GCodeTools.GetXYFromGCode(line);
                        searchMode = SearchMode.RecordGCodeAndLookForStartWipe;
                    }
                    break;

                case SearchMode.RecordGCodeAndLookForStartWipe:

                    // Add valid move
                    if (ValidGmoveWithExtrusionRegex().IsMatch(line))
                    {
                        currentPath.AddSegmentInfo(new SegmentInfo(lineNbr, startPosition, line, isOverhang));
                        startPosition = GCodeTools.GetXYFromGCode(line);
                    }

                    // End of a wall ?
                    if (line.StartsWith(startWipe) || line.StartsWith(startFeature) && !line.Equals(startOverhangWall) && !line.Equals(_featureToExtract))
                    {
                        currentPath.FullGCodeStartLine = startFeaturePos;
                        currentPath.FullGCodeEndLine = lineNbr - 1;
                        currentLayerPaths.Add(currentPath);
                        currentPath = new();
                        startFeaturePos = -1;
                        if (line.StartsWith(startFeature))
                        {
                            searchMode = SearchMode.SearchFeatureStartComment; // When a new feature come without wip we must serach another outer wall
                        }
                        else
                        {
                            searchMode = SearchMode.SearchFeatureStartCommentOrExtrusion;
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        // Done
        return result;
    }
}