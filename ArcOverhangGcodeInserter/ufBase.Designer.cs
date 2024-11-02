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
            cbSampleFiles.Items.AddRange(new object[] { "C:\\Users\\frede\\OneDrive\\Desktop\\Sample.gcode", "C:\\Users\\frede\\OneDrive\\Desktop\\SImple overhang.gcode" });
            cbSampleFiles.Location = new Point(12, 13);
            cbSampleFiles.Name = "cbSampleFiles";
            cbSampleFiles.Size = new Size(445, 23);
            cbSampleFiles.TabIndex = 2;
            // 
            // pbLayerImage
            // 
            pbLayerImage.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pbLayerImage.Location = new Point(12, 42);
            pbLayerImage.Name = "pbLayerImage";
            pbLayerImage.Size = new Size(851, 470);
            pbLayerImage.SizeMode = PictureBoxSizeMode.Zoom;
            pbLayerImage.TabIndex = 3;
            pbLayerImage.TabStop = false;
            // 
            // tbLayer
            // 
            tbLayer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tbLayer.Location = new Point(12, 518);
            tbLayer.Maximum = 1;
            tbLayer.Minimum = 1;
            tbLayer.Name = "tbLayer";
            tbLayer.Size = new Size(851, 45);
            tbLayer.TabIndex = 4;
            tbLayer.Value = 1;
            tbLayer.ValueChanged += TbLayer_ValueChanged;
            // 
            // laLayerInfo
            // 
            laLayerInfo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            laLayerInfo.AutoSize = true;
            laLayerInfo.Location = new Point(12, 551);
            laLayerInfo.Name = "laLayerInfo";
            laLayerInfo.Size = new Size(59, 15);
            laLayerInfo.TabIndex = 5;
            laLayerInfo.Text = "Layer ? / ?";
            // 
            // UfBase
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(875, 575);
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
    }
}
