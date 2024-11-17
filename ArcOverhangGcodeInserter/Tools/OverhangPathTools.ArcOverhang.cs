using ArcOverhangGcodeInserter.Extensions;
using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools
{
    public partial class OverhangPathTools
    {
        private List<PathInfo> ComputeArcOverhang()
        {
            // Get center
            _center = GetCenterPointFromStartArea();
            if (_center == PointF.Empty)
            {
                throw new InvalidOperationException("Unable to find ArcOverhang center point");
            }

            // Compute circles parameters
            if (!ComputeOuterOverhangStartStopStep())
            {
                throw new InvalidOperationException("Unable to find ArcOverhang start, stop and step radius");
            }

            // Get arcs
            List<List<GeometryAndPrintInfo>> allGAPI = GetArcsGeometryInfo();
            if (allGAPI.Count == 0)
            {
                throw new InvalidOperationException("Unable to calculate OuterCircleOverhang arcs");
            }

            // Group arcs into paths
            return LinkGeometryAndPrintInfoAsPathInfo(allGAPI, true, 1.2f);
        }

        private bool ComputeOuterOverhangStartStopStep()
        {
            // Reset data
            _startRadius = float.NaN;
            _stopRadius = float.NaN;
            _absRadiusChangeStep = (nozzleDiameter * (1f - Constants.ArcIntersection)).ScaleUp() / 10f;

            // Search start radius
            float radius = (_absRadiusChangeStep / 2f) - _absRadiusChangeStep;
            do
            {
                // Update radius
                radius += _absRadiusChangeStep;
                if (radius.ScaleDown() > 256)
                {
                    return true;
                }

                // Create reference circle
                GraphicsPath circle = new();
                circle.AddEllipse(_center.X - radius, _center.Y - radius, 2 * radius, 2 * radius);
                Region circleReg = new(circle);

                // Check state
                if (float.IsNaN(_startRadius))
                {
                    circleReg.Intersect(_overhang);
                    if (!circleReg.IsEmpty(_gra))
                    {
                        // We have target
                        _absRadiusChangeStep *= 10;
                        _startRadius = radius;
                    }
                }
                else
                {
                    Region testRegion = _overhang.Clone();
                    testRegion.Exclude(circleReg);
                    if (testRegion.IsEmpty(_gra))
                    {
                        // We have start
                        _stopRadius = radius;
                        break;
                    }
                }
            } while (true);

            // Done
            return true;
        }

        public List<List<GeometryAndPrintInfo>> GetArcsGeometryInfo(PointF center)
        {
            // For result
            List<List<GeometryAndPrintInfo>> result = [];

            // Create list of arcs by increasing radius step by step
            float radiusIncreaseStep = (nozzleDiameter * (1f - Constants.ArcIntersection)).ScaleUp();
            float radius = (0.25f * nozzleDiameter).ScaleUp() - radiusIncreaseStep;
            float startAngle;
            int nbrOfFailure = 0;
            bool needOneMoreRun;
            do
            {
                // Increase radius, compute angle step to have point on circle moving 0.01mm and reset
                List<GeometryAndPrintInfo> currentRadiusArcs = [];
                needOneMoreRun = false;
                radius += radiusIncreaseStep;
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
                        new PointF(center.X + radius, center.Y + 0.01f).ScaleDown(),
                        new PointF(center.X + radius, center.Y - 0.01f).ScaleDown(),
                        center.ScaleDown(),
                        radius.ScaleDown(),
                        ArcDirection.CounterClockwise);
                    currentRadiusArcs.Add(fullCircle);
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
            } while (needOneMoreRun && nbrOfFailure < 100);

            return result;
        }
    }
}