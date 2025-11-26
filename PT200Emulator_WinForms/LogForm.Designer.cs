namespace PT200EmulatorWinForms
{
    partial class LogForm
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
            logTextBox = new TextBox();
            SuspendLayout();
            // 
            // logTextBox
            // 
            logTextBox.Dock = DockStyle.Fill;
            logTextBox.Location = new Point(0, 0);
            logTextBox.Multiline = true;
            logTextBox.Name = "logTextBox";
            logTextBox.ScrollBars = ScrollBars.Both;
            logTextBox.Size = new Size(800, 450);
            logTextBox.TabIndex = 0;
            // 
            // LogForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(logTextBox);
            Name = "LogForm";
            Text = "LogForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        public TextBox logTextBox;
    }
}