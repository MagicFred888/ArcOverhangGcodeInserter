using ArcOverhangGcodeInserter.Tools;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Info
{
    public partial class LayerInfo
    {
        private readonly LayerInfo? _previousLayer;

        public int LayerIndex { get; private set; }

        public List<string> LayerGCode { get; private set; }

        public float LayerZPos { get; private set; }

        public float LayerHeight { get; private set; }

        public List<PathInfo> OuterWalls { get; private set; }

        public GraphicsPath OuterWallGraphicsPath { get; private set; } = new();

        public List<PathInfo> InnerWalls { get; private set; }

        public GraphicsPath InnerWallGraphicsPath { get; private set; } = new();

        public List<PathInfo> Overhang { get; private set; }

        public Region? OverhangRegion { get; private set; } = null;

        public Region? OverhangStartRegion { get; private set; } = null;

        public List<PathInfo> NewOverhangArcsWalls { get; private set; } = [];

        public LayerInfo(int layerIndex, List<string> layerGCode, List<PathInfo> paths, LayerInfo? previousLayer)
        {
            LayerIndex = layerIndex;
            LayerGCode = layerGCode;
            _previousLayer = previousLayer;

            // Etxract layer information from G-Code
            LayerZPos = GetLayerZPos();
            LayerHeight = GetLayerHeight();

            // Clean path by removing all extrusion not made at current reference layer
            string layerHeightStr = LayerZPos.ToString("#.#");
            for (int i = 0; i < paths.Count; i++)
            {
                PathInfo pi = paths[i];
                List<SegmentInfo> newSegments = pi.AllSegments.FindAll(x => !x.GCodeCommand.Contains(" Z") || x.GCodeCommand.Contains($" Z{layerHeightStr} "));
                if (newSegments.Count != pi.AllSegments.Count)
                {
                    if (newSegments.Count == 0)
                    {
                        paths.RemoveAt(i);
                        i--;
                        continue;
                    }
                    PathInfo newPi = new(pi.Type);
                    foreach (SegmentInfo si in newSegments)
                    {
                        newPi.AddSegmentInfo(si);
                    }
                    paths[i] = newPi;
                }
            }

            // Save paths
            OuterWalls = paths.FindAll(x => x.Type == PathType.OuterWall);
            if (OuterWalls.Count > 0)
            {
                OuterWallGraphicsPath = CombinePaths(OuterWalls);
            }
            InnerWalls = paths.FindAll(x => x.Type == PathType.InnerWall);
            if (InnerWalls.Count > 0)
            {
                InnerWallGraphicsPath = CombinePaths(InnerWalls);
            }
            Overhang = paths.FindAll(x => x.Type == PathType.OverhangArea);
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
            if (!center.IsEmpty)
            {
                List<List<GeometryAndPrintInfo>> allArcsPerRadius = OverhangTools.GetArcsGeometryInfo(OverhangRegion, center);
                NewOverhangArcsWalls = OverhangTools.GetArcsPathInfo(allArcsPerRadius);
            }
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
            string key = "; Z_HEIGHT: ";
            string? gCodeLine = LayerGCode.Find(c => c.StartsWith(key));
            if (string.IsNullOrEmpty(gCodeLine)) throw new InvalidDataException("Unable to find Z_HEIGHT:");
            return float.Parse(gCodeLine.Replace(key, "").Trim());
        }

        private float GetLayerHeight()
        {
            string key = "; LAYER_HEIGHT: ";
            string? gCodeLine = LayerGCode.Find(c => c.StartsWith(key));
            if (string.IsNullOrEmpty(gCodeLine)) throw new InvalidDataException("Unable to find Z_HEIGHT:");
            return float.Parse(gCodeLine.Replace(key, "").Trim());
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
            GCodeTools gCodeTools = new(LayerZPos, LayerHeight, 0.4f, 1.75f); //TODO: Use real values
            List<string> newGCode = gCodeTools.GetFullGCodeSequence(NewOverhangArcsWalls);

            // Done
            return [(Overhang[0].FullGCodeStartLine, Overhang[0].FullGCodeEndLine, newGCode)];
        }
    }
}