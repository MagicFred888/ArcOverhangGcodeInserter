using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Info;

public class PathInfo()
{
    private GraphicsPath? _fullPath = null;

    public PointF StartPoint => AllSegments.Count > 0 ? AllSegments[^1].EndPoint : PointF.Empty;

    public PointF EndPoint => AllSegments.Count > 0 ? AllSegments[0].StartPoint : PointF.Empty;

    public int FullGCodeStartLine { get; set; } = -1;

    public int FullGCodeEndLine { get; set; } = -1;

    public int NbrOfSegments => AllSegments.Count;

    public List<SegmentInfo> AllSegments { get; private set; } = [];

    public void AddSegmentInfo(SegmentInfo newGCodeInfo)
    {
        AllSegments.Add(newGCodeInfo);
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
}