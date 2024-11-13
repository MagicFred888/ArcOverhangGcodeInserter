namespace ArcOverhangGcodeInserter
{
    partial class UfBase
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btLoadGcode = new Button();
            cbSampleFiles = new ComboBox();
            pbLayerImage = new PictureBox();
            tbLayer = new TrackBar();
            laLayerInfo = new Label();
            lvLayers = new ListView();
            layerID = new ColumnHeader();
            overhangStatus = new ColumnHeader();
            BtExportGCode = new Button();
            ((System.ComponentModel.ISupportInitialize)pbLayerImage).BeginInit();
            ((System.ComponentModel.ISupportInitialize)tbLayer).BeginInit();
            SuspendLayout();
            // 
            // btLoadGcode
            // 
            btLoadGcode.Location = new Point(463, 12);
            btLoadGcode.Name = "btLoadGcode";
            btLoadGcode.Size = new Size(110, 23);
            btLoadGcode.TabIndex = 0;
            btLoadGcode.Text = "Load G-Code";
            btLoadGcode.UseVisualStyleBackColor = true;
            btLoadGcode.Click += BtLoadGcode_Click;
            // 
            // cbSampleFiles
            // 
            cbSampleFiles.FormattingEnabled = true;
            cbSampleFiles.Items.AddRange(new object[] { "C:\\Users\\frede\\OneDrive\\Desktop\\Sample.gcode.3mf", "C:\\Users\\frede\\OneDrive\\Desktop\\SimpleOverhang.gcode.3mf", "C:\\Users\\frede\\OneDrive\\Desktop\\Test3.gcode.3mf", "C:\\Users\\frede\\OneDrive\\Desktop\\Overhang_Test.gcode.3mf", "C:\\Users\\frede\\OneDrive\\Desktop\\Test plate.gcode.3mf" });
            cbSampleFiles.Location = new Point(12, 13);
            cbSampleFiles.Name = "cbSampleFiles";
            cbSampleFiles.Size = new Size(445, 23);
            cbSampleFiles.TabIndex = 2;
            // 
            // pbLayerImage
            // 
            pbLayerImage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pbLayerImage.Location = new Point(225, 42);
            pbLayerImage.Name = "pbLayerImage";
            pbLayerImage.Size = new Size(873, 479);
            pbLayerImage.SizeMode = PictureBoxSizeMode.Zoom;
            pbLayerImage.TabIndex = 3;
            pbLayerImage.TabStop = false;
            // 
            // tbLayer
            // 
            tbLayer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tbLayer.Location = new Point(12, 527);
            tbLayer.Maximum = 1;
            tbLayer.Minimum = 1;
            tbLayer.Name = "tbLayer";
            tbLayer.Size = new Size(1086, 45);
            tbLayer.TabIndex = 4;
            tbLayer.Value = 1;
            tbLayer.ValueChanged += TbLayer_ValueChanged;
            // 
            // laLayerInfo
            // 
            laLayerInfo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            laLayerInfo.AutoSize = true;
            laLayerInfo.Location = new Point(12, 560);
            laLayerInfo.Name = "laLayerInfo";
            laLayerInfo.Size = new Size(59, 15);
            laLayerInfo.TabIndex = 5;
            laLayerInfo.Text = "Layer ? / ?";
            // 
            // lvLayers
            // 
            lvLayers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            lvLayers.Columns.AddRange(new ColumnHeader[] { layerID, overhangStatus });
            lvLayers.FullRowSelect = true;
            lvLayers.GridLines = true;
            lvLayers.Location = new Point(12, 42);
            lvLayers.MultiSelect = false;
            lvLayers.Name = "lvLayers";
            lvLayers.Size = new Size(207, 479);
            lvLayers.TabIndex = 6;
            lvLayers.UseCompatibleStateImageBehavior = false;
            lvLayers.View = View.Details;
            lvLayers.SelectedIndexChanged += LvLayers_SelectedIndexChanged;
            // 
            // layerID
            // 
            layerID.Text = "ID";
            // 
            // overhangStatus
            // 
            overhangStatus.Text = "With overhang";
            overhangStatus.TextAlign = HorizontalAlignment.Center;
            overhangStatus.Width = 120;
            // 
            // BtExportGCode
            // 
            BtExportGCode.Location = new Point(579, 12);
            BtExportGCode.Name = "BtExportGCode";
            BtExportGCode.Size = new Size(110, 23);
            BtExportGCode.TabIndex = 7;
            BtExportGCode.Text = "Export G-Code";
            BtExportGCode.UseVisualStyleBackColor = true;
            BtExportGCode.Click += BtExportGCode_Click;
            // 
            // UfBase
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1110, 584);
            Controls.Add(BtExportGCode);
            Controls.Add(lvLayers);
            Controls.Add(laLayerInfo);
            Controls.Add(tbLayer);
            Controls.Add(pbLayerImage);
            Controls.Add(cbSampleFiles);
            Controls.Add(btLoadGcode);
            Name = "UfBase";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ArcOverhangGcodeInserter";
            ((System.ComponentModel.ISupportInitialize)pbLayerImage).EndInit();
            ((System.ComponentModel.ISupportInitialize)tbLayer).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btLoadGcode;
        private ComboBox cbSampleFiles;
        private PictureBox pbLayerImage;
        private TrackBar tbLayer;
        private Label laLayerInfo;
        private ListView lvLayers;
        private ColumnHeader layerID;
        private ColumnHeader overhangStatus;
        private Button BtExportGCode;
    }
}
