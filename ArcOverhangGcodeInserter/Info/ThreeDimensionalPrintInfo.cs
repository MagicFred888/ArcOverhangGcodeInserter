using ArcOverhangGcodeInserter.Tools;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ArcOverhangGcodeInserter.Info;

public partial class ThreeDimensionalPrintInfo
{
    [GeneratedRegex(@"^;\s*total\s*layer\s*number:\s*(?<value>\d+)")]
    private static partial Regex NbrOfLayerRegex();

    [GeneratedRegex(@"^;\s*nozzle_diameter\s*=\s*(?<value>\d+(\.\d+)?)")]
    private static partial Regex NozzleDiameterRegex();

    private readonly string _gCodeFileNameIn3mf = "plate_1.gcode";

    private readonly string _gCodeMD5FileNameIn3mf = "plate_1.gcode.md5";

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

        // Check and read file
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The specified GCode file does not exist !", filePath);
        }
        if (!Path.GetExtension(filePath).Equals(".3mf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The specified file is not a 3mf file");
        }

        // Open 3mf file as a zip archive
        using ZipArchive archive = ZipFile.OpenRead(filePath);
        if (archive.Entries.Count == 0)
        {
            throw new InvalidDataException("The specified 3mf file is empty");
        }

        // Search plate_1.gcode in the archive (dirty, to be improve later)
        ZipArchiveEntry? zipEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(_gCodeFileNameIn3mf, StringComparison.OrdinalIgnoreCase)) ??
            throw new InvalidDataException($"Unable to find {_gCodeFileNameIn3mf} file in the 3mf archive");

        FullGCode = [];
        using Stream stream = zipEntry.Open();
        using StreamReader reader = new(stream, Encoding.UTF8);
        FullGCode = [.. reader.ReadToEnd().Split("\n")];

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
        Dictionary<int, (List<PathInfo> paths, List<string> gCode)> outerWall = ExtractingTools.ExtractAllLayerInfoFromGCode(FullGCode, ExtractingTools.ExtractionType.OuterWall);
        Dictionary<int, (List<PathInfo> paths, List<string> gCode)> innerWall = ExtractingTools.ExtractAllLayerInfoFromGCode(FullGCode, ExtractingTools.ExtractionType.InnerWall);
        Dictionary<int, (List<PathInfo> paths, List<string> gCode)> overhangArea = ExtractingTools.ExtractAllLayerInfoFromGCode(FullGCode, ExtractingTools.ExtractionType.OverhangArea);

        // Create all layer objects
        AllLayers = [];
        foreach (int layerId in outerWall.Keys)
        {
            // Create new layer and add in list
            LayerInfo newLayer = new(layerId, outerWall[layerId].gCode, AllLayers.Count != 0 ? AllLayers[^1] : null);
            AllLayers.Add(newLayer);

            // Add wall and overhang information if exist
            newLayer.AddOuterWallInfo(outerWall[layerId].paths);
            if (innerWall.TryGetValue(layerId, out (List<PathInfo> paths, List<string> gCode) innerWallValue))
            {
                newLayer.AddInnerWallInfo(innerWallValue.paths);
            }
            if (overhangArea.TryGetValue(layerId, out (List<PathInfo> paths, List<string> gCode) overhangAreaValue))
            {
                newLayer.AddOverhangInfo(overhangAreaValue.paths);
            }

            // Compute Overhang Regions
            newLayer.ComputeIfOverhangAndArcsIf();
        }

        // Check if correct number of layers
        if (nbrOfLayers != AllLayers.Count)
        {
            throw new InvalidDataException("The number of layers extracted does not match the number of layers found in the GCode file");
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
        string newGCodeString = string.Join("\n", newGCode);

        // Duplicate source path
        string newFilePath = Path.Combine(Path.GetDirectoryName(FilePath) ?? throw new DirectoryNotFoundException(), "Modified_" + Path.GetFileNameWithoutExtension(FilePath) + Path.GetExtension(FilePath));
        File.Copy(FilePath, newFilePath, true);

        // Open new file as a zip archive and write new GCode
        using ZipArchive archive = ZipFile.Open(newFilePath, ZipArchiveMode.Update);
        ZipArchiveEntry? zipEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(_gCodeFileNameIn3mf, StringComparison.OrdinalIgnoreCase)) ??
            throw new InvalidDataException("Unable to find plate_1.gcode file in the 3mf archive");
        using Stream zipStream = zipEntry.Open();
        using (StreamWriter writer = new(zipStream, Encoding.UTF8))
        {
            writer.Write(newGCodeString);
            writer.Flush();
        }

        // Update MD5
        zipEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(_gCodeMD5FileNameIn3mf, StringComparison.OrdinalIgnoreCase)) ??
            throw new InvalidDataException($"Unable to find {_gCodeMD5FileNameIn3mf} file in the 3mf archive");
        using Stream md5Stream = zipEntry.Open();
        using (StreamWriter writer = new(md5Stream, Encoding.UTF8))
        {
            string md5 = MD5.HashData(Encoding.UTF8.GetBytes(newGCodeString)).Aggregate(new StringBuilder(), (sb, b) => sb.Append(b.ToString("X2")), sb => sb.ToString());
            writer.Write(md5);
            writer.Flush();
        }
    }

    public Image GetLayerImage(int layerNumber)
    {
        return _layerImageTools.GetImageFromLayerGraphicsPath(AllLayers[layerNumber - 1]);
    }
}