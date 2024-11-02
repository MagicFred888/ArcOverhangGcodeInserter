namespace ArcOverhangGcodeInserter
{
    partial class Form1
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
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(cbSampleFiles);
            Controls.Add(btLoadGcode);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Button btLoadGcode;
        private ComboBox cbSampleFiles;
    }
}
