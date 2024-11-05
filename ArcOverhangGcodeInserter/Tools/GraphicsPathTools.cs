using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools
{
    public static class GraphicsPathTools
    {
        public static GraphicsPath CreateGraphicsPathBorder(GraphicsPath refGraphicsPath)
        {
            // Extract all points who are defining each line of the path in the wall
            List<PointF> pathPoints = [.. refGraphicsPath.PathPoints];
            pathPoints.Sort((p1, p2) => Math.Abs(p1.Y - p2.Y) < 0.0000001f ? p1.X.CompareTo(p2.X) : p1.Y.CompareTo(p2.Y));

            // Sort pathPoints by closest to 0

            // Create a new path by always adding the next point to the previous one
            GraphicsPath borderPath = new();
            PointF startPoint = pathPoints[0];
            PointF currentPoint = startPoint;
            PointF nextPoint;
            pathPoints.RemoveAt(0);

            while (pathPoints.Count > 0)
            {
                float minDistance = pathPoints.Min(p => Distance(currentPoint, p));
                nextPoint = pathPoints.First(p => Math.Abs(Distance(currentPoint, p) - minDistance) < 0.0000001f);
                pathPoints.Remove(nextPoint);
                borderPath.AddLine(currentPoint, nextPoint);
                currentPoint = nextPoint;
            }
            borderPath.AddLine(currentPoint, startPoint);

            return borderPath;
        }

        private static float Distance(PointF point1, PointF point2)
        {
            return (float)Math.Sqrt(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2));
        }
    }
}