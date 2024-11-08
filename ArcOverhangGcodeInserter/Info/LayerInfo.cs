using ArcOverhangGcodeInserter.Tools;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Info
{
    public class LayerInfo(int layerIndex, List<string> layerGCode)
    {
        public int LayerIndex { get; private set; } = layerIndex;

        public List<string> LayerGCode { get; private set; } = layerGCode;

        public List<PathInfo> OuterWalls { get; private set; } = [];

        public GraphicsPath OuterWallGraphicsPath { get; private set; } = new();

        public List<PathInfo> InnerWalls { get; private set; } = [];

        public GraphicsPath InnerWallGraphicsPath { get; private set; } = new();

        public Region? OverhangRegion { get; private set; } = null;

        public Region? OverhangStartRegion { get; private set; } = null;

        public List<PathInfo> NewOverhangArcsWalls { get; private set; } = [];

        public void AddOuterWallInfo(List<PathInfo> wallInfos)
        {
            OuterWalls = wallInfos;
            OuterWallGraphicsPath = CombinePaths(wallInfos);
        }

        public void AddInnerWallInfo(List<PathInfo> wallInfos)
        {
            InnerWalls = wallInfos;
            InnerWallGraphicsPath = CombinePaths(wallInfos);
        }

        private static GraphicsPath CombinePaths(List<PathInfo> wallInfos)
        {
            GraphicsPath result = new();
            foreach (PathInfo wallInfo in wallInfos)
            {
                result.AddPath(wallInfo.FullPath, true);
                result.CloseFigure();
            }
            return result;
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
            List<List<SegmentGeometryInfo>> allArcsPerRadius = OverhangTools.GetArcsGeometryInfo(OverhangRegion, center);
            NewOverhangArcsWalls = OverhangTools.GetArcsPathInfo(allArcsPerRadius);
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