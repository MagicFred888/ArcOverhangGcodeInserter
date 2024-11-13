using ArcOverhangGcodeInserter.Class;
using ArcOverhangGcodeInserter.Tools;
using System.Text.RegularExpressions;

namespace ArcOverhangGcodeInserter.Info;

public partial class ThreeDimensionalPrintInfo
{
    [GeneratedRegex(@"^;\s*total\s*layer\s*number:\s*(?<value>\d+)")]
    private static partial Regex NbrOfLayerRegex();

    [GeneratedRegex(@"^;\s*nozzle_diameter\s*=\s*(?<value>\d+(\.\d+)?)")]
    private static partial Regex NozzleDiameterRegex();

    public string FilePath { get; private set; }

    public List<string> FullGCode { get; private set; }

    public readonly List<LayerInfo> AllLayers;

    private readonly LayerImageTools _layerImageTools;

    public int NbrOfLayers => AllLayers.Count;

    public float NozzleDiameter { get; private set; }

    public ThreeDimensionalPrintInfo(string filePath)
    {
        // Save file path
        FilePath = filePath;

        // Read full GCode
        FullGCode = GCodeAnd3MfFileTools.GetFullGCodeFromFile(FilePath);

        // Find layer number for check
        string? gCode = FullGCode.Find(l => NbrOfLayerRegex().IsMatch(l));
        if (string.IsNullOrEmpty(gCode))
        {
            throw new InvalidDataException("Unable to find number of layers");
        }
        int nbrOfLayers = int.Parse(NbrOfLayerRegex().Match(gCode).Groups["value"].Value);

        // Extract nozzle diameter
        gCode = FullGCode.Find(l => NozzleDiameterRegex().IsMatch(l));
        if (string.IsNullOrEmpty(gCode))
        {
            throw new InvalidDataException("Unable to find nozzle diameter");
        }
        NozzleDiameter = float.Parse(NozzleDiameterRegex().Match(gCode).Groups["value"].Value);

        // Extract information (OuterWall, InnerWall, OverhangArea) from GCode
        Dictionary<int, (List<string> gCode, List<PathInfo> paths)> extraction = GCodeExtractionTools.ExtractAllLayerInfoFromGCode(FullGCode);

        // Check if correct number of layers
        if (nbrOfLayers != extraction.Count)
        {
            throw new InvalidDataException("The number of layers extracted does not match the number of layers found in the GCode file");
        }

        // Create all layer objects
        AllLayers = [];
        for (int layerId = 1; layerId <= nbrOfLayers; layerId++)
        {
            // Create new layer and add in list
            LayerInfo newLayer = new(layerId, extraction[layerId].gCode, extraction[layerId].paths, AllLayers.Count != 0 ? AllLayers[^1] : null);
            AllLayers.Add(newLayer);

            //Compute Overhang Regions
            newLayer.ComputeIfOverhangAndArcsIf();
        }

        // Initialize LayerImageTools
        _layerImageTools = new LayerImageTools(AllLayers);
    }

    public void ExportGCode()
    {
        // Scan each layer
        List<(int start, int stop, List<string> gCode)> gCodeToInsert = [];
        foreach (LayerInfo layer in AllLayers.FindAll(l => l.HaveOverhang))
        {
            gCodeToInsert.AddRange(layer.GetNewOverhangGCode());
        }

        // Check if there is something to insert
        if (gCodeToInsert.Count == 0)
        {
            MessageBox.Show("No overhang detected in the GCode file", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Create new GCode
        List<string> newGCode = [];
        int originalGCodeLineStart = 1;
        foreach ((int start, int stop, List<string> gCode) in gCodeToInsert)
        {
            newGCode.AddRange(FullGCode.GetRange(originalGCodeLineStart - 1, start - originalGCodeLineStart)); // Original code
            newGCode.AddRange(gCode);
            originalGCodeLineStart = stop + 1;
        }
        newGCode.AddRange(FullGCode.GetRange(originalGCodeLineStart - 1, FullGCode.Count - originalGCodeLineStart)); // End of original code

        // Save new GCode
        string newFilePath = Path.Combine(Path.GetDirectoryName(FilePath) ?? throw new DirectoryNotFoundException(), "Modified_" + Path.GetFileNameWithoutExtension(FilePath) + Path.GetExtension(FilePath));
        GCodeAnd3MfFileTools.SaveGCodeFile(FilePath, newFilePath, newGCode);
    }

    public Image GetLayerImage(int layerNumber)
    {
        return _layerImageTools.GetImageFromLayerGraphicsPath(AllLayers[layerNumber - 1]);
    }
}