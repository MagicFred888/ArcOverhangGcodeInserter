using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Info;

public enum PathType
{
    Unknown = 0,
    OuterWall = 1,
    InnerWall = 2,
    OverhangArea = 3,
}

public class PathInfo(PathType pathType)
{
    private GraphicsPath? _fullPath = null;

    public PathType Type { get; set; } = pathType;

    public PointF StartPosition => AllSegments.Count > 0 ? AllSegments[0].SegmentGeometryInfo.StartPosition : PointF.Empty;

    public PointF EndPosition => AllSegments.Count > 0 ? AllSegments[^1].SegmentGeometryInfo.EndPosition : PointF.Empty;

    public int FullGCodeStartLine { get; set; } = -1;

    public int FullGCodeEndLine { get; set; } = -1;

    public int NbrOfSegments => AllSegments.Count;

    public List<SegmentInfo> AllSegments { get; private set; } = [];

    public void AddSegmentInfo(SegmentInfo newSegment)
    {
        AllSegments.Add(newSegment);
        _fullPath = null;
    }

    public void InsertSegmentInfo(int position, SegmentInfo newSegment)
    {
        AllSegments.Insert(position, newSegment);
        _fullPath = null;
    }

    public GraphicsPath FullPath
    {
        get
        {
            if (_fullPath == null)
            {
                _fullPath = new GraphicsPath();
                foreach (GraphicsPath segmentPath in AllSegments.Select(x => x.GraphicsPath))
                {
                    _fullPath.AddPath(segmentPath, true);
                }
            }
            return _fullPath;
        }
    }

    public override string ToString()
    {
        return $"{Type} - NbrOfSegments: {NbrOfSegments}";
    }
}