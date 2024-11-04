using ArcOverhangGcodeInserter.Tools;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Info
{
    public class LayerInfos(int layerIndex, List<string> layerGCode)
    {
        public int LayerIndex { get; private set; } = layerIndex;

        public List<string> LayerGCode { get; private set; } = layerGCode;

        public List<WallInfo> OuterWalls { get; private set; } = [];

        public GraphicsPath OuterWallGraphicsPath { get; private set; } = new();

        public List<WallInfo> InnerWalls { get; private set; } = [];

        public GraphicsPath InnerWallGraphicsPath { get; private set; } = new();

        public List<WallInfo> Overhang { get; private set; } = [];

        public GraphicsPath OverhangGraphicsPath { get; private set; } = new();

        public GraphicsPath OverhangBorderGraphicsPath { get; private set; } = new();

        public void AddOuterWallInfo(List<WallInfo> wallInfos)
        {
            OuterWalls = wallInfos;
            OuterWallGraphicsPath = GCodeTools.ConvertGcodeIntoGraphicsPath(wallInfos);
        }

        public void AddInnerWallInfo(List<WallInfo> wallInfos)
        {
            InnerWalls = wallInfos;
            InnerWallGraphicsPath = GCodeTools.ConvertGcodeIntoGraphicsPath(wallInfos);
        }

        public void AddOverhangGCode(List<WallInfo> overhangInfos)
        {
            Overhang = overhangInfos;
            OverhangGraphicsPath = GCodeTools.ConvertGcodeIntoGraphicsPath(overhangInfos);
        }
    }
}