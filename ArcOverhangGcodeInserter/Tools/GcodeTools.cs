using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace ArcOverhangGcodeInserter.Tools
{
    public static partial class GCodeTools
    {
        [GeneratedRegex("^G[123] X(?<X>[-0-9\\.]+) Y(?<Y>[-0-9\\.]+)")]
        private static partial Regex XYExtractRegex();

        [GeneratedRegex("^G[123] X(?<X>[-0-9\\.]+) Y(?<Y>[-0-9\\.]+) I(?<I>[-0-9\\.]+) J(?<J>[-0-9\\.]+).*$")]
        private static partial Regex XYIJExtractRegex();

        public static string GetGCodeLine(PointF startPoint, PointF endPoint)
        {
            float eParam = CalculateLineE(1.75f, 0.2f, 0.4f, startPoint, endPoint);
            return $"G1 X{endPoint.X:0.#####} Y{endPoint.Y:0.#####} E{eParam}";
        }

        public static string GetGCodeArc(PointF startPoint, PointF endPoint, PointF centerPoint, bool clockwise)
        {
            // Compute the length of the arc
            float eParam = CalculateArcE(1.75f, 0.2f, 0.4f, startPoint, endPoint, centerPoint, clockwise);
            return $"G{(clockwise ? 2 : 3)} X{endPoint.X:0.#####} Y{endPoint.Y:0.#####} I{centerPoint.X - startPoint.X:0.#####} J{centerPoint.Y - startPoint.Y:0.#####} E{eParam}";
        }

        public static GraphicsPath ConvertGcodeIntoGraphicsPath(PointF startPoint, string gCode)
        {
            // For result
            GraphicsPath result = new();

            // single GCode line
            PointF endPoint;
            switch (gCode[..2])
            {
                case "G1":
                    // Line
                    endPoint = GetXYFromGCode(gCode);
                    result.AddLine(startPoint, endPoint);
                    break;

                case "G2":
                case "G3":
                    // Arc
                    endPoint = GetXYFromGCode(gCode);
                    PointF ijPos = GetIJFromGCode(gCode);
                    (RectangleF arcRect, float startAngle, float sweepAngle) = ComputeArcParameters(startPoint, endPoint, ijPos, gCode[..2] == "G2");
                    result.AddArc(arcRect, startAngle, sweepAngle);
                    break;

                default:
                    throw new InvalidDataException($"NOT SUPPORTED GCODE :{gCode}");
            }

            // Close path
            return result;
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

        private static float CalculateArcE(double filamentDiameter, double layerHeight, double extrusionWidth, PointF startPoint, PointF endPoint, PointF centerPoint, bool isClockwise)
        {
            // Calculate the filament cross-sectional area
            double filamentRadius = filamentDiameter / 2.0;
            double filamentArea = Math.PI * Math.Pow(filamentRadius, 2);

            // Calculate the layer cross-sectional area
            double layerArea = layerHeight * extrusionWidth;

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

        private static float CalculateLineE(double filamentDiameter, double layerHeight, double extrusionWidth, PointF startPoint, PointF endPoint)
        {
            // Calculate the filament cross-sectional area
            double filamentRadius = filamentDiameter / 2.0;
            double filamentArea = Math.PI * Math.Pow(filamentRadius, 2);

            // Calculate the layer cross-sectional area
            double layerArea = layerHeight * extrusionWidth;

            // Calculate the movement distance
            double distance = Math.Sqrt(Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2));

            // Calculate the extrusion amount (E)
            float extrusion = (float)((layerArea * distance) / filamentArea);

            // Done
            return extrusion;
        }

        private static (RectangleF arcRect, float startAngle, float sweepAngle) ComputeArcParameters(PointF start, PointF end, PointF ij, bool clockwise)
        {
            // Invert clockwise because graphics path have Y axis pointing down will g-code assum pointing up
            clockwise = !clockwise;

            // Calculate center of the arc
            PointF center = new(start.X + ij.X, start.Y + ij.Y);

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

        internal static List<string> GetFullGCodeSequence(List<Info.PathInfo> newOverhangArcsWalls, float layerHeight)
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
            foreach (Info.PathInfo path in newOverhangArcsWalls)
            {
                // Move sequence
                result.Add(setNormalSpeed);
                result.Add(moveHeadUp);
                result.Add($"G1 X{path.EndPoint.X:0.###} Y{path.EndPoint.Y:0.###} E-0.7");
                result.Add(moveHeadDown);
                result.Add("G1 E0.75");
                result.Add(setOverhangSpeed);

                // Add each move
                foreach (Info.SegmentInfo segment in path.AllSegments)
                {
                    // Add segment
                    result.Add(segment.GCodeCommand);
                }
            }

            // End sequence
            result.Add("; End of overhang sequence");
            return result;
        }
    }
}