using System.Diagnostics;
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

            // Split per layer
            Dictionary<int, List<string>> layerGcode = ExtractingTools.GetCodePerLayer([.. fileContent]);

            // Extract outer wall G-Code per layer
            foreach (KeyValuePair<int, List<string>> tmpPair in layerGcode)
            {
                List<List<string>> outerWallGcode = ExtractingTools.ExtractOuterLayerGcode(tmpPair.Value);

                Debug.Print(outerWallGcode.ToString());
            }
        }
    }
}