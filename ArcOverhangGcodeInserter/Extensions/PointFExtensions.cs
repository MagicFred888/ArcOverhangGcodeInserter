namespace ArcOverhangGcodeInserter.Extensions
{
    public static class PointFExtensions
    {
        public static PointF Scale100(this PointF point)
        {
            return new PointF(point.X * 100, point.Y * 100);
        }

        public static float Distance(this PointF point1, PointF point2)
        {
            return (float)Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2));
        }
    }
}