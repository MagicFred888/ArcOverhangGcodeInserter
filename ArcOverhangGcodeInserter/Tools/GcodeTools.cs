using ArcOverhangGcodeInserter.Info;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace ArcOverhangGcodeInserter.Tools
{
    public static partial class GCodeTools
    {
        [GeneratedRegex("^G[123] X(?<X>[-0-9\\.]+) Y(?<Y>[-0-9\\.]+) .*$")]
        private static partial Regex XYExtractRegex();

        [GeneratedRegex("^G[123] X(?<X>[-0-9\\.]+) Y(?<Y>[-0-9\\.]+) I(?<I>[-0-9\\.]+) J(?<J>[-0-9\\.]+).*$")]
        private static partial Regex XYIJExtractRegex();

        public static GraphicsPath ConvertGcodeIntoGraphicsPath(List<WallInfo> allOuterWallInfo)
        {
            GraphicsPath newPath = new();
            for (int wallId = 0; wallId < allOuterWallInfo.Count; wallId++)
            {
                Match tmpMatch = XYExtractRegex().Match(allOuterWallInfo[wallId].WallGCodeContent[0].GCodeCommand);
                PointF startPos = new(float.Parse(tmpMatch.Groups["X"].Value), float.Parse(tmpMatch.Groups["Y"].Value));
                PointF endPos;
                for (int gCodeId = 1; gCodeId < allOuterWallInfo[wallId].WallGCodeContent.Count; gCodeId++)
                {
                    GraphicsPath currentGCodeGraphicsPath = new();
                    string tmpGCode = allOuterWallInfo[wallId].WallGCodeContent[gCodeId].GCodeCommand;
                    switch (tmpGCode[..2])
                    {
                        case "G1":
                            // Line
                            tmpMatch = XYExtractRegex().Match(tmpGCode);
                            endPos = new(float.Parse(tmpMatch.Groups["X"].Value), float.Parse(tmpMatch.Groups["Y"].Value));
                            newPath.AddLine(startPos, endPos);
                            currentGCodeGraphicsPath.AddLine(startPos, endPos);
                            break;

                        case "G2":
                        case "G3":
                            // Arc
                            tmpMatch = XYIJExtractRegex().Match(tmpGCode);
                            endPos = new(float.Parse(tmpMatch.Groups["X"].Value), float.Parse(tmpMatch.Groups["Y"].Value));
                            PointF ijPos = new(float.Parse(tmpMatch.Groups["I"].Value), float.Parse(tmpMatch.Groups["J"].Value));
                            (RectangleF arcRect, float startAngle, float sweepAngle) = ComputeArcParameters(startPos, endPos, ijPos, tmpGCode[..2] == "G2");
                            newPath.AddArc(arcRect, startAngle, sweepAngle);
                            currentGCodeGraphicsPath.AddArc(arcRect, startAngle, sweepAngle);
                            break;

                        default:
                            throw new InvalidDataException($"NOT SUPPORTED GCODE :{tmpGCode}");
                    }

                    // Save graphics to current GCode
                    allOuterWallInfo[wallId].WallGCodeContent[gCodeId].SetGraphicsPath(currentGCodeGraphicsPath);

                    // Switch point
                    startPos = endPos;
                }

                // Close path
                newPath.CloseFigure();
            }

            // Close path
            return newPath;
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
    }
}