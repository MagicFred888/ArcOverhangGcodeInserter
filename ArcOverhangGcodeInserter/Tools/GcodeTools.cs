using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace ArcOverhangGcodeInserter.Tools
{
    public static partial class GcodeTools
    {
        public static GraphicsPath ConvertGcodeIntoGraphicsPath(List<List<string>> allOuterWallsGcode)
        {
            GraphicsPath newPath = new();
            for (int wallId = 0; wallId < allOuterWallsGcode.Count; wallId++)
            {
                Debug.Print($"Start scan of wallId={wallId}");
                List<string> wallGcode = allOuterWallsGcode[wallId];
                Match tmpMatch = XYExtractRegex().Match(wallGcode[0]);
                PointF startPos = new(float.Parse(tmpMatch.Groups["X"].Value), float.Parse(tmpMatch.Groups["Y"].Value));
                PointF endPos;
                for (int gCodeId = 1; gCodeId < wallGcode.Count; gCodeId++)
                {
                    Debug.Print($"Start scan of gCodeId=={gCodeId} in wallId=={wallId}");
                    switch (wallGcode[gCodeId][..2])
                    {
                        case "G1":
                            // Line
                            tmpMatch = XYExtractRegex().Match(wallGcode[gCodeId]);
                            endPos = new(float.Parse(tmpMatch.Groups["X"].Value), float.Parse(tmpMatch.Groups["Y"].Value));
                            newPath.AddLine(startPos, endPos);
                            break;

                        case "G2":
                        case "G3":
                            // Arc
                            tmpMatch = XYIJExtractRegex().Match(wallGcode[gCodeId]);
                            endPos = new(float.Parse(tmpMatch.Groups["X"].Value), float.Parse(tmpMatch.Groups["Y"].Value));
                            PointF ijPos = new(float.Parse(tmpMatch.Groups["I"].Value), float.Parse(tmpMatch.Groups["J"].Value));
                            AddArcToPath(newPath, startPos, endPos, ijPos, wallGcode[gCodeId][..2] != "G2");
                            break;

                        default:
                            throw new InvalidDataException($"NOT SUPPORTED GCODE :{wallGcode[gCodeId]}");
                    }

                    // Switch point
                    startPos = endPos;
                    Debug.Print($"End of scan of gCodeID=={gCodeId} in wallId=={wallId}");
                }

                // Close path
                newPath.CloseFigure();
                Debug.Print($"End of scan of wallId=={wallId}");
            }

            // Close path
            return newPath;
        }

        private static void AddArcToPath(GraphicsPath path, PointF start, PointF end, PointF ij, bool clockwise)
        {
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

            // Add the arc to the path
            RectangleF arcRect = new(center.X - radius, center.Y - radius, radius * 2, radius * 2);
            path.AddArc(arcRect, startAngle, sweepAngle);
        }

        private static float Distance(PointF p1, PointF p2)
        {
            return (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        private static float Angle(PointF center, PointF point)
        {
            return (float)(Math.Atan2(point.Y - center.Y, point.X - center.X) * (180.0 / Math.PI));
        }

        [GeneratedRegex("^G[123] X(?<X>[-0-9\\.]+) Y(?<Y>[-0-9\\.]+) .*$")]
        private static partial Regex XYExtractRegex();

        [GeneratedRegex("^G[123] X(?<X>[-0-9\\.]+) Y(?<Y>[-0-9\\.]+) I(?<I>[-0-9\\.]+) J(?<J>[-0-9\\.]+).*$")]
        private static partial Regex XYIJExtractRegex();
    }
}