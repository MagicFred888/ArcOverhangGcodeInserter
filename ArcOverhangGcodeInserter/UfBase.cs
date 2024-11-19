using ArcOverhangGcodeInserter.Info;
using System.Reflection;

namespace ArcOverhangGcodeInserter
{
    public partial class UfBase : Form
    {
        private ThreeDimensionalPrintInfo? _3DPrint = null;
        private readonly string _sampleDataFolder = string.Empty;

        public UfBase()
        {
            InitializeComponent();

            // Load sample files starting where the exe is located
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            while (!Directory.Exists(Path.Combine(exePath, "TestData")))
            {
                exePath = Path.GetDirectoryName(exePath) ?? "";
            }
            if (Directory.Exists(exePath))
            {
                _sampleDataFolder = Path.Combine(exePath, "TestData");
                List<string> sampleFiles = [.. Directory.GetFiles(_sampleDataFolder, "*.gcode.3mf")];
                sampleFiles = sampleFiles.ConvertAll(i => Path.GetFileName(i));
                sampleFiles.Sort();
                cbSampleFiles.Items.AddRange([.. sampleFiles.ConvertAll(i => (object)i)]);
                if (cbSampleFiles.Items.Count > 0)
                {
                    cbSampleFiles.SelectedIndex = 4;
                    BtLoadGcode_Click(new(), new());
                }
            }
        }

        private void BtLoadGcode_Click(object sender, EventArgs e)
        {
            // Get source file
            string sourceFile = cbSampleFiles.Text;
            if (!File.Exists(sourceFile))
            {
                sourceFile = Path.Combine(_sampleDataFolder, cbSampleFiles.Text);
            }
            if (!File.Exists(sourceFile))
            {
                MessageBox.Show("Unable to find the requested source file", "Information !", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Analyze G-Code file
            this.Enabled = false;
            _3DPrint = new ThreeDimensionalPrintInfo(sourceFile);

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
                newItem.SubItems.Add(layer.OverhangRegions.Count > 0 ? "Yes" : "No");
                newItem.BackColor = layer.OverhangRegions.Count > 0 ? Color.LightGreen : Color.LightCoral;
                if (selectFirstLayerWithOverhang && layer.OverhangRegions.Count > 0)
                {
                    selectFirstLayerWithOverhang = false;
                    newItem.Selected = true;
                    newItem.EnsureVisible();
                }
            }
        }

        private void BtExportGCode_Click(object sender, EventArgs e)
        {
            if (_3DPrint == null)
            {
                return;
            }
            this.Enabled = false;
            _3DPrint.ExportGCode(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            this.Enabled = true;
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