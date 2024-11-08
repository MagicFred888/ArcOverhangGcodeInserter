﻿using ArcOverhangGcodeInserter.Info;
using System.Text.RegularExpressions;

namespace ArcOverhangGcodeInserter.Tools;

public static partial class ExtractingTools
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
        SearchStartOuterWall = 0,
        SearchStartInnerWallOrExtrusion = 1,
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
        bool isOverhang = false;
        PathInfo currentPath = new();
        List<PathInfo> currentLayerPaths = [];
        PointF startPosition = PointF.Empty;
        SearchMode searchMode = SearchMode.SearchStartOuterWall;

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
                currentPath = new();
                currentLayerPaths = [];
                searchMode = SearchMode.SearchStartOuterWall;
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
                case SearchMode.SearchStartOuterWall:
                    // Search start of an outer wall
                    if (line.Equals(_featureToExtract))
                    {
                        searchMode = SearchMode.RecordGCodeAndLookForStartWipe;
                    }
                    break;

                case SearchMode.SearchStartInnerWallOrExtrusion:
                    if (line.StartsWith(startFeature))
                    {
                        if (line.Equals(_featureToExtract))
                        {
                            // Same case than when SearchStartOuterWall mode
                            searchMode = SearchMode.RecordGCodeAndLookForStartWipe;
                        }
                        else
                        {
                            // Another type of feature so we must find another SearchStartOuterWall case
                            searchMode = SearchMode.SearchStartOuterWall;
                        }
                    }
                    else if (ValidGmoveWithExtrusionRegex().IsMatch(line))
                    {
                        // New outer wall without comment in G-code... Bug from Bambu Studio???
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
                    if (line.StartsWith(startWipe) || (line.StartsWith(startFeature) && !line.Equals(startOverhangWall) && !line.Equals(_featureToExtract)))
                    {
                        currentLayerPaths.Add(currentPath);
                        currentPath = new();
                        if (line.StartsWith(startFeature))
                        {
                            searchMode = SearchMode.SearchStartOuterWall; // When a new feature come without wip we must serach another outer wall
                        }
                        else
                        {
                            searchMode = SearchMode.SearchStartInnerWallOrExtrusion;
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