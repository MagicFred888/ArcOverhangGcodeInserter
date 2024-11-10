using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace ArcOverhangGcodeInserter.Tools
{
    public partial class GCodeTools(float layerHeight, float nozzleDiameter, float filamentDiameter)
    {
        [GeneratedRegex("^G[123] X(?<X>[-0-9\\.]+) Y(?<Y>[-0-9\\.]+)")]
        private static partial Regex XYExtractRegex();

        [GeneratedRegex("^G[123] X(?<X>[-0-9\\.]+) Y(?<Y>[-0-9\\.]+) I(?<I>[-0-9\\.]+) J(?<J>[-0-9\\.]+).*$")]
        private static partial Regex XYIJExtractRegex();

        public List<string> GetFullGCodeSequence(List<PathInfo> newOverhangArcs)
        {
            string moveHeadUp = $"G1 Z{layerHeight + 0.4:0.##} E-0.05";
            string moveHeadDown = $"G1 Z{layerHeight:0.##}";

            string setFanFullSpeed = "M106 S255";

            string setOverhangSpeed = "G1 F300";
            string setNormalSpeed = "G1 F10000";

            // Start block
            List<string> result = [];
            result.Add("; FEATURE: Start of overhang sequence");

            // Set fan speed
            result.Add(setFanFullSpeed);

            // All move
            foreach (PathInfo path in newOverhangArcs)
            {
                // Move sequence
                result.Add(setNormalSpeed);
                result.Add(moveHeadUp);
                result.Add($"G1 X{path.StartPosition.X:0.###} Y{path.StartPosition.Y:0.###} E-0.7"); //TODO: Fix start point
                result.Add(moveHeadDown);
                result.Add("G1 E0.75");
                result.Add(setOverhangSpeed);

                // Add each move
                foreach (SegmentGeometryInfo sgi in path.AllSegments.Select(s => s.SegmentGeometryInfo))
                {
                    // Add segment
                    string gCode = GetGCodeFromSegmentGeometryInfo(sgi);
                    result.Add(gCode);
                }
            }

            // End sequence
            result.Add("; End of overhang sequence");
            return result;
        }

        public string GetGCodeFromSegmentGeometryInfo(SegmentGeometryInfo sgi)
        {
            return sgi.Type switch
            {
                SegmentType.Line => GetGCodeLine(sgi.StartPosition, sgi.EndPosition),
                SegmentType.ClockwiseArc => GetGCodeArc(sgi.StartPosition, sgi.EndPosition, sgi.CenterPosition, true),
                SegmentType.CounterClockwiseArc => GetGCodeArc(sgi.StartPosition, sgi.EndPosition, sgi.CenterPosition, false),
                _ => throw new InvalidDataException($"NOT SUPPORTED SEGMENT TYPE :{sgi.Type}"),
            };
        }

        public string GetGCodeLine(PointF startPoint, PointF endPoint)
        {
            float eParam = CalculateLineE(startPoint, endPoint);
            return $"G1 X{endPoint.X:0.#####} Y{endPoint.Y:0.#####} E{eParam}";
        }

        private float CalculateLineE(PointF startPoint, PointF endPoint)
        {
            // Calculate the filament cross-sectional area
            double filamentRadius = filamentDiameter / 2.0;
            double filamentArea = Math.PI * Math.Pow(filamentRadius, 2);

            // Calculate the layer cross-sectional area
            double layerArea = layerHeight * nozzleDiameter;

            // Calculate the movement distance
            double distance = Math.Sqrt(Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2));

            // Calculate the extrusion amount (E)
            float extrusion = (float)((layerArea * distance) / filamentArea);

            // Done
            return extrusion;
        }

        public string GetGCodeArc(PointF startPoint, PointF endPoint, PointF centerPoint, bool clockwise)
        {
            // Compute the length of the arc
            float eParam = CalculateArcE(startPoint, endPoint, centerPoint, clockwise);
            return $"G{(clockwise ? 2 : 3)} X{endPoint.X:0.#####} Y{endPoint.Y:0.#####} I{centerPoint.X - startPoint.X:0.#####} J{centerPoint.Y - startPoint.Y:0.#####} E{eParam}";
        }

        private float CalculateArcE(PointF startPoint, PointF endPoint, PointF centerPoint, bool isClockwise)
        {
            // Calculate the filament cross-sectional area
            double filamentRadius = filamentDiameter / 2.0;
            double filamentArea = Math.PI * Math.Pow(filamentRadius, 2);

            // Calculate the layer cross-sectional area
            double layerArea = layerHeight * nozzleDiameter;

            // Calculate the radius of the arc
            double radius = Math.Sqrt(Math.Pow(startPoint.X - centerPoint.X, 2) + Math.Pow(startPoint.Y - centerPoint.Y, 2));

            // Calculate the start and end angles
            double startAngle = Math.Atan2(startPoint.Y - centerPoint.Y, startPoint.X - centerPoint.X);
            double endAngle = Math.Atan2(endPoint.Y - centerPoint.Y, endPoint.X - centerPoint.X);

            // Normalize angles to ensure proper calculation of the arc length
            if (isClockwise)
            {
                if (endAngle > startAngle) endAngle -= 2 * Math.PI;
            }
            else
            {
                if (endAngle < startAngle) endAngle += 2 * Math.PI;
            }

            // Calculate the arc length
            double arcLength = Math.Abs(radius * (endAngle - startAngle));

            // Calculate the extrusion amount (E)
            float extrusion = (float)((layerArea / filamentArea) * arcLength);

            // Done
            return extrusion;
        }

        public static SegmentGeometryInfo GetSegmentGeometryInfoFromGCode(PointF startPosition, string gCodeCommand)
        {
            switch (gCodeCommand[..2])
            {
                case "G1":
                    // Line
                    PointF lineEndPosition = GetXYFromGCode(gCodeCommand);
                    return new SegmentGeometryInfo(startPosition, lineEndPosition);

                case "G2":
                case "G3":
                    // Arc
                    PointF circleEndPosition = GetXYFromGCode(gCodeCommand);
                    PointF ijValues = GetIJFromGCode(gCodeCommand);
                    PointF circleCenterPosition = new(startPosition.X + ijValues.X, startPosition.Y + ijValues.Y);
                    float radius = Distance(circleCenterPosition, startPosition);
                    return new(startPosition, circleEndPosition, circleCenterPosition, radius, gCodeCommand[..2] == "G2" ? ArcDirection.Clockwise : ArcDirection.CounterClockwise);

                default:
                    throw new InvalidDataException($"NOT SUPPORTED GCODE :{gCodeCommand}");
            }
        }

        public static GraphicsPath GetGraphicsPathFromSegmentGeometryInfo(SegmentGeometryInfo sgi)
        {
            switch (sgi.Type)
            {
                case SegmentType.Line:
                    // Line
                    GraphicsPath linePath = new();
                    linePath.AddLine(sgi.StartPosition, sgi.EndPosition);
                    return linePath;

                case SegmentType.ClockwiseArc:
                case SegmentType.CounterClockwiseArc:
                    // Arc
                    GraphicsPath arcPath = new();
                    (RectangleF arcRect, float startAngle, float sweepAngle) = ComputeArcParameters(sgi.StartPosition, sgi.EndPosition, sgi.CenterPosition, sgi.Type == SegmentType.ClockwiseArc);
                    arcPath.AddArc(arcRect, startAngle, sweepAngle);
                    return arcPath;

                default:
                    throw new InvalidDataException($"NOT SUPPORTED SEGMENT TYPE :{sgi.Type}");
            }
        }

        public static PointF GetXYFromGCode(string gCode)
        {
            Match tmpMatch = XYExtractRegex().Match(gCode);
            return new(float.Parse(tmpMatch.Groups["X"].Value), float.Parse(tmpMatch.Groups["Y"].Value));
        }

        public static PointF GetIJFromGCode(string gCode)
        {
            Match tmpMatch = XYIJExtractRegex().Match(gCode);
            return new(float.Parse(tmpMatch.Groups["I"].Value), float.Parse(tmpMatch.Groups["J"].Value));
        }

        private static (RectangleF arcRect, float startAngle, float sweepAngle) ComputeArcParameters(PointF start, PointF end, PointF center, bool clockwise)
        {
            // Invert clockwise because graphics path have Y axis pointing down will g-code assum pointing up
            clockwise = !clockwise;

            // Calculate the radius
            float radius = Distance(center, start);

            // Calculate start angle
            float startAngle = Angle(center, start);

            // Calculate end angle
            float endAngle = Angle(center, end);

            // Calculate sweep angle
            float sweepAngle = clockwise ? (endAngle - startAngle) : (startAngle - endAngle);

            // Adjust the sweep angle for proper direction and range
            if (sweepAngle < 0)
                sweepAngle += 360;
            if (!clockwise)
                sweepAngle = -sweepAngle;

            // Compute arc rectangle
            RectangleF arcRect = new(center.X - radius, center.Y - radius, radius * 2, radius * 2);

            // Done
            return (arcRect, startAngle, sweepAngle);
        }

        private static float Distance(PointF p1, PointF p2)
        {
            return (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        private static float Angle(PointF center, PointF point)
        {
            return (float)(Math.Atan2(point.Y - center.Y, point.X - center.X) * (180.0 / Math.PI));
        }
    }
}