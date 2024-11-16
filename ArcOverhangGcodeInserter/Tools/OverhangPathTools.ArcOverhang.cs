using ArcOverhangGcodeInserter.Extensions;
using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;

namespace ArcOverhangGcodeInserter.Tools
{
    public partial class OverhangPathTools
    {
        private List<PathInfo> ComputeArcOverhang(Region startOverhang, Region overhang)
        {
            // Get center
            PointF center = GetArcsCenter(startOverhang);
            if (center == PointF.Empty)
            {
                throw new InvalidOperationException("Unable to find ArcOverhang center point");
            }

            // Get arcs
            List<List<GeometryAndPrintInfo>> allGAPI = GetArcsGeometryInfo(overhang, center);

            // Group arcs into paths
            return GetArcsPathInfo(allGAPI, true, 1.2f);
        }

        public static PointF GetArcsCenter(Region overhangStartRegion)
        {
            // Extract all points from start region
            List<PointF> startPoints = [];
            foreach (RectangleF rect in overhangStartRegion.GetRegionScans(new Matrix()))
            {
                // extract each point
                startPoints.Add(new PointF(rect.Left, rect.Top));
                startPoints.Add(new PointF(rect.Left, rect.Bottom));
                startPoints.Add(new PointF(rect.Right, rect.Top));
                startPoints.Add(new PointF(rect.Right, rect.Bottom));
            }
            if (startPoints.Count == 0)
            {
                return PointF.Empty;
            }

            // Compute center of the arc x
            float xCenter = (startPoints.Min(pt => pt.X) + startPoints.Max(pt => pt.X)) / 2;

            // Compute center of the arc y
            bool centerFound = false;
            float yMin = float.NaN;
            float yMax = float.NaN;
            for (float y = startPoints.Min(pt => pt.Y); y < startPoints.Max(pt => pt.Y); y++)
            {
                // Check if point xCenter, y is inside the overhang region
                if (float.IsNaN(yMin) && overhangStartRegion.IsVisible(xCenter, y))
                {
                    // Compute the arc
                    yMin = y;
                    yMax = y;
                }
                if (!float.IsNaN(yMax) && overhangStartRegion.IsVisible(xCenter, y))
                {
                    yMax = y;
                    centerFound = true;
                }
            }

            // Check if not found
            if (!centerFound || float.IsNaN(yMin) || float.IsNaN(yMax))
            {
                // We make the start point in the center of the scaledOverhangStartRegion
                float yCenter = (startPoints.Min(pt => pt.Y) + startPoints.Max(pt => pt.Y)) / 2;
                return new PointF(xCenter, yCenter);
            }

            // Done
            return new(xCenter, (yMin + yMax) / 2f);
        }

        public List<List<GeometryAndPrintInfo>> GetArcsGeometryInfo(Region overhangRegion, PointF center)
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
                PointF testPos = GetPoint(center, radius, startScanAngle);

                // Search a point out of the overhang area
                while (overhangRegion.IsVisible(testPos) && startScanAngle < 360)
                {
                    startScanAngle += angleStep;
                    testPos = GetPoint(center, radius, startScanAngle);
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
                    if (float.IsNaN(startAngle) && overhangRegion.IsVisible(GetPoint(center, radius, angle)))
                    {
                        // Start new arc
                        startAngle = angle;
                        continue;
                    }
                    if (!float.IsNaN(startAngle) && !overhangRegion.IsVisible(GetPoint(center, radius, angle)))
                    {
                        // End arc
                        float stopAngle = angle - angleStep;
                        float arcLength = (stopAngle - startAngle) * radius * (float)Math.PI / 180;
                        if (arcLength >= (1.5f * nozzleDiameter).ScaleUp())
                        {
                            // We make sure it make sense to start an extrusion
                            GeometryAndPrintInfo arc = new(
                                GetPoint(center, radius, startAngle).ScaleDown(),
                                GetPoint(center, radius, stopAngle).ScaleDown(),
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