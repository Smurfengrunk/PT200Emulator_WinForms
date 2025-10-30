using System.Windows.Forms;

namespace PT200Emulator_WinForms
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
            SidePanel = new Panel();
            PortTextBox = new TextBox();
            HostTextBox = new TextBox();
            LogLevelCombo = new ComboBox();
            ScreenFormatCombo = new ComboBox();
            rbPanel = new Panel();
            rbGreen = new RadioButton();
            rbColor = new RadioButton();
            rbBlue = new RadioButton();
            rbAmber = new RadioButton();
            rbWhite = new RadioButton();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            statusLine = new StatusStrip();
            messageLabel = new ToolStripStatusLabel();
            onlineLabel = new ToolStripStatusLabel();
            dsrLabel = new ToolStripStatusLabel();
            systemLabel = new ToolStripStatusLabel();
            g0g1Label = new ToolStripStatusLabel();
            scrollLockLabel = new ToolStripStatusLabel();
            insertLabel = new ToolStripStatusLabel();
            capsLockLabel = new ToolStripStatusLabel();
            numLockLabel = new ToolStripStatusLabel();
            clockLabel = new ToolStripStatusLabel();
            logLabel = new ToolStripStatusLabel();
            TerminalPanel = new Panel();
            SidePanel.SuspendLayout();
            rbPanel.SuspendLayout();
            statusLine.SuspendLayout();
            TerminalPanel.SuspendLayout();
            SuspendLayout();
            // 
            // SidePanel
            // 
            SidePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            SidePanel.BackColor = Color.DarkGray;
            SidePanel.BorderStyle = BorderStyle.Fixed3D;
            SidePanel.CausesValidation = false;
            SidePanel.Controls.Add(PortTextBox);
            SidePanel.Controls.Add(HostTextBox);
            SidePanel.Controls.Add(LogLevelCombo);
            SidePanel.Controls.Add(ScreenFormatCombo);
            SidePanel.Controls.Add(rbPanel);
            SidePanel.Dock = DockStyle.Left;
            SidePanel.Location = new Point(0, 0);
            SidePanel.Name = "SidePanel";
            SidePanel.Size = new Size(175, 450);
            SidePanel.TabIndex = 2;
            // 
            // PortTextBox
            // 
            PortTextBox.Location = new Point(10, 231);
            PortTextBox.Name = "PortTextBox";
            PortTextBox.Size = new Size(100, 23);
            PortTextBox.TabIndex = 9;
            PortTextBox.Text = "2323";
            // 
            // HostTextBox
            // 
            HostTextBox.Location = new Point(10, 202);
            HostTextBox.Name = "HostTextBox";
            HostTextBox.Size = new Size(150, 23);
            HostTextBox.TabIndex = 8;
            HostTextBox.Text = "localhost";
            // 
            // LogLevelCombo
            // 
            LogLevelCombo.Anchor = AnchorStyles.Left;
            LogLevelCombo.FormattingEnabled = true;
            LogLevelCombo.Location = new Point(21, 159);
            LogLevelCombo.Name = "LogLevelCombo";
            LogLevelCombo.Size = new Size(121, 23);
            LogLevelCombo.TabIndex = 7;
            LogLevelCombo.SelectedIndexChanged += LogLevelCombo_SelectedIndexChanged;
            // 
            // ScreenFormatCombo
            // 
            ScreenFormatCombo.Anchor = AnchorStyles.Left;
            ScreenFormatCombo.FormattingEnabled = true;
            ScreenFormatCombo.Location = new Point(21, 119);
            ScreenFormatCombo.Name = "ScreenFormatCombo";
            ScreenFormatCombo.Size = new Size(121, 23);
            ScreenFormatCombo.TabIndex = 6;
            ScreenFormatCombo.SelectedIndexChanged += ScreenFormatCombo_SelectedIndexChanged;
            // 
            // rbPanel
            // 
            rbPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            rbPanel.BackColor = Color.Transparent;
            rbPanel.CausesValidation = false;
            rbPanel.Controls.Add(rbGreen);
            rbPanel.Controls.Add(rbColor);
            rbPanel.Controls.Add(rbBlue);
            rbPanel.Controls.Add(rbAmber);
            rbPanel.Controls.Add(rbWhite);
            rbPanel.Location = new Point(3, 3);
            rbPanel.Name = "rbPanel";
            rbPanel.Size = new Size(158, 96);
            rbPanel.TabIndex = 5;
            // 
            // rbGreen
            // 
            rbGreen.AutoSize = true;
            rbGreen.ForeColor = Color.LimeGreen;
            rbGreen.Location = new Point(0, 7);
            rbGreen.Name = "rbGreen";
            rbGreen.Size = new Size(51, 19);
            rbGreen.TabIndex = 0;
            rbGreen.TabStop = true;
            rbGreen.Text = "Grön";
            rbGreen.UseVisualStyleBackColor = true;
            rbGreen.CheckedChanged += rbGreen_CheckedChanged;
            // 
            // rbColor
            // 
            rbColor.AutoSize = true;
            rbColor.Location = new Point(0, 57);
            rbColor.Name = "rbColor";
            rbColor.Size = new Size(48, 19);
            rbColor.TabIndex = 4;
            rbColor.TabStop = true;
            rbColor.Text = "Färg";
            rbColor.UseVisualStyleBackColor = true;
            rbColor.CheckedChanged += rbColor_CheckedChanged;
            // 
            // rbBlue
            // 
            rbBlue.AutoSize = true;
            rbBlue.ForeColor = Color.Blue;
            rbBlue.Location = new Point(78, 32);
            rbBlue.Name = "rbBlue";
            rbBlue.Size = new Size(41, 19);
            rbBlue.TabIndex = 1;
            rbBlue.TabStop = true;
            rbBlue.Text = "Blå";
            rbBlue.UseVisualStyleBackColor = true;
            rbBlue.CheckedChanged += rbBlue_CheckedChanged;
            // 
            // rbAmber
            // 
            rbAmber.AutoSize = true;
            rbAmber.ForeColor = Color.DarkOrange;
            rbAmber.Location = new Point(78, 7);
            rbAmber.Name = "rbAmber";
            rbAmber.Size = new Size(61, 19);
            rbAmber.TabIndex = 3;
            rbAmber.TabStop = true;
            rbAmber.Text = "Amber";
            rbAmber.UseVisualStyleBackColor = true;
            rbAmber.CheckedChanged += rbAmber_CheckedChanged;
            // 
            // rbWhite
            // 
            rbWhite.AutoSize = true;
            rbWhite.ForeColor = Color.White;
            rbWhite.Location = new Point(0, 32);
            rbWhite.Name = "rbWhite";
            rbWhite.Size = new Size(39, 19);
            rbWhite.TabIndex = 2;
            rbWhite.TabStop = true;
            rbWhite.Text = "Vit";
            rbWhite.UseVisualStyleBackColor = true;
            rbWhite.CheckedChanged += rbWhite_CheckedChanged;
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(176, 17);
            toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // statusLine
            // 
            statusLine.BackColor = Color.Transparent;
            statusLine.Font = new Font("Consolas", 10F);
            statusLine.Items.AddRange(new ToolStripItem[] { messageLabel, logLabel, onlineLabel, dsrLabel, systemLabel, g0g1Label, scrollLockLabel, insertLabel, capsLockLabel, numLockLabel, clockLabel });
            statusLine.Location = new Point(0, 428);
            statusLine.Name = "statusLine";
            statusLine.Size = new Size(625, 22);
            statusLine.TabIndex = 1;
            // 
            // messageLabel
            // 
            messageLabel.Name = "messageLabel";
            messageLabel.Size = new Size(200, 17);
            messageLabel.Spring = true;
            messageLabel.Text = "Ready...";
            messageLabel.TextAlign = ContentAlignment.MiddleLeft;
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
            systemLabel.Size = new Size(90, 17);
            systemLabel.Text = "SYSTEM";
            // 
            // g0g1Label
            // 
            g0g1Label.AutoSize = false;
            g0g1Label.Name = "g0g1Label";
            g0g1Label.Size = new Size(28, 17);
            g0g1Label.Text = "G0";
            // 
            // scrollLockLabel
            // 
            scrollLockLabel.AutoSize = false;
            scrollLockLabel.Name = "scrollLockLabel";
            scrollLockLabel.Size = new Size(44, 17);
            scrollLockLabel.Text = "SCRL";
            // 
            // insertLabel
            // 
            insertLabel.AutoSize = false;
            insertLabel.Name = "insertLabel";
            insertLabel.Size = new Size(36, 17);
            insertLabel.Text = "INS";
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
            // logLabel
            //
            logLabel.AutoSize = false;
            logLabel.Name = "logLabel";
            logLabel.Size = new Size(100, 17);
            logLabel.Text = "";
            logLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // TerminalPanel
            // 
            TerminalPanel.BackColor = Color.Transparent;
            TerminalPanel.Controls.Add(statusLine);
            TerminalPanel.Dock = DockStyle.Fill;
            TerminalPanel.ForeColor = SystemColors.ActiveCaptionText;
            TerminalPanel.Location = new Point(175, 0);
            TerminalPanel.Name = "TerminalPanel";
            TerminalPanel.Size = new Size(625, 450);
            TerminalPanel.TabIndex = 4;
            // 
            // PT200
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(TerminalPanel);
            Controls.Add(SidePanel);
            Name = "PT200";
            Text = "Form1";
            SidePanel.ResumeLayout(false);
            SidePanel.PerformLayout();
            rbPanel.ResumeLayout(false);
            rbPanel.PerformLayout();
            statusLine.ResumeLayout(false);
            statusLine.PerformLayout();
            TerminalPanel.ResumeLayout(false);
            TerminalPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private Panel SidePanel;
        private RadioButton rbGreen;
        private RadioButton rbBlue;
        private RadioButton rbColor;
        private RadioButton rbAmber;
        private RadioButton rbWhite;
        private Panel rbPanel;
        private ComboBox LogLevelCombo;
        private ComboBox ScreenFormatCombo;
        private TextBox PortTextBox;
        private TextBox HostTextBox;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private StatusStrip statusLine;
        private ToolStripStatusLabel messageLabel;
        private ToolStripStatusLabel onlineLabel;
        private ToolStripStatusLabel dsrLabel;
        private ToolStripStatusLabel systemLabel;
        private ToolStripStatusLabel g0g1Label;
        private ToolStripStatusLabel scrollLockLabel;
        private ToolStripStatusLabel insertLabel;
        private ToolStripStatusLabel capsLockLabel;
        private ToolStripStatusLabel numLockLabel;
        private ToolStripStatusLabel clockLabel;
        private ToolStripStatusLabel logLabel;
        private Panel TerminalPanel;
    }
}
