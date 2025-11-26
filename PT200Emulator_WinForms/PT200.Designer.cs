using PT200EmulatorWinForms.Engine;
using System.Windows.Forms;

namespace PT200EmulatorWinForms
{
    partial class PT200
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
            StatusLine = new StatusStrip();
            messageLabel = new ToolStripStatusLabel();
            logLabel = new ToolStripStatusLabel();
            onlineLabel = new ToolStripStatusLabel();
            dsrLabel = new ToolStripStatusLabel();
            systemLabel = new ToolStripStatusLabel();
            g0g1Label = new ToolStripStatusLabel();
            insertLabel = new ToolStripStatusLabel();
            scrollLockLabel = new ToolStripStatusLabel();
            capsLockLabel = new ToolStripStatusLabel();
            numLockLabel = new ToolStripStatusLabel();
            clockLabel = new ToolStripStatusLabel();
            layoutPanel = new TableLayoutPanel();
            StatusLine.SuspendLayout();
            layoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // StatusLine
            // 
            StatusLine.AutoSize = false;
            StatusLine.BackColor = Color.LimeGreen;
            StatusLine.Font = new Font("Consolas", 10F);
            StatusLine.Items.AddRange(new ToolStripItem[] { messageLabel, logLabel, onlineLabel, dsrLabel, systemLabel, g0g1Label, insertLabel, scrollLockLabel, capsLockLabel, numLockLabel, clockLabel });
            StatusLine.Location = new Point(0, 327);
            StatusLine.Name = "StatusLine";
            StatusLine.Size = new Size(800, 22);
            StatusLine.TabIndex = 1;
            // 
            // messageLabel
            // 
            messageLabel.Name = "messageLabel";
            messageLabel.Size = new Size(305, 17);
            messageLabel.Spring = true;
            messageLabel.Text = "Ready...";
            messageLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // logLabel
            // 
            logLabel.AutoSize = false;
            logLabel.Name = "logLabel";
            logLabel.Size = new Size(100, 17);
            // 
            // onlineLabel
            // 
            onlineLabel.AutoSize = false;
            onlineLabel.Name = "onlineLabel";
            onlineLabel.Size = new Size(68, 17);
            onlineLabel.Text = "OFFLINE";
            // 
            // dsrLabel
            // 
            dsrLabel.AutoSize = false;
            dsrLabel.Name = "dsrLabel";
            dsrLabel.Size = new Size(36, 17);
            dsrLabel.Text = "DSR";
            // 
            // systemLabel
            // 
            systemLabel.AutoSize = false;
            systemLabel.Name = "systemLabel";
            systemLabel.Size = new Size(30, 17);
            systemLabel.Text = "RDY";
            // 
            // g0g1Label
            // 
            g0g1Label.AutoSize = false;
            g0g1Label.Name = "g0g1Label";
            g0g1Label.Size = new Size(28, 17);
            g0g1Label.Text = "G0";
            // 
            // insertLabel
            // 
            insertLabel.AutoSize = false;
            insertLabel.Name = "insertLabel";
            insertLabel.Size = new Size(36, 17);
            insertLabel.Text = "INS";
            // 
            // scrollLockLabel
            // 
            scrollLockLabel.AutoSize = false;
            scrollLockLabel.Name = "scrollLockLabel";
            scrollLockLabel.Size = new Size(44, 17);
            scrollLockLabel.Text = "SCRL";
            // 
            // capsLockLabel
            // 
            capsLockLabel.AutoSize = false;
            capsLockLabel.Name = "capsLockLabel";
            capsLockLabel.Size = new Size(44, 17);
            capsLockLabel.Text = "CAPS";
            // 
            // numLockLabel
            // 
            numLockLabel.AutoSize = false;
            numLockLabel.Name = "numLockLabel";
            numLockLabel.Size = new Size(36, 17);
            numLockLabel.Text = "NUM";
            // 
            // clockLabel
            // 
            clockLabel.AutoSize = false;
            clockLabel.Name = "clockLabel";
            clockLabel.Size = new Size(58, 17);
            clockLabel.Text = "00:00";
            clockLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // layoutPanel
            // 
            layoutPanel.BackColor = Color.Black;
            layoutPanel.ColumnCount = 1;
            layoutPanel.ColumnStyles.Add(new ColumnStyle());
            layoutPanel.Controls.Add(StatusLine, 1, 1);
            layoutPanel.Dock = DockStyle.Fill;
            layoutPanel.Location = new Point(0, 0);
            layoutPanel.Margin = new Padding(0);
            layoutPanel.Name = "layoutPanel";
            layoutPanel.RowCount = 2;
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            layoutPanel.Size = new Size(543, 349);
            layoutPanel.TabIndex = 3;
            // 
            // PT200
            // 
            AutoScaleMode = AutoScaleMode.None;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(543, 349);
            Controls.Add(layoutPanel);
            Name = "PT200";
            Text = "PT200";
            StatusLine.ResumeLayout(false);
            StatusLine.PerformLayout();
            layoutPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        internal StatusStrip StatusLine;
        internal ToolStripStatusLabel messageLabel;
        internal ToolStripStatusLabel onlineLabel;
        internal ToolStripStatusLabel dsrLabel;
        internal ToolStripStatusLabel systemLabel;
        internal ToolStripStatusLabel g0g1Label;
        internal ToolStripStatusLabel scrollLockLabel;
        internal ToolStripStatusLabel insertLabel;
        internal ToolStripStatusLabel capsLockLabel;
        internal ToolStripStatusLabel numLockLabel;
        internal ToolStripStatusLabel clockLabel;
        internal ToolStripStatusLabel logLabel;
        internal TableLayoutPanel layoutPanel;
    }
}
