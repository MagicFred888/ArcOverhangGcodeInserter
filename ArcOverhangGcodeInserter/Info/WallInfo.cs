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
}