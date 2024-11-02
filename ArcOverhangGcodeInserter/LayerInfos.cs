using ArcOverhangGcodeInserter.Tools;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter
{
    public class LayerInfos
    {
        public int LayerIndex { get; private set; }

        public List<string> LayerGcode { get; private set; }

        public List<List<string>> OuterWallGCode { get; private set; }

        public GraphicsPath LayerGraphicsPath { get; set; }

        public LayerInfos(int layerIndex, List<string> layerGCode)
        {
            // Save basic infos
            LayerIndex = layerIndex;
            LayerGcode = layerGCode;

            // Computing extra infos from G-code
            OuterWallGCode = ExtractingTools.ExtractAllLayerInfosFromGCode(layerGCode);
            LayerGraphicsPath = GcodeTools.ConvertGcodeIntoGraphicsPath(OuterWallGCode);
        }
    }
}