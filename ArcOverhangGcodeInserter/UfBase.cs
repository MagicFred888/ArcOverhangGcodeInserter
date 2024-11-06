using ArcOverhangGcodeInserter.Info;

namespace ArcOverhangGcodeInserter
{
    public partial class UfBase : Form
    {
        private ThreeDimensionalPrintInfo? _3DPrint = null;

        public UfBase()
        {
            InitializeComponent();
            cbSampleFiles.SelectedIndex = 1;
            BtLoadGcode_Click(new(), new());
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

            // Uplade Layer info list
            bool selectFirstLayerWithOverhang = true;
            lvLayers.Items.Clear();
            foreach (LayerInfo layer in _3DPrint.AllLayers)
            {
                ListViewItem newItem = lvLayers.Items.Add(layer.LayerIndex.ToString());
                newItem.Tag = layer.LayerIndex;
                newItem.UseItemStyleForSubItems = true;
                newItem.SubItems.Add(layer.OverhangRegion != null ? "Yes" : "No");
                newItem.BackColor = layer.OverhangRegion != null ? Color.LightGreen : Color.LightCoral;
                if (selectFirstLayerWithOverhang && layer.OverhangRegion != null)
                {
                    selectFirstLayerWithOverhang = false;
                    newItem.Selected = true;
                    newItem.EnsureVisible();
                }
            }
        }

        private void TbLayer_ValueChanged(object sender, EventArgs e)
        {
            pbLayerImage.Image?.Dispose();
            if (_3DPrint != null)
            {
                pbLayerImage.Image = _3DPrint.GetLayerImage(tbLayer.Value);
                laLayerInfo.Text = $"Layer {tbLayer.Value} / {tbLayer.Maximum}";
            }
        }

        private void LvLayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvLayers.SelectedItems.Count != 1 || lvLayers.SelectedItems[0].Tag == null)
            {
                return;
            }
            int layerNbr = int.TryParse(lvLayers.SelectedItems[0].Tag?.ToString(), out int result) ? result : -1;
            tbLayer.Value = layerNbr;
        }
    }
}