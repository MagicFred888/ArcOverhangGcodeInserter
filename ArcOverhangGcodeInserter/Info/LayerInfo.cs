using ArcOverhangGcodeInserter.Extensions;
using ArcOverhangGcodeInserter.Tools;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Info
{
    public partial class LayerInfo
    {
        private readonly ThreeDimensionalPrintInfo _parent;

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

        public LayerInfo(ThreeDimensionalPrintInfo parent, int layerIndex, List<string> layerGCode, List<PathInfo> paths, LayerInfo? previousLayer)
        {
            _parent = parent;
            LayerIndex = layerIndex;
            LayerGCode = layerGCode;
            _previousLayer = previousLayer;

            // Etxract layer information from G-Code
            LayerZPos = parent.ExtractionTools.GetLayerZPos(layerGCode);
            LayerHeight = parent.ExtractionTools.GetLayerHeight(layerGCode);

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
            OverhangRegionTools overhangRegionTools = new(_parent.NozzleDiameter);
            OverhangRegions = overhangRegionTools.ComputeOverhangRegion(_previousLayer, this);
            if (OverhangRegions.Count == 0)
            {
                // Remove if any due to infill overhang who do not interest us
                OverhangInfillAndWallsPaths.Clear();
                OuterWalls.ForEach(wall => wall.AllSegments.ForEach(segment => segment.SetOverhangState(false)));
                InnerWalls.ForEach(wall => wall.AllSegments.ForEach(segment => segment.SetOverhangState(false)));
                return;
            }

            // Compute arcs
            OverhangPathTools overhangPathTools = new(_parent.NozzleDiameter);

            List<PathInfo> possibleWall = [];
            possibleWall.AddRange(OverhangInfillAndWallsPaths);
            possibleWall.AddRange(OuterWalls);
            possibleWall.AddRange(InnerWalls);
            NewOverhangArcsWalls = overhangPathTools.ComputeNewOverhangPathInfo(OverhangRegions, possibleWall);
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

        public (List<(int start, int stop)> toRemove, List<string> gCodeToAdd) GetLayerGCodeChangeInfo()
        {
            if (OverhangInfillAndWallsPaths.Count == 0)
            {
                return new();
            }

            // Extract new GCode
            GCodeTools gCodeTools = new(LayerZPos, LayerHeight, _parent.NozzleDiameter, Constants.FilamentDiameter);
            List<string> newOverhangGCode = gCodeTools.GetFullGCodeSequence(NewOverhangArcsWalls);

            // Get list of GCode to remove
            List<PathInfo> allOverhangInFill = OverhangInfillAndWallsPaths.FindAll(o => o.Type == PathType.OverhangArea);
            List<(int start, int stop)> toRemove = [.. allOverhangInFill.Select(o => (o.FullGCodeStartLine, o.FullGCodeEndLine))];

            // Done
            return (toRemove, newOverhangGCode);
        }

        public override string ToString()
        {
            return $"Layer {LayerIndex} - ZPos: {LayerZPos} - Height: {LayerHeight} -HaveOverhang: {HaveOverhang}";
        }
    }
}