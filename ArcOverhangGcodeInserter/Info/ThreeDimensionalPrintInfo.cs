using ArcOverhangGcodeInserter.Tools;
using System.Text;

namespace ArcOverhangGcodeInserter.Info;

public class ThreeDimensionalPrintInfo
{
    public List<string> FullGCode { get; private set; }

    public readonly List<LayerInfo> AllLayers;

    private readonly LayerImageTools _layerImageTools;

    public int NbrOfLayers => AllLayers.Count;

    public ThreeDimensionalPrintInfo(string gCodeFilePath)
    {
        // Read GCode file
        if (!File.Exists(gCodeFilePath))
        {
            throw new FileNotFoundException("The specified GCode file does not exist !", gCodeFilePath);
        }

        // Read file and keep it in memory
        FullGCode = [.. File.ReadAllLines(gCodeFilePath, Encoding.UTF8)];

        // Extract each information (even if it need 3 scan of the GCode)
        Dictionary<int, (List<WallInfo> walls, List<string> gCode)> outerWall = ExtractingTools.ExtractAllLayerInfoFromGCode(FullGCode, ExtractingTools.ExtractionType.OuterWall);
        Dictionary<int, (List<WallInfo> walls, List<string> gCode)> innerWall = ExtractingTools.ExtractAllLayerInfoFromGCode(FullGCode, ExtractingTools.ExtractionType.InnerWall);
        Dictionary<int, (List<WallInfo> walls, List<string> gCode)> overhangArea = ExtractingTools.ExtractAllLayerInfoFromGCode(FullGCode, ExtractingTools.ExtractionType.OverhangArea);

        // Create all layer objects
        AllLayers = [];
        foreach (int layerId in outerWall.Keys)
        {
            LayerInfo newLayer = new(layerId, outerWall[layerId].gCode);
            newLayer.AddOuterWallInfo(outerWall[layerId].walls);
            if (innerWall.TryGetValue(layerId, out (List<WallInfo> walls, List<string> gCode) innerWallValue))
            {
                newLayer.AddInnerWallInfo(innerWallValue.walls);
            }
            AllLayers.Add(newLayer);
        }

        // Compute Overhang Regions
        for (int pos = 1; pos < AllLayers.Count; pos++)
        {
            if (!overhangArea.ContainsKey(pos))
            {
                continue;
            }
            (Region overhangRegion, Region overhangStartRegion) = RegionTools.ComputeOverhangRegion(AllLayers[pos - 2], AllLayers[pos - 1]);
            AllLayers[pos - 1].AddOverhangRegion(overhangRegion, overhangStartRegion);
        }

        // Compute all arc needed to fill overhang
        foreach (LayerInfo layer in AllLayers.FindAll(l => l.HaveOverhang))
        {
            layer.ComputeArcs();
        }

        // Initialize LayerImageTools
        _layerImageTools = new LayerImageTools(AllLayers);
    }

    public Image GetLayerImage(int layerNumber)
    {
        return _layerImageTools.GetImageFromLayerGraphicsPath(AllLayers[layerNumber - 1]);
    }
}