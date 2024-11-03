using ArcOverhangGcodeInserter.Info;

namespace ArcOverhangGcodeInserter
{
    public partial class UfBase : Form
    {
        private ThreeDimensionalPrintInfo? _3DPrint = null;

        public UfBase()
        {
            InitializeComponent();
            cbSampleFiles.SelectedIndex = 0;
        }

        private void BtLoadGcode_Click(object sender, EventArgs e)
        {
            // Analyze G-Code file
            this.Enabled = false;
            _3DPrint = new ThreeDimensionalPrintInfo(cbSampleFiles.Text);

            // Update TrackBar
            tbLayer.Value = tbLayer.Minimum;
            tbLayer.Maximum = _3DPrint.NbrOfLayers;
            TbLayer_ValueChanged(new(), new());
            this.Enabled = true;
        }

        private void TbLayer_ValueChanged(object sender, EventArgs e)
        {
            pbLayerImage.Image?.Dispose();
            if (_3DPrint != null)
            {
                pbLayerImage.Image = _3DPrint.GetLayerImage(tbLayer.Value, false);
                laLayerInfo.Text = $"Layer {tbLayer.Value} / {tbLayer.Maximum}";
            }
        }
    }
}