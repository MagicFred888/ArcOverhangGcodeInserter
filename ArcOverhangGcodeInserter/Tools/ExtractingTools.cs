using System.Text.RegularExpressions;
using ArcOverhangGcodeInserter.Info;

namespace ArcOverhangGcodeInserter.Tools;

public static partial class ExtractingTools
{
    [GeneratedRegex(@"^; layer num/total_layer_count: (?<layerNbr>\d+)/\d+$")]
    private static partial Regex LayerStartRegex();

    [GeneratedRegex(@"^G[123] X.+?Y")]
    private static partial Regex ValidGmoveRegex();

    [GeneratedRegex(@"^G[123] X.+?Y.+?E\d*\.\d+$")]
    private static partial Regex ValidGmoveWithExtrusionRegex();

    private const string lastLayerEnd = "; close powerlost recovery";
    private const string startOuterWall = "; FEATURE: Outer wall";
    private const string startOverhangWall = "; FEATURE: Overhang wall";
    private const string startFeature = "; FEATURE:";
    private const string startWipe = "; WIPE_START";

    private enum SearchMode
    {
        SearchStartOuterWall = 0,
        SearchStartInnerWallOrExtrusion = 1,
        RecordGCodeAndLookForStartWipe = 2,
    }

    public static IEnumerable<LayerInfos> ExtractAllLayerInfosFromGCode(List<string> fullGCode)
    {
        // Variables used to extract layers
        int layerNumber = 0;
        int startLayerPos = 0;
        bool isOverhang = false;
        WallInfo currentWall = new();
        List<WallInfo> currentLayerWalls = new();
        GCodeInfo lastValidGmove = new GCodeInfo(0, "", false);
        SearchMode searchMode = SearchMode.SearchStartOuterWall;

        // Full G-code scan
        for (int lineNbr = 1; lineNbr < fullGCode.Count; lineNbr++)
        {
            // Extract line
            string line = fullGCode[lineNbr - 1];

            // Check if new layer
            if (LayerStartRegex().IsMatch(line) || line.Equals(lastLayerEnd))
            {
                // Save current layer
                if (currentLayerWalls.Count > 0)
                {
                    yield return new LayerInfos(layerNumber, currentLayerWalls, fullGCode.GetRange(startLayerPos - 1, lineNbr - startLayerPos));
                }

                // End of the part
                if (line.Equals(lastLayerEnd))
                {
                    break;
                }

                // New layer
                layerNumber = int.Parse(LayerStartRegex().Match(line).Groups["layerNbr"].Value);
                startLayerPos = lineNbr;
                currentWall = new();
                currentLayerWalls = new();
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
                lastValidGmove = new GCodeInfo(lineNbr, line, isOverhang);
            }

            // Action based on search mode
            switch (searchMode)
            {
                case SearchMode.SearchStartOuterWall:
                    // Search start of an outer wall
                    if (line.Equals(startOuterWall))
                    {
                        searchMode = SearchMode.RecordGCodeAndLookForStartWipe;
                    }
                    break;

                case SearchMode.SearchStartInnerWallOrExtrusion:
                    if (line.StartsWith(startFeature))
                    {
                        if (line.Equals(startOuterWall))
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
                        currentWall.AddGCodeInfo(lastValidGmove);
                        currentWall.AddGCodeInfo(new GCodeInfo(lineNbr, line, isOverhang));
                        searchMode = SearchMode.RecordGCodeAndLookForStartWipe;
                    }
                    break;

                case SearchMode.RecordGCodeAndLookForStartWipe:
                    // Add previous valid Gmove to have the starting point
                    if (currentWall.NbrOfGCodeInfo == 0)
                    {
                        currentWall.AddGCodeInfo(lastValidGmove);
                    }

                    // Add valid move
                    if (ValidGmoveWithExtrusionRegex().IsMatch(line)) //&& !currentWall.Contains(line)
                    {
                        currentWall.AddGCodeInfo(new GCodeInfo(lineNbr, line, isOverhang));
                    }

                    // End of a wall ?
                    if (line.StartsWith(startWipe) || (line.StartsWith(startFeature) && !line.Equals(startOverhangWall) && !line.Equals(startOuterWall))) // Second condition if a wall
                    {
                        currentLayerWalls.Add(currentWall);
                        currentWall = new();
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
    }
}