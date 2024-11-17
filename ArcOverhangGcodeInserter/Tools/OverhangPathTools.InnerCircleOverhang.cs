using ArcOverhangGcodeInserter.Extensions;
using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools
{
    public partial class OverhangPathTools
    {
        private List<PathInfo> ComputeInnerCircleOverhang()
        {
            PointF center = GetCenterPointFromWalls();
            if (center == PointF.Empty)
            {
                center = GetCenterPointFromStartArea();
            }

            // Get arcs
            List<List<GeometryAndPrintInfo>> allGAPI = GetInnerArcsGeometryInfo(center);

            // Group arcs into paths
            return LinkGeometryAndPrintInfoAsPathInfo(allGAPI, false, 3f);
        }

        public List<List<GeometryAndPrintInfo>> GetInnerArcsGeometryInfo(PointF center)
        {
            // For result
            List<List<GeometryAndPrintInfo>> result = [];

            // Search start radius
            float radius;
            for (radius = 0.5f; radius < 1000; radius += 0.5f * nozzleDiameter)
            {
                // Substract a circle of radius to the overhang region and check if something remains
                Region testRegion = new(_overhang.GetBounds(Graphics.FromHwnd(IntPtr.Zero)));
                GraphicsPath circlePath = new();
                circlePath.AddEllipse(center.X - radius, center.Y - radius, 2 * radius, 2 * radius);
                testRegion.Exclude(circlePath);
                if (testRegion.IsEmpty(Graphics.FromHwnd(IntPtr.Zero)))
                {
                    // We found the start radius
                    break;
                }
            }

            // Create list of arcs by increasing radius step by step
            float radiusDecreaseStep = -(nozzleDiameter * (1f - Constants.ArcIntersection)).ScaleUp();
            float startAngle;
            int nbrOfFailure = 0;
            bool needOneMoreRun;
            do
            {
                // Increase radius, compute angle step to have point on circle moving 0.01mm and reset
                List<GeometryAndPrintInfo> currentRadiusArcs = [];
                needOneMoreRun = false;
                radius += radiusDecreaseStep;
                float angleStep = 360f / ((float)Math.PI * 2f * radius / 0.01f.ScaleUp());
                float startScanAngle = 0;
                PointF testPos = center.GetPoint(radius, startScanAngle);

                // Search a point out of the overhang area
                while (_overhang.IsVisible(testPos) && startScanAngle < 360)
                {
                    startScanAngle += angleStep;
                    testPos = center.GetPoint(radius, startScanAngle);
                }

                // Full circle ?
                if (startScanAngle > 360)
                {
                    GeometryAndPrintInfo fullCircle = new(
                        0,
                        360 - 30 * angleStep,
                        center.ScaleDown(),
                        radius.ScaleDown(),
                        ArcDirection.CounterClockwise);
                    result.Add([fullCircle]);
                    needOneMoreRun = true;
                    continue;
                }

                // Perform a 360° scan from current angle to find all arc segment within the overhang area
                startAngle = float.NaN;
                for (float angle = startScanAngle; angle <= startScanAngle + 360f + angleStep; angle += angleStep)
                {
                    if (float.IsNaN(startAngle) && _overhang.IsVisible(center.GetPoint(radius, angle)))
                    {
                        // Start new arc
                        startAngle = angle;
                        continue;
                    }
                    if (!float.IsNaN(startAngle) && !_overhang.IsVisible(center.GetPoint(radius, angle)))
                    {
                        // End arc
                        float stopAngle = angle - angleStep;
                        float arcLength = (stopAngle - startAngle) * radius * (float)Math.PI / 180;
                        if (arcLength >= (1.5f * nozzleDiameter).ScaleUp())
                        {
                            // We make sure it make sense to start an extrusion
                            GeometryAndPrintInfo arc = new(
                                center.GetPoint(radius, startAngle).ScaleDown(),
                                center.GetPoint(radius, stopAngle).ScaleDown(),
                                center.ScaleDown(),
                                radius.ScaleDown(),
                                ArcDirection.CounterClockwise);
                            currentRadiusArcs.Add(arc);
                        }
                        needOneMoreRun = true;
                        startAngle = float.NaN;
                    }
                }

                // Full circle
                if (!float.IsNaN(startAngle))
                {
                    GeometryAndPrintInfo fullCircle = new(
                        new PointF(center.X + radius, center.Y).ScaleDown(),
                        new PointF(center.X + radius, center.Y).ScaleDown(),
                        center.ScaleDown(),
                        radius.ScaleDown(),
                        ArcDirection.CounterClockwise);
                    currentRadiusArcs.Add(fullCircle);
                    needOneMoreRun = true;
                }

                // Save current radius
                if (currentRadiusArcs.Count > 0)
                {
                    result.Add(currentRadiusArcs);
                    nbrOfFailure = 0;
                }
                else
                {
                    needOneMoreRun = true;
                    nbrOfFailure++;
                }
            } while (needOneMoreRun && nbrOfFailure < 100 && radius + radiusDecreaseStep > 0);

            return result;
        }
    }
}