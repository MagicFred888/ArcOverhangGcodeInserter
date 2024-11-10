using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace ArcOverhangGcodeInserter.Tools
{
    public static class GCodeAnd3MfFileTools
    {
        private static readonly string _gCodeFileNameIn3mf = "plate_1.gcode";
        private static readonly string _gCodeMD5FileNameIn3mf = "plate_1.gcode.md5";

        public static List<string> GetFullGCodeFromFile(string sourceFilePath)
        {
            // Check if file exist
            if (!File.Exists(sourceFilePath))
            {
                throw new FileNotFoundException("The specified GCode file does not exist !", sourceFilePath);
            }

            // Read file and return result
            return Path.GetExtension(sourceFilePath).ToLower() switch
            {
                ".3mf" => GetGCodeFrom3mfFile(sourceFilePath),
                ".gcode" => [.. File.ReadAllLines(sourceFilePath)],
                _ => throw new InvalidDataException("The specified file is not a GCode or 3mf file"),
            };
        }

        public static bool SaveGCodeFile(string originalFilePath, string newFilePath, List<string> newGCode)
        {
            // Read file and return result
            bool returnValue = true;
            switch (Path.GetExtension(originalFilePath).ToLower())
            {
                case ".3mf":
                    returnValue = SaveGCodeInto3mfFile(originalFilePath, newFilePath, newGCode);
                    break;

                case ".gcode":
                    File.WriteAllLines(newFilePath, [.. newGCode], Encoding.UTF8);
                    break;

                default:
                    throw new InvalidDataException("The specified file is not a GCode or 3mf file");
            }
            return returnValue;
        }

        private static bool SaveGCodeInto3mfFile(string originalFilePath, string newFilePath, List<string> newGCode)
        {
            // Make it single line and compute MD5
            string newGCodeString = string.Join("\n", newGCode);
            string md5 = ComputeMd5Hash(newGCodeString);

            // Duplicate source path
            File.Copy(originalFilePath, newFilePath, true);

            // Open new file as a zip archive and write new GCode
            using ZipArchive archive = ZipFile.Open(newFilePath, ZipArchiveMode.Update);
            ZipArchiveEntry? zipEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(_gCodeFileNameIn3mf, StringComparison.OrdinalIgnoreCase)) ??
                throw new InvalidDataException("Unable to find plate_1.gcode file in the 3mf archive");
            using Stream zipStream = zipEntry.Open();
            using (StreamWriter writer = new(zipStream, new UTF8Encoding(false)))
            {
                writer.Write(newGCodeString);
                writer.Flush();
            }

            // Update MD5
            zipEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(_gCodeMD5FileNameIn3mf, StringComparison.OrdinalIgnoreCase)) ??
                throw new InvalidDataException($"Unable to find {_gCodeMD5FileNameIn3mf} file in the 3mf archive");
            using Stream md5Stream = zipEntry.Open();
            using (StreamWriter writer = new(md5Stream, new UTF8Encoding(false)))
            {
                writer.Write(md5);
                writer.Flush();
            }

            // Done
            return true;
        }

        private static List<string> GetGCodeFrom3mfFile(string sourceFilePath)
        {
            // Open 3mf file as a zip archive
            using ZipArchive archive = ZipFile.OpenRead(sourceFilePath);
            if (archive.Entries.Count == 0)
            {
                throw new InvalidDataException("The specified 3mf file is empty");
            }

            // Search plate_1.gcode in the archive (dirty, to be improve later)
            ZipArchiveEntry? zipEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(_gCodeFileNameIn3mf, StringComparison.OrdinalIgnoreCase)) ??
                throw new InvalidDataException($"Unable to find {_gCodeFileNameIn3mf} file in the 3mf archive");

            using Stream stream = zipEntry.Open();
            using StreamReader reader = new(stream, Encoding.UTF8);
            return [.. reader.ReadToEnd().Split("\n")];
        }

        private static string ComputeMd5Hash(string dataToHash)
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(dataToHash);
            byte[] hashBytes = MD5.HashData(inputBytes);
            StringBuilder sb = new();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}