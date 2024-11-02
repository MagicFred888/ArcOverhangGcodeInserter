using ArcOverhangGcodeInserter.Tools;

namespace ArcOverhangGcodeInserter
{
    public partial class UfBase : Form
    {
        //private const string destinationFolder = @"C:\Users\frede\Downloads\Layers";
        private List<LayerInfos> allLayers = [];

        private LayerImageTools? layerImageTools = null;

        public UfBase()
        {
            InitializeComponent();
            cbSampleFiles.SelectedIndex = 0;
        }

        private void BtLoadGcode_Click(object sender, EventArgs e)
        {
            // Split G-code per layer
            allLayers = ExtractingTools.ExtractAllLayerInfosFromGCode(cbSampleFiles.Text);

            // Load Image tool
            layerImageTools = new(allLayers);

            // Update TrackBar
            tbLayer.Value = tbLayer.Minimum;
            tbLayer.Maximum = allLayers.Count;
            TbLayer_ValueChanged(new(), new());

            // Draw images
            //Directory.Delete(destinationFolder, true);
            //Directory.CreateDirectory(destinationFolder);
            //foreach (LayerInfos layerInfo in allLayers)
            //{
            //    using Image layerImage = layerImageTools.GetImageFromLayerGraphicsPath(layerInfo.LayerGraphicsPath);
            //    layerImage.Save(Path.Combine(destinationFolder, $@"Layer{layerInfo.LayerIndex:00}.png"), ImageFormat.Png);
            //}

            // Done
            MessageBox.Show("Operation completed successfully !", "Information !", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void TbLayer_ValueChanged(object sender, EventArgs e)
        {
            pbLayerImage.Image?.Dispose();
            if (layerImageTools != null)
            {
                pbLayerImage.Image = layerImageTools.GetImageFromLayerGraphicsPath(allLayers[tbLayer.Value - 1].LayerGraphicsPath);
                laLayerInfo.Text = $"Layer {tbLayer.Value} / {allLayers.Count}";
            }
        }
    }
}