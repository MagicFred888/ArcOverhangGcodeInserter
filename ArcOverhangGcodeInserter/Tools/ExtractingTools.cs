using System.Text;
using System.Text.RegularExpressions;

namespace ArcOverhangGcodeInserter.Tools;

public static partial class ExtractingTools
{
    private enum SearchMode
    {
        SearchStartOuterWall = 0,
        SearchStartInnerWallOrExtrusion = 1,
        RecordGCodeAndLookForStartWipe = 2,
    }

    private const string lastLayerEnd = "; close powerlost recovery";
    private const string startOuterWall = "; FEATURE: Outer wall";
    private const string startOverhangWall = "; FEATURE: Overhang wall";
    private const string startFeature = "; FEATURE:";
    private const string startWipe = "; WIPE_START";

    public static List<LayerInfos> ExtractAllLayerInfosFromGCode(string gCodeFilePath)
    {
        // Read file
        List<string> fileContent = [.. File.ReadAllLines(gCodeFilePath, Encoding.UTF8)];

        // Get each layer start and stop pos
        List<string> layerStartAndEnd = fileContent.ToList().FindAll(l => LayerStartRegex().IsMatch(l));
        layerStartAndEnd.Add(lastLayerEnd);

        // Extract info from all layer
        List<LayerInfos> result = [];
        for (int i = 1; i < layerStartAndEnd.Count; i++)
        {
            int startPos = fileContent.IndexOf(layerStartAndEnd[i - 1]);
            int endPos = fileContent.IndexOf(layerStartAndEnd[i]);
            result.Add(new LayerInfos(i, fileContent.GetRange(startPos, endPos - startPos)));
        }

        // Done
        return result;
    }

    public static List<List<string>> ExtractAllLayerInfosFromGCode(List<string> layerGCode)
    {
        // For result
        List<List<string>> result = [];

        // Scan layer to find all outer wall
        List<string> currentWall = [];
        string lastValidGmove = string.Empty;
        SearchMode searchMode = SearchMode.SearchStartOuterWall;
        foreach (string line in layerGCode)
        {
            // Keep last valid G-code move to get starting point
            if (ValidGmoveRegex().IsMatch(line) && !ValidGmoveWithExtrusionRegex().IsMatch(line))
            {
                lastValidGmove = line;
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
                        currentWall.Add(lastValidGmove);
                        currentWall.Add(line);
                        searchMode = SearchMode.RecordGCodeAndLookForStartWipe;
                    }
                    break;

                case SearchMode.RecordGCodeAndLookForStartWipe:
                    // Add previous valid Gmove to have the starting point
                    if (currentWall.Count == 0)
                    {
                        currentWall.Add(lastValidGmove);
                    }

                    // Add valid move
                    if (ValidGmoveWithExtrusionRegex().IsMatch(line) && !currentWall.Contains(line))
                    {
                        currentWall.Add(line);
                    }

                    // End of a wall ?
                    if (line.StartsWith(startWipe) || (line.StartsWith(startFeature) && !line.Equals(startOverhangWall) && !line.Equals(startOuterWall))) // Second condition if a wall
                    {
                        result.Add(currentWall);
                        currentWall = [];
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

    [GeneratedRegex("^; layer num/total_layer_count: \\d+/\\d+$")]
    private static partial Regex LayerStartRegex();

    [GeneratedRegex("^G[123] X.+?Y")]
    private static partial Regex ValidGmoveRegex();

    [GeneratedRegex("^G[123] X.+?Y.+?E\\d*\\.\\d+$")]
    private static partial Regex ValidGmoveWithExtrusionRegex();
}