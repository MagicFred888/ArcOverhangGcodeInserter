using ArcOverhangGcodeInserter.Tools;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;

namespace ArcOverhangGcodeInserter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void BtLoadGcode_Click(object sender, EventArgs e)
        {
            // Read file
            string[] fileContent = File.ReadAllLines(tbGcodeFilePath.Text, Encoding.UTF8);

            // Split G-code per layer
            Dictionary<int, List<string>> gCodeLayers = ExtractingTools.GetCodePerLayer([.. fileContent]);

            // Extract outer walls
            for (int layerId = 0; layerId < gCodeLayers.Count; layerId++)
            {
                List<List<string>> outerWallGcode = ExtractingTools.ExtractOuterLayerGcode(gCodeLayers.Values.ToList()[layerId]);

                GraphicsPath outerWallPaths = GcodeTools.ConvertGcodeIntoGraphicsPath(outerWallGcode);

                using Bitmap aaaa = new Bitmap(256, 256);
                using Graphics gra = Graphics.FromImage(aaaa);
                gra.Clear(Color.White);
                gra.DrawPath(new Pen(Color.Black), outerWallPaths);
                aaaa.Save($@"C:\Users\frede\OneDrive\Desktop\Layers\Result{layerId + 1}.bmp", ImageFormat.Png);
            }
        }
    }
}