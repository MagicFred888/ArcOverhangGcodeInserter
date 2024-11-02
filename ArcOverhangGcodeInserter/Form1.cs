using ArcOverhangGcodeInserter.Tools;
using System.Drawing.Imaging;

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
            // Split G-code per layer
            List<LayerInfos> allLayers = ExtractingTools.ExtractAllLayerInfosFromGCode(cbSampleFiles.Text);

            // Draw images
            Directory.Delete(destinationFolder, true);
            Directory.CreateDirectory(destinationFolder);
            LayerImageTools layerImageTools = new(allLayers);
            foreach (LayerInfos layerInfo in allLayers)
            {
                using Image layerImage = layerImageTools.GetImageFromLayerGraphicsPath(layerInfo.LayerGraphicsPath);
                layerImage.Save(Path.Combine(destinationFolder, $@"Layer{layerInfo.LayerIndex:00}.png"), ImageFormat.Png);
            }

            // Done
            MessageBox.Show("Operation completed successfully !", "Information !", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}