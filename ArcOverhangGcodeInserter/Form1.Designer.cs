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
            tbGcodeFilePath = new TextBox();
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
            // tbGcodeFilePath
            // 
            tbGcodeFilePath.Location = new Point(12, 12);
            tbGcodeFilePath.Name = "tbGcodeFilePath";
            tbGcodeFilePath.Size = new Size(445, 23);
            tbGcodeFilePath.TabIndex = 1;
            tbGcodeFilePath.Text = "C:\\Users\\frede\\OneDrive\\Desktop\\Body1.gcode";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(tbGcodeFilePath);
            Controls.Add(btLoadGcode);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btLoadGcode;
        private TextBox tbGcodeFilePath;
    }
}
