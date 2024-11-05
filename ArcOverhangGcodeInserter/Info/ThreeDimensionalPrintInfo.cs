using ArcOverhangGcodeInserter.Tools;
using System.Text;

namespace ArcOverhangGcodeInserter.Info;

public class ThreeDimensionalPrintInfo
{
    public List<string> FullGCode { get; private set; }

    private readonly List<LayerInfos> _allLayers;
    private readonly LayerImageTools _layerImageTools;

    public int NbrOfLayers => _allLayers.Count;

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
        Dictionary<int, (List<WallInfo> walls, List<string> gCode)> outerWall = ExtractingTools.ExtractAllLayerInfosFromGCode(FullGCode, ExtractingTools.ExtractionType.OuterWall);
        Dictionary<int, (List<WallInfo> walls, List<string> gCode)> innerWall = ExtractingTools.ExtractAllLayerInfosFromGCode(FullGCode, ExtractingTools.ExtractionType.InnerWall);

        // Create all layer objects
        _allLayers = [];
        foreach (int layerId in outerWall.Keys)
        {
            LayerInfos newLayer = new(layerId, outerWall[layerId].gCode);
            newLayer.AddOuterWallInfo(outerWall[layerId].walls);
            if (innerWall.TryGetValue(layerId, out (List<WallInfo> walls, List<string> gCode) innerWallValue))
            {
                newLayer.AddInnerWallInfo(innerWallValue.walls);
            }
            _allLayers.Add(newLayer);
        }

        // Compute Overhang Regions
        for (int pos = 1; pos < _allLayers.Count; pos++)
        {
            (Region overhangRegion, Region overhangStartRegion) = RegionTools.ComputeOverhangRegion(_allLayers[pos - 1], _allLayers[pos]);
            _allLayers[pos].AddOverhangRegion(overhangRegion, overhangStartRegion);
        }

        // Initialize LayerImageTools
        _layerImageTools = new LayerImageTools(_allLayers);
    }

    public Image GetLayerImage(int layerNumber)
    {
        return _layerImageTools.GetImageFromLayerGraphicsPath(_allLayers[layerNumber - 1]);
    }
}