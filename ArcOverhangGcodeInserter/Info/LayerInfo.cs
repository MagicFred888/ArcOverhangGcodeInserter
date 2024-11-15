using ArcOverhangGcodeInserter.Tools;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Info
{
    public partial class LayerInfo
    {
        private readonly LayerInfo? _previousLayer;

        public int LayerIndex { get; init; }

        public List<string> LayerGCode { get; init; }

        public float LayerZPos { get; init; }

        public float LayerHeight { get; init; }

        public List<PathInfo> OuterWalls { get; init; }

        public GraphicsPath OuterWallGraphicsPaths { get; init; } = new();

        public List<PathInfo> InnerWalls { get; init; }

        public GraphicsPath InnerWallGraphicsPaths { get; init; } = new();

        public List<PathInfo> OverhangInfillAndWallsPaths { get; init; }

        public List<(Region overhang, Region startOverhang)> OverhangRegions { get; private set; } = [];

        public List<PathInfo> NewOverhangArcsWalls { get; private set; } = [];

        public LayerInfo(int layerIndex, List<string> layerGCode, List<PathInfo> paths, LayerInfo? previousLayer)
        {
            LayerIndex = layerIndex;
            LayerGCode = layerGCode;
            _previousLayer = previousLayer;

            // Etxract layer information from G-Code
            LayerZPos = GetLayerZPos();
            LayerHeight = GetLayerHeight();

            // Save paths
            OuterWalls = paths.FindAll(x => x.Type == PathType.OuterWall);
            if (OuterWalls.Count > 0)
            {
                OuterWallGraphicsPaths = CombinePaths(OuterWalls);
            }
            InnerWalls = paths.FindAll(x => x.Type == PathType.InnerWall);
            if (InnerWalls.Count > 0)
            {
                InnerWallGraphicsPaths = CombinePaths(InnerWalls);
            }
            OverhangInfillAndWallsPaths = paths.FindAll(x => x.Type == PathType.OverhangArea || x.Type == PathType.OuterOverhangWall || x.Type == PathType.InnerOverhangWall);

            //Compute Overhang Regions
            ComputeIfOverhangAndArcsIf();
        }

        public void ComputeIfOverhangAndArcsIf()
        {
            // Check if overhang is present and not first layer
            if (OverhangInfillAndWallsPaths.Count == 0 || _previousLayer == null)
            {
                return;
            }

            // Compute Overhang Regions and exit if empty
            OverhangRegions = OverhangRegionTools.ComputeOverhangRegion(_previousLayer, this);
            if (OverhangRegions.Count == 0)
            {
                // Remove if any due to infill overhang who do not interest us
                OverhangInfillAndWallsPaths.Clear();
                OuterWalls.ForEach(wall => wall.AllSegments.ForEach(segment => segment.SetOverhangState(false)));
                InnerWalls.ForEach(wall => wall.AllSegments.ForEach(segment => segment.SetOverhangState(false)));
                return;
            }

            // Compute arcs
            NewOverhangArcsWalls = OverhangNewPathTools.ComputeNewOverhangArcsWalls(OverhangRegions);
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
                return OverhangRegions.Count > 0;
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

        public List<(int start, int stop, List<string> gCode)> GetNewOverhangGCode()
        {
            if (OverhangInfillAndWallsPaths.Count == 0)
            {
                return [];
            }

            //TODO: Implement overhang with more than one path
            if (OverhangInfillAndWallsPaths.Count > 1)
            {
                throw new NotImplementedException("Overhang with more than one path are not yet supported");
            }

            // Prepare fully working G-Code sequence
            GCodeTools gCodeTools = new(LayerZPos, LayerHeight, 0.4f, 1.75f); //TODO: Use real values
            List<string> newGCode = gCodeTools.GetFullGCodeSequence(NewOverhangArcsWalls);

            // Done
            return [(OverhangInfillAndWallsPaths[0].FullGCodeStartLine, OverhangInfillAndWallsPaths[0].FullGCodeEndLine, newGCode)];
        }

        public override string ToString()
        {
            return $"Layer {LayerIndex} - ZPos: {LayerZPos} - Height: {LayerHeight} -HaveOverhang: {HaveOverhang}";
        }
    }
}