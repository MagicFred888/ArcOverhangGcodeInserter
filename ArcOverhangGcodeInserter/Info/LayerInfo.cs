using ArcOverhangGcodeInserter.Tools;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace ArcOverhangGcodeInserter.Info
{
    public partial class LayerInfo
    {
        [GeneratedRegex(@"G1\s+Z(?<Z>[\d\.]+)")]
        private static partial Regex ZPosRegex();

        private readonly LayerInfo? _previousLayer;

        public int LayerIndex { get; private set; }

        public List<string> LayerGCode { get; private set; }

        public float LayerZPos { get; private set; }

        public float LayerHeight { get; private set; }

        public List<PathInfo> OuterWalls { get; private set; } = [];

        public GraphicsPath OuterWallGraphicsPath { get; private set; } = new();

        public List<PathInfo> InnerWalls { get; private set; } = [];

        public GraphicsPath InnerWallGraphicsPath { get; private set; } = new();

        public List<PathInfo> Overhang { get; private set; } = [];

        public Region? OverhangRegion { get; private set; } = null;

        public Region? OverhangStartRegion { get; private set; } = null;

        public List<PathInfo> NewOverhangArcsWalls { get; private set; } = [];

        public LayerInfo(int layerIndex, List<string> layerGCode, LayerInfo? previousLayer)
        {
            LayerIndex = layerIndex;
            LayerGCode = layerGCode;
            _previousLayer = previousLayer;

            // Compute layer Z position and height
            LayerZPos = GetLayerZPos();
            LayerHeight = _previousLayer != null ? LayerZPos - _previousLayer.LayerZPos : LayerZPos;
        }

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

        public void AddOverhangInfo(List<PathInfo> overhang)
        {
            Overhang = overhang;
        }

        public void ComputeIfOverhangAndArcsIf()
        {
            // Check if overhang is present and not first layer
            if (Overhang.Count == 0 || _previousLayer == null)
            {
                return;
            }

            // Compute Overhang Regions and exit if empty
            (Region overhangRegion, Region overhangStartRegion) = RegionTools.ComputeOverhangRegion(_previousLayer, this);
            using Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            if (overhangRegion.IsEmpty(g))
            {
                return;
            }
            OverhangRegion = overhangRegion;
            OverhangStartRegion = overhangStartRegion;

            // Compute arcs
            PointF center = OverhangTools.GetArcsCenter(OverhangRegion, OverhangStartRegion);
            List<List<SegmentGeometryInfo>> allArcsPerRadius = OverhangTools.GetArcsGeometryInfo(OverhangRegion, center);
            NewOverhangArcsWalls = OverhangTools.GetArcsPathInfo(allArcsPerRadius);
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

        public bool HaveOverhang
        {
            get
            {
                return OverhangRegion != null;
            }
        }

        private float GetLayerZPos()
        {
            // Find all Z position in gCode
            List<string> gCodeLine = LayerGCode.FindAll(x => ZPosRegex().IsMatch(x));

            // Count number of each values in gCodeLine and keep biggest one
            string maxKey = string.Empty;
            int maxValue = 0;
            foreach (string key in gCodeLine.Distinct())
            {
                if (gCodeLine.Count(x => x == key) > maxValue)
                {
                    maxKey = key;
                    maxValue = gCodeLine.Count(x => x == key);
                }
            }

            // Return value
            Match match = ZPosRegex().Match(maxKey);
            return float.Parse(match.Groups["Z"].Value);
        }

        public override string ToString()
        {
            return $"Layer {LayerIndex} - ZPos: {LayerZPos} - Height: {LayerHeight} -HaveOverhang: {HaveOverhang}";
        }

        public List<(int start, int stop, List<string> gCode)> GetNewOverhangGCode()
        {
            if (Overhang.Count == 0)
            {
                return [];
            }

            //TODO: Implement overhang with more than one path
            if (Overhang.Count > 1)
            {
                throw new NotImplementedException("Overhang with more than one path are not yet supported");
            }

            // Prepare fully working G-Code sequence
            GCodeTools gCodeTools = new(LayerZPos, 0.4f, 1.75f);
            List<string> newGCode = gCodeTools.GetFullGCodeSequence(NewOverhangArcsWalls);

            // Done
            return [(Overhang[0].FullGCodeStartLine, Overhang[0].FullGCodeEndLine, newGCode)];
        }
    }
}