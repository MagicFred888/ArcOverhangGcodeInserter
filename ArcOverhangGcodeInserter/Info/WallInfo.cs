using ArcOverhangGcodeInserter.Tools;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Info;

public class WallInfo
{
    public List<GCodeInfo> WallGCodeContent { get; private set; } = [];

    public int NbrOfGCodeInfo => WallGCodeContent.Count;

    public bool AddGCodeInfo(GCodeInfo newGCodeInfo)
    {
        if (WallGCodeContent.Find(w => w.OriginalGCodeLineNbr == newGCodeInfo.OriginalGCodeLineNbr) != null)
        {
            return false;
        }
        WallGCodeContent.Add(newGCodeInfo);
        return true;
    }

    private GraphicsPath? _wallBorderGraphicsPath;

    public GraphicsPath WallBorderGraphicsPath
    {
        get
        {
            if (_wallBorderGraphicsPath == null)
            {
                _wallBorderGraphicsPath = GraphicsPathTools.CreateGraphicsPathBorder(this);
            }
            return _wallBorderGraphicsPath;
        }
    }
}