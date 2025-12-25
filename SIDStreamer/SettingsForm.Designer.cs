namespace SIDStream
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
                DisposeManagedResources();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.skinLabel = new Label();
            this.skinComboBox = new ComboBox();
            SuspendLayout();
            // 
            // skinLabel
            // 
            this.skinLabel.AutoSize = true;
            this.skinLabel.Location = new Point(79, 41);
            this.skinLabel.Name = "skinLabel";
            this.skinLabel.Size = new Size(78, 32);
            this.skinLabel.TabIndex = 0;
            this.skinLabel.Text = "label1";
            // 
            // skinComboBox
            // 
            skinComboBox.FormattingEnabled = true;
            skinComboBox.Location = new Point(209, 38);
            skinComboBox.Name = "skinComboBox";
            skinComboBox.Size = new Size(242, 40);
            skinComboBox.TabIndex = 1;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(skinComboBox);
            Controls.Add(this.skinLabel);
            Name = "SettingsForm";
            Text = "SettingsForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label skinLabel;
        private ComboBox skinComboBox;
    }
}