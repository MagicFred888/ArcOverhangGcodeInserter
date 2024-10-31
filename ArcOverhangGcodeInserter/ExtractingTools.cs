using System.Text.RegularExpressions;

namespace ArcOverhangGcodeInserter;

public static partial class ExtractingTools
{
    private const string lastLayerEnd = "; close powerlost recovery";
    private const string startOuterWall = "; FEATURE: Outer wall";
    private const string commentStringStart = "; ";

    public static List<List<string>> ExtractOuterLayerGcode(List<string> layerGcode)
    {
        // For result
        List<List<string>> result = [];

        // Scan layer to find all outer wall
        bool searchingStartOuterWall = true;
        string lastValidGmove = string.Empty;
        List<string> currentWall = [];
        foreach (string line in layerGcode)
        {
            // Keep last valid G-code move to get starting point
            if (ValidGmoveRegex().IsMatch(line))
            {
                lastValidGmove = line;
            }

            // Check if searching start of Outer wall
            if (searchingStartOuterWall)
            {
                if (line.Equals(startOuterWall, StringComparison.InvariantCultureIgnoreCase))
                {
                    searchingStartOuterWall = false;
                }
                continue;
            }

            // Add previous valid Gmove to have the starting point
            if (currentWall.Count == 0)
            {
                currentWall.Add(lastValidGmove);
            }

            // Add valid move
            if (ValidGmoveRegex().IsMatch(line))
            {
                currentWall.Add(line);
            }

            // End of a wall ?
            if (line.StartsWith(commentStringStart))
            {
                result.Add(currentWall);
                currentWall = [];
                searchingStartOuterWall = true;
            }
        }

        // Done
        return result;
    }

    public static Dictionary<int, List<string>> GetCodePerLayer(List<string> fileContent)
    {
        // For result
        Dictionary<int, List<string>> result = [];

        // Get each layer start and stop pos
        List<string> layerStartAndEnd = fileContent.ToList().FindAll(l => LayerStartRegex().IsMatch(l));
        layerStartAndEnd.Add(lastLayerEnd);

        // Extract layer code
        for (int i = 0; i < layerStartAndEnd.Count - 1; i++)
        {
            int startPos = fileContent.IndexOf(layerStartAndEnd[i]);
            int endPos = fileContent.IndexOf(layerStartAndEnd[i + 1]);
            result.Add(i + 1, fileContent.GetRange(startPos, endPos - startPos));
        }

        // Done
        return result;
    }

    [GeneratedRegex("^; layer num/total_layer_count: \\d+/\\d+$")]
    private static partial Regex LayerStartRegex();

    [GeneratedRegex("^G[123] X.+?Y")]
    private static partial Regex ValidGmoveRegex();
}