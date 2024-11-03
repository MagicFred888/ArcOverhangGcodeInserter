using ArcOverhangGcodeInserter.Tools;
using System.Text;

namespace ArcOverhangGcodeInserter.Info;

public class ThreeDimensionalPrintInfo
{
    private readonly List<string> _fullGCode;
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
        _fullGCode = [.. File.ReadAllLines(gCodeFilePath, Encoding.UTF8)];

        // Extract each layers
        _allLayers = ExtractingTools.ExtractAllLayerInfosFromGCode(_fullGCode).ToList();

        // Initialize LayerImageTools
        _layerImageTools = new LayerImageTools(_allLayers);
    }

    public Image GetLayerImage(int layerNumber, bool showOverhang)
    {
        return _layerImageTools.GetImageFromLayerGraphicsPath(_allLayers[layerNumber - 1].LayerGraphicsPath);
    }
}