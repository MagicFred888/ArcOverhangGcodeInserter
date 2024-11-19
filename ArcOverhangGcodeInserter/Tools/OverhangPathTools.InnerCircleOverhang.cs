using ArcOverhangGcodeInserter.Extensions;
using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools
{
    public partial class OverhangPathTools
    {
        private List<PathInfo> ComputeInnerCircleOverhang()
        {
            // Get center
            _center = GetCenterPointFromWalls();
            if (_center == PointF.Empty)
            {
                _center = GetCenterPointFromStartArea();
            }
            if (_center == PointF.Empty)
            {
                throw new InvalidOperationException("Unable to find InnerCircleOverhang center point");
            }

            // Compute circles parameters
            if (!ComputeInnerOverhangStartStopStep())
            {
                throw new InvalidOperationException("Unable to find InnerCircleOverhang start, stop and step radius");
            }

            // Get arcs
            List<List<GeometryAndPrintInfo>> allGAPI = GetArcsGeometryInfo();
            if (allGAPI.Count == 0)
            {
                throw new InvalidOperationException("Unable to calculate InnerCircleOverhang arcs");
            }

            // Group arcs into paths
            _maxDistanceToChain = 2f;
            return LinkGeometryAndPrintInfoAsPathInfo(allGAPI, false);
        }

        private bool ComputeInnerOverhangStartStopStep()
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
                if (float.IsNaN(_stopRadius))
                {
                    circleReg.Intersect(_overhang);
                    if (!circleReg.IsEmpty(_gra))
                    {
                        // We have target
                        _absRadiusChangeStep *= 10;
                        radius -= _absRadiusChangeStep / 3f;
                        _stopRadius = radius;
                    }
                }
                else
                {
                    Region testRegion = _overhang.Clone();
                    testRegion.Exclude(circleReg);
                    if (testRegion.IsEmpty(_gra))
                    {
                        // We have start
                        _startRadius = radius;
                        break;
                    }
                }
            } while (true);

            // Done
            return true;
        }

        public List<List<GeometryAndPrintInfo>> GetArcsGeometryInfo()
        {
            // For result
            List<List<GeometryAndPrintInfo>> result = [];
            bool stop;
            float radius = _startRadius;
            do
            {
                List<GeometryAndPrintInfo> tmpData = GetArcsAtRadius(radius);
                if (tmpData.Count > 0)
                {
                    result.Add(tmpData);
                }

                // Next radius
                radius += _startRadius < _stopRadius ? _absRadiusChangeStep : -_absRadiusChangeStep;
                stop = _startRadius < _stopRadius ? radius > _stopRadius + (nozzleDiameter.ScaleUp() / 4) : radius < _stopRadius - (nozzleDiameter.ScaleUp() / 4);
            } while (!stop);
            return result;
        }

        private List<GeometryAndPrintInfo> GetArcsAtRadius(float radius)
        {
            float angleStep = 360f / ((float)Math.PI * 2f * radius / 0.01f.ScaleUp());
            float startScanAngle = 0;
            PointF testPos = _center.GetPoint(radius, startScanAngle);

            // Search a point out of the overhang area
            while (_overhang.IsVisible(testPos) && Math.Abs(startScanAngle) < 360)
            {
                startScanAngle += angleStep;
                testPos = _center.GetPoint(radius, startScanAngle);
            }

            // Full circle ?
            if (Math.Abs(startScanAngle) >= 360)
            {
                GeometryAndPrintInfo fullCircle = new(
                    0,
                    360,
                    _center.ScaleDown(),
                    radius.ScaleDown(),
                    ArcDirection.CounterClockwise);
                return [fullCircle];
            }

            // Perform a 360° scan from current angle to find all arc segment within the overhang area
            float startAngle = float.NaN;
            List<GeometryAndPrintInfo> result = [];
            for (float angle = startScanAngle; angle <= startScanAngle + 360f + angleStep; angle += angleStep)
            {
                if (float.IsNaN(startAngle) && _overhang.IsVisible(_center.GetPoint(radius, angle)))
                {
                    // Start new arc
                    startAngle = angle;
                    continue;
                }
                if (!float.IsNaN(startAngle) && !_overhang.IsVisible(_center.GetPoint(radius, angle)))
                {
                    // End arc
                    float stopAngle = angle - angleStep;
                    float arcLength = (stopAngle - startAngle) * radius * (float)Math.PI / 180;
                    if (arcLength >= (1.5f * nozzleDiameter).ScaleUp())
                    {
                        // We make sure it make sense to start an extrusion
                        GeometryAndPrintInfo arc = new(
                            startAngle,
                            stopAngle,
                            _center.ScaleDown(),
                            radius.ScaleDown(),
                            ArcDirection.CounterClockwise);
                        result.Add(arc);
                    }
                    startAngle = float.NaN;
                }
            }

            // Done
            return result;
        }
    }
}