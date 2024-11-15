namespace ArcOverhangGcodeInserter.Extensions
{
    public static class RegionExtensions
    {
        public static bool IntersectWith(this Region thisRegion, Region otherRegion)
        {
            using Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);
            using Region tmpRegion = thisRegion.Clone();
            tmpRegion.Intersect(otherRegion);
            return !tmpRegion.IsEmpty(graphics);
        }
    }
}