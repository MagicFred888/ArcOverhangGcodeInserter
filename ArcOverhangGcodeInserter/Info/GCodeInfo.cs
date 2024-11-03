using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Info;

public class GCodeInfo(int originalGCodeLineNbr, string gCodeCommand, bool isOverhang)
{
    public int OriginalGCodeLineNbr { get; private set; } = originalGCodeLineNbr;

    public string GCodeCommand { get; private set; } = gCodeCommand;

    public bool IsOverhang { get; private set; } = isOverhang;

    public GraphicsPath? GraphicsPath { get; private set; }

    public void SetGraphicsPath(GraphicsPath graphicsPath)
    {
        GraphicsPath = graphicsPath;
    }

    public override string ToString()
    {
        return $"{OriginalGCodeLineNbr}: {GCodeCommand} (IsOverhang={IsOverhang})";
    }
}