using ArcOverhangGcodeInserter.Tools;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Info
{
    public class LayerInfo(int layerIndex, List<string> layerGCode)
    {
        public int LayerIndex { get; private set; } = layerIndex;

        public List<string> LayerGCode { get; private set; } = layerGCode;

        public List<WallInfo> OuterWalls { get; private set; } = [];

        public GraphicsPath OuterWallGraphicsPath { get; private set; } = new();

        public List<WallInfo> InnerWalls { get; private set; } = [];

        public GraphicsPath InnerWallGraphicsPath { get; private set; } = new();

        public Region? OverhangRegion { get; private set; } = null;

        public Region? OverhangStartRegion { get; private set; } = null;

        public List<WallInfo> NewOverhangArcsWalls { get; private set; } = [];

        public void AddOuterWallInfo(List<WallInfo> wallInfos)
        {
            OuterWalls = wallInfos;
            OuterWallGraphicsPath = GCodeTools.ConvertGcodeIntoGraphicsPath(wallInfos, true);
        }

        public void AddInnerWallInfo(List<WallInfo> wallInfos)
        {
            InnerWalls = wallInfos;
            InnerWallGraphicsPath = GCodeTools.ConvertGcodeIntoGraphicsPath(wallInfos, true);
        }

        public void AddOverhangRegion(Region overhangRegion, Region overhangStartRegion)
        {
            // Check if region is empty (to remove internal bridges under top layers)
            using Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            if (overhangRegion.IsEmpty(g))
            {
                return;
            }

            OverhangRegion = overhangRegion;
            OverhangStartRegion = overhangStartRegion;
        }

        public void ComputeArcs()
        {
            // Check
            if (OverhangRegion == null || OverhangStartRegion == null)
            {
                return;
            }

            // Compute arcs
            PointF center = OverhangTools.GetArcsCenter(OverhangRegion, OverhangStartRegion);
            List<List<GraphicsPath>> allArcsPerRadius = OverhangTools.GetArcs(OverhangRegion, center);
            NewOverhangArcsWalls = OverhangTools.GetArcsWallInfo(allArcsPerRadius);
        }

        public bool HaveOverhang
        {
            get
            {
                return OverhangRegion != null;
            }
        }
    }
}