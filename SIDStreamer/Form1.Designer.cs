namespace SIDStreamer
{
    partial class SIDstreamer
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
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                // Dispose logo bitmaps created in the other partial class.
                // Fields are declared in Form1.cs and are accessible here because this is the same partial class.
                try
                {
                    logoOriginal?.Dispose();
                    logoScaled?.Dispose();
                }
                catch
                {
                    // swallow any disposal errors to avoid breaking designer-generated cleanup
                }

                DisposeManagedResources();
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
            openFileDialog1 = new OpenFileDialog();
            label1 = new Label();
            label2 = new Label();
            labelInfo = new Label();
            noFocusTrackBar1 = new SIDStreamer.Controls.NoFocusTrackBar();
            label3 = new Label();
            ((System.ComponentModel.ISupportInitialize)noFocusTrackBar1).BeginInit();
            SuspendLayout();
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.Black;
            label1.ForeColor = Color.DarkGray;
            label1.Location = new Point(12, 268);
            label1.Name = "label1";
            label1.Size = new Size(495, 32);
            label1.TabIndex = 2;
            label1.Text = "Merry Christmas 2025 - Retro And Gaming ©";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.Black;
            label2.ForeColor = Color.White;
            label2.Location = new Point(222, 213);
            label2.Name = "label2";
            label2.Size = new Size(78, 32);
            label2.TabIndex = 3;
            label2.Text = "label2";
            // 
            // labelInfo
            // 
            labelInfo.AutoSize = true;
            labelInfo.BackColor = Color.Black;
            labelInfo.ForeColor = Color.Lime;
            labelInfo.Location = new Point(95, 213);
            labelInfo.Name = "labelInfo";
            labelInfo.Size = new Size(78, 32);
            labelInfo.TabIndex = 4;
            labelInfo.Text = "label3";
            // 
            // noFocusTrackBar1
            // 
            noFocusTrackBar1.BackColor = Color.Black;
            noFocusTrackBar1.LargeChange = 1;
            noFocusTrackBar1.Location = new Point(148, 321);
            noFocusTrackBar1.Name = "noFocusTrackBar1";
            noFocusTrackBar1.Size = new Size(208, 90);
            noFocusTrackBar1.TabIndex = 6;
            noFocusTrackBar1.TabStop = false;
            noFocusTrackBar1.Value = 5;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = Color.Black;
            label3.ForeColor = Color.White;
            label3.Location = new Point(331, 213);
            label3.Name = "label3";
            label3.Size = new Size(89, 32);
            label3.TabIndex = 7;
            label3.Text = "00 / 00";
            // 
            // SIDstreamer
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(589, 517);
            Controls.Add(label3);
            Controls.Add(noFocusTrackBar1);
            Controls.Add(labelInfo);
            Controls.Add(label2);
            Controls.Add(label1);
            Name = "SIDstreamer";
            Text = "SIDstreamer";
            ((System.ComponentModel.ISupportInitialize)noFocusTrackBar1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private OpenFileDialog openFileDialog1;
        private Label label1;
        private Label label2;
        private Label labelInfo;
        private Controls.NoFocusTrackBar noFocusTrackBar1;
        private Label label3;
    }
}
