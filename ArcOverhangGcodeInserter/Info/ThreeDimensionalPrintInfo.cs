using ArcOverhangGcodeInserter.Class;
using ArcOverhangGcodeInserter.Tools;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace ArcOverhangGcodeInserter.Info;

public partial class ThreeDimensionalPrintInfo
{
    [GeneratedRegex(@"^;\s*nozzle_diameter\s*=\s*(?<value>\d+(\.\d+)?)")]
    private static partial Regex NozzleDiameterRegex();

    private readonly LayerImageTools _layerImageTools;

    public string FilePath { get; private set; }

    public List<string> FullGCode { get; private set; }

    public ImmutableList<LayerInfo> AllLayers { get; init; }

    public int NbrOfLayers => AllLayers.Count;

    public float NozzleDiameter { get; private set; }

    public ThreeDimensionalPrintInfo(string filePath)
    {
        // Save file path
        FilePath = filePath;

        // Read full GCode
        FullGCode = GCodeAnd3MfFileTools.GetFullGCodeFromFile(FilePath);

        // Extract nozzle diameter first since it will be used by sub-procedure
        string? gCode = FullGCode.Find(l => NozzleDiameterRegex().IsMatch(l));
        if (string.IsNullOrEmpty(gCode))
        {
            throw new InvalidDataException("Unable to find nozzle diameter");
        }
        NozzleDiameter = float.Parse(NozzleDiameterRegex().Match(gCode).Groups["value"].Value);

        // Extract information (OuterWall, InnerWall, OverhangArea) from GCode
        AllLayers = [.. GCodeExtractionTools.ExtractAllLayerInfoFromGCode(this, FullGCode)];

        // Initialize LayerImageTools
        _layerImageTools = new LayerImageTools(AllLayers);
    }

    public void ExportGCode(string targetFolder)
    {
        // Scan each layer
        List<(List<(int start, int stop)> toRemove, List<string> gCodeToAdd)> gCodeChange = [];
        foreach (LayerInfo layer in AllLayers.FindAll(l => l.HaveOverhang))
        {
            gCodeChange.Add(layer.GetLayerGCodeChangeInfo());
        }

        // Check if there is something to insert
        if (gCodeChange.Count == 0)
        {
            MessageBox.Show("No overhang detected in the GCode file", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Create new GCode
        List<string> newGCode = [];
        int originalGCodeLineStart = 1;
        foreach ((List<(int start, int stop)> toRemove, List<string> gCodeToAdd) in gCodeChange)
        {
            bool first = true;
            foreach ((int start, int stop) in toRemove)
            {
                newGCode.AddRange(FullGCode.GetRange(originalGCodeLineStart - 1, start - originalGCodeLineStart)); // Original code
                if (first)
                {
                    newGCode.AddRange(gCodeToAdd);
                    first = false;
                }
                originalGCodeLineStart = stop + 1;
            }
        }
        newGCode.AddRange(FullGCode.GetRange(originalGCodeLineStart - 1, FullGCode.Count - originalGCodeLineStart)); // End of original code

        // Save new GCode
        string newFilePath = Path.Combine(targetFolder, "Modified_" + Path.GetFileNameWithoutExtension(FilePath) + Path.GetExtension(FilePath));
        GCodeAnd3MfFileTools.SaveGCodeFile(FilePath, newFilePath, newGCode);
    }

    public Image GetLayerImage(int layerNumber)
    {
        return _layerImageTools.GetImageFromLayerGraphicsPath(AllLayers[layerNumber - 1]);
    }
}