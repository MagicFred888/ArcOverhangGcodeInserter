using ArcOverhangGcodeInserter.Extensions;
using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace ArcOverhangGcodeInserter.Tools
{
    public partial class GCodeTools(float layZPos, float layHeight, float nozzleDiameter, float filamentDiameter)
    {
        [GeneratedRegex("^G[123] X(?<X>[-0-9\\.]+) Y(?<Y>[-0-9\\.]+)")]
        private static partial Regex XYExtractRegex();

        [GeneratedRegex("^G[123] X(?<X>[-0-9\\.]+) Y(?<Y>[-0-9\\.]+) I(?<I>[-0-9\\.]+) J(?<J>[-0-9\\.]+).*$")]
        private static partial Regex XYIJExtractRegex();

        public List<string> GetFullGCodeSequence(List<PathInfo> newOverhangArcs)
        {
            // For float check
            const float tolerance = 0.0001f;

            // Up and down movement
            string moveHeadUp = $"G1 Z{layZPos + 0.4:0.##} E-0.05";
            string moveHeadDown = $"G1 Z{layZPos:0.##}";

            // Move speed
            string setNormalSpeed = "G1 F10000";

            // Start block
            List<string> result = [];
            result.Add("; FEATURE: Start of overhang sequence");

            // All move
            int coolingFanSpeedInPercent = -1;
            float printSpeedInMmPerSecond = -1;
            foreach (PathInfo path in newOverhangArcs)
            {
                // Move sequence
                result.Add(setNormalSpeed);
                printSpeedInMmPerSecond = (int)Math.Round(10000f / 60f, 0); // TODO: Make function to set speed and edit this variable
                result.Add(moveHeadUp);
                result.Add($"G1 X{path.StartPosition.X:0.###} Y{path.StartPosition.Y:0.###} E-0.7");
                result.Add(moveHeadDown);
                result.Add("G1 E0.75");

                // Add each move
                foreach (GeometryAndPrintInfo sgi in path.AllSegments.Select(s => s.SegmentGeometryInfo))
                {
                    // Update fan if necessary
                    if (sgi.CoolingFanSpeedInPercent != coolingFanSpeedInPercent)
                    {
                        coolingFanSpeedInPercent = sgi.CoolingFanSpeedInPercent;
                        result.Add($"M106 S{coolingFanSpeedInPercent}");
                    }

                    // Update print speed if necessary
                    if (Math.Abs(sgi.PrintSpeedInMmPerSecond - printSpeedInMmPerSecond) > tolerance)
                    {
                        printSpeedInMmPerSecond = sgi.PrintSpeedInMmPerSecond;
                        result.Add($"G1 F{(int)Math.Round(60 * printSpeedInMmPerSecond, 0)}");
                    }

                    string gCode = GetGCodeFromGeometryAndPrintInfo(sgi);
                    result.Add(gCode);
                }
            }

            // End sequence
            result.Add("; End of overhang sequence");
            return result;
        }

        public string GetGCodeFromGeometryAndPrintInfo(GeometryAndPrintInfo gapi)
        {
            return gapi.Type switch
            {
                SegmentType.Line => GetGCodeLine(gapi),
                SegmentType.ClockwiseArc => GetGCodeArc(gapi),
                SegmentType.CounterClockwiseArc => GetGCodeArc(gapi),
                _ => throw new InvalidDataException($"NOT SUPPORTED SEGMENT TYPE :{gapi.Type}"),
            };
        }

        public string GetGCodeLine(GeometryAndPrintInfo gapi)
        {
            float eParam = CalculateLineE(gapi.StartPosition, gapi.EndPosition) * gapi.ExtrusionMultiplier;
            return $"G1 X{gapi.EndPosition.X:0.#####} Y{gapi.EndPosition.Y:0.#####} E{eParam}";
        }

        private float CalculateLineE(PointF startPoint, PointF endPoint)
        {
            // Calculate the filament cross-sectional area
            double filamentRadius = filamentDiameter / 2.0;
            double filamentArea = Math.PI * Math.Pow(filamentRadius, 2);

            // Calculate the layer cross-sectional area
            double layerArea = layHeight * nozzleDiameter;

            // Calculate the movement distance
            double distance = startPoint.Distance(endPoint);

            // Calculate the extrusion amount (E)
            float extrusion = (float)((layerArea * distance) / filamentArea);

            // Done
            return extrusion;
        }

        public string GetGCodeArc(GeometryAndPrintInfo gapi)
        {
            // Compute the length of the arc
            bool clockwise = gapi.Type == SegmentType.ClockwiseArc;
            float eParam = CalculateArcE(gapi.StartPosition, gapi.EndPosition, gapi.CenterPosition, clockwise) * gapi.ExtrusionMultiplier;
            string gCommand = clockwise ? "G2" : "G3";
            return $"{gCommand} X{gapi.EndPosition.X:0.#####} Y{gapi.EndPosition.Y:0.#####} I{gapi.CenterPosition.X - gapi.StartPosition.X:0.#####} J{gapi.CenterPosition.Y - gapi.StartPosition.Y:0.#####} E{eParam}";
        }

        private float CalculateArcE(PointF startPoint, PointF endPoint, PointF centerPoint, bool isClockwise)
        {
            // Calculate the filament cross-sectional area
            double filamentRadius = filamentDiameter / 2.0;
            double filamentArea = Math.PI * Math.Pow(filamentRadius, 2);

            // Calculate the layer cross-sectional area
            double layerArea = layHeight * nozzleDiameter;

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

        public static GeometryAndPrintInfo GetSegmentGeometryInfoFromGCode(PointF startPosition, string gCodeCommand)
        {
            switch (gCodeCommand[..2])
            {
                case "G1":
                    // Line
                    PointF lineEndPosition = GetXYFromGCode(gCodeCommand);
                    return new GeometryAndPrintInfo(startPosition, lineEndPosition);

                case "G2":
                case "G3":
                    // Arc
                    PointF circleEndPosition = GetXYFromGCode(gCodeCommand);
                    PointF ijValues = GetIJFromGCode(gCodeCommand);
                    PointF circleCenterPosition = new(startPosition.X + ijValues.X, startPosition.Y + ijValues.Y);
                    float radius = circleCenterPosition.Distance(startPosition);
                    return new(startPosition, circleEndPosition, circleCenterPosition, radius, gCodeCommand[..2] == "G2" ? ArcDirection.Clockwise : ArcDirection.CounterClockwise);

                default:
                    throw new InvalidDataException($"NOT SUPPORTED GCODE :{gCodeCommand}");
            }
        }

        public static GraphicsPath GetGraphicsPathFromSegmentGeometryInfo(GeometryAndPrintInfo sgi)
        {
            switch (sgi.Type)
            {
                case SegmentType.Line:
                    // Line
                    GraphicsPath linePath = new();
                    linePath.AddLine(sgi.StartPosition.ScaleUp(), sgi.EndPosition.ScaleUp());
                    return linePath;

                case SegmentType.ClockwiseArc:
                case SegmentType.CounterClockwiseArc:
                    // Arc
                    GraphicsPath arcPath = new();
                    (RectangleF arcRect, float startAngle, float sweepAngle) = ComputeArcParameters(sgi.StartPosition.ScaleUp(), sgi.EndPosition.ScaleUp(), sgi.CenterPosition.ScaleUp(), sgi.Type == SegmentType.ClockwiseArc);
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
            float radius = center.Distance(start);

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

        public static float Angle(PointF center, PointF point)
        {
            return (float)(Math.Atan2(point.Y - center.Y, point.X - center.X) * (180.0 / Math.PI));
        }

        public static PointF GetCenterFromG1G2(string g)
        {
            // From G2 or G3 command, extract PointF of center
            PointF start = GetXYFromGCode(g);
            PointF centerOffset = GetIJFromGCode(g);
            return new(start.X + centerOffset.X, start.Y + centerOffset.Y);
        }
    }
}