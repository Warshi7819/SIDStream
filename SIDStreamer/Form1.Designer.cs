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
            SuspendLayout();
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // SIDstreamer
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(589, 517);
            Name = "SIDstreamer";
            Text = "SIDstreamer";
            ResumeLayout(false);
        }

        #endregion
        private OpenFileDialog openFileDialog1;
    }
}
