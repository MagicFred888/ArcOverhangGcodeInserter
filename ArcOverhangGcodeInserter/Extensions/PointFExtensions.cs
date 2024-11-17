namespace ArcOverhangGcodeInserter.Extensions
{
    public static class PointFExtensions
    {
        /// <summary>
        /// Scales up the point by the internal calculation scale factor.
        /// </summary>
        /// <param name="point">The point to scale up.</param>
        /// <returns>A new PointF that is scaled up.</returns>
        public static PointF ScaleUp(this PointF point)
        {
            return new PointF(point.X * Constants.InternalCalculationScaleFactor, point.Y * Constants.InternalCalculationScaleFactor);
        }

        /// <summary>
        /// Scales down the point by the internal calculation scale factor.
        /// </summary>
        /// <param name="point">The point to scale down.</param>
        /// <returns>A new PointF that is scaled down.</returns>
        public static PointF ScaleDown(this PointF point)
        {
            return new PointF(point.X / Constants.InternalCalculationScaleFactor, point.Y / Constants.InternalCalculationScaleFactor);
        }

        /// <summary>
        /// Calculates the distance between current point and another one.
        /// </summary>
        /// <param name="point">The point we are looking at.</param>
        /// <returns>The distance between the two points.</returns>
        public static float Distance(this PointF refPoint, PointF point)
        {
            return (float)Math.Sqrt(Math.Pow(point.X - refPoint.X, 2) + Math.Pow(point.Y - refPoint.Y, 2));
        }

        /// <summary>
        /// Gets a point at a specified radius and angle from current center point.
        /// </summary>
        /// <param name="radius">The radius from the center point.</param>
        /// <param name="angleDeg">The angle in degrees from the center point.</param>
        /// <returns>A new PointF at the specified radius and angle from the center point.</returns>
        public static PointF GetPoint(this PointF center, float radius, float angleDeg)
        {
            float x = center.X + (float)Math.Cos(angleDeg * Math.PI / 180) * radius;
            float y = center.Y + (float)Math.Sin(angleDeg * Math.PI / 180) * radius;
            return new(x, y);
        }
    }
}