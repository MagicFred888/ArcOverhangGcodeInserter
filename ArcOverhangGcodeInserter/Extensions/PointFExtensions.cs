namespace ArcOverhangGcodeInserter.Extensions
{
    public static class PointFExtensions
    {
        public static PointF ScaleUp(this PointF point)
        {
            return new PointF(point.X * Constants.InternalCalculationScaleFactor, point.Y * Constants.InternalCalculationScaleFactor);
        }

        public static PointF ScaleDown(this PointF point)
        {
            return new PointF(point.X / Constants.InternalCalculationScaleFactor, point.Y / Constants.InternalCalculationScaleFactor);
        }

        public static float Distance(this PointF point1, PointF point2)
        {
            return (float)Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2));
        }
    }
}