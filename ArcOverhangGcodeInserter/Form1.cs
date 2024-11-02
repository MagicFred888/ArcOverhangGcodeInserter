using ArcOverhangGcodeInserter.Tools;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;

namespace ArcOverhangGcodeInserter
{
    public partial class Form1 : Form
    {
        private const string destinationFolder = @"C:\Users\frede\Downloads\Layers";

        public Form1()
        {
            InitializeComponent();
            cbSampleFiles.SelectedIndex = 0;
        }

        private void BtLoadGcode_Click(object sender, EventArgs e)
        {
            // Erase all previous data
            Directory.Delete(destinationFolder, true);
            Directory.CreateDirectory(destinationFolder);

            // Read file
            string[] fileContent = File.ReadAllLines(cbSampleFiles.Text, Encoding.UTF8);

            // Split G-code per layer
            Dictionary<int, List<string>> gCodeLayers = ExtractingTools.GetCodePerLayer([.. fileContent]);

            // Extract outer walls
            for (int layerId = 1; layerId < gCodeLayers.Count; layerId++) // Layer ID start at 1 to match the one in Bambu Studio
            {
                // To help on debug
                Debug.Print($"Start scan of layer {layerId}");

                // From each layer G-Code, get a GraphicsPath object
                List<List<string>> outerWallGcode = ExtractingTools.ExtractOuterLayerGcode(gCodeLayers.Values.ToList()[layerId - 1]);
                GraphicsPath outerWallPaths = GcodeTools.ConvertGcodeIntoGraphicsPath(outerWallGcode);

                // Save GraphicsPath for debug and review purpose
                using Bitmap layerImage = new(256, 256);
                using Graphics gra = Graphics.FromImage(layerImage);
                gra.Clear(Color.White);
                gra.DrawPath(new Pen(Color.Black), outerWallPaths);
                layerImage.Save(Path.Combine(destinationFolder, $@"Layer{layerId:00}.png"), ImageFormat.Png);

                // To help on debug
                Debug.Print($"End of layer {layerId}");
            }

            // Done
            MessageBox.Show("Operation completed successfully !", "Information !", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}