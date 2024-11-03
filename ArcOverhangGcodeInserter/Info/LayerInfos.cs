using ArcOverhangGcodeInserter.Tools;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Info
{
    public class LayerInfos
    {
        public int LayerIndex { get; private set; }

        public List<WallInfo> OuterWalls { get; private set; }

        public List<string> LayerGCode { get; private set; }

        public GraphicsPath LayerGraphicsPath { get; set; }

        public LayerInfos(int layerIndex, List<WallInfo> outerWalls, List<string> layerGCode)
        {
            // Save basic infos
            LayerIndex = layerIndex;
            OuterWalls = outerWalls;
            LayerGCode = layerGCode;

            // Computing extra infos from G-code
            LayerGraphicsPath = GCodeTools.ConvertGcodeIntoGraphicsPath(outerWalls);
        }
    }
}