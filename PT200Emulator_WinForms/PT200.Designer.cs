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
            FullRedrawButton = new Button();
            DiagButton = new Button();
            DisconnectButton = new Button();
            ConnectButton = new Button();
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
            statusLine = new StatusStrip();
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
            SidePanel.SuspendLayout();
            rbPanel.SuspendLayout();
            statusLine.SuspendLayout();
            layoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // SidePanel
            // 
            SidePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            SidePanel.BackColor = Color.DarkGray;
            SidePanel.BorderStyle = BorderStyle.Fixed3D;
            SidePanel.CausesValidation = false;
            SidePanel.Controls.Add(FullRedrawButton);
            SidePanel.Controls.Add(DiagButton);
            SidePanel.Controls.Add(DisconnectButton);
            SidePanel.Controls.Add(ConnectButton);
            SidePanel.Controls.Add(PortTextBox);
            SidePanel.Controls.Add(HostTextBox);
            SidePanel.Controls.Add(LogLevelCombo);
            SidePanel.Controls.Add(ScreenFormatCombo);
            SidePanel.Controls.Add(rbPanel);
            SidePanel.Dock = DockStyle.Left;
            SidePanel.Location = new Point(0, 0);
            SidePanel.Margin = new Padding(0);
            SidePanel.MinimumSize = new Size(175, 450);
            SidePanel.Name = "SidePanel";
            layoutPanel.SetRowSpan(SidePanel, 2);
            SidePanel.Size = new Size(175, 450);
            SidePanel.TabIndex = 2;
            // 
            // FullRedrawButton
            // 
            FullRedrawButton.Anchor = AnchorStyles.Left;
            FullRedrawButton.Location = new Point(92, 279);
            FullRedrawButton.Name = "FullRedrawButton";
            FullRedrawButton.Size = new Size(76, 23);
            FullRedrawButton.TabIndex = 13;
            FullRedrawButton.TabStop = false;
            FullRedrawButton.Text = "Full redraw";
            FullRedrawButton.UseVisualStyleBackColor = true;
            FullRedrawButton.Click += FullRedrawButton_Click;
            // 
            // DiagButton
            // 
            DiagButton.Anchor = AnchorStyles.Left;
            DiagButton.Location = new Point(3, 279);
            DiagButton.Name = "DiagButton";
            DiagButton.Size = new Size(86, 23);
            DiagButton.TabIndex = 12;
            DiagButton.TabStop = false;
            DiagButton.Text = "Diag overlay";
            DiagButton.UseVisualStyleBackColor = true;
            DiagButton.Click += DiagButton_Click;
            // 
            // DisconnectButton
            // 
            DisconnectButton.Anchor = AnchorStyles.Left;
            DisconnectButton.Location = new Point(3, 250);
            DisconnectButton.Name = "DisconnectButton";
            DisconnectButton.Size = new Size(75, 23);
            DisconnectButton.TabIndex = 11;
            DisconnectButton.TabStop = false;
            DisconnectButton.Text = "Disconnect";
            DisconnectButton.UseVisualStyleBackColor = true;
            // 
            // ConnectButton
            // 
            ConnectButton.Anchor = AnchorStyles.Left;
            ConnectButton.Location = new Point(3, 221);
            ConnectButton.Name = "ConnectButton";
            ConnectButton.Size = new Size(75, 23);
            ConnectButton.TabIndex = 10;
            ConnectButton.TabStop = false;
            ConnectButton.Text = "Connect";
            ConnectButton.UseVisualStyleBackColor = true;
            ConnectButton.Click += ConnectButton_Click;
            // 
            // PortTextBox
            // 
            PortTextBox.Anchor = AnchorStyles.Left;
            PortTextBox.Location = new Point(7, 192);
            PortTextBox.Name = "PortTextBox";
            PortTextBox.Size = new Size(100, 23);
            PortTextBox.TabIndex = 9;
            PortTextBox.TabStop = false;
            PortTextBox.Text = "2323";
            // 
            // HostTextBox
            // 
            HostTextBox.Anchor = AnchorStyles.Left;
            HostTextBox.Location = new Point(7, 163);
            HostTextBox.Name = "HostTextBox";
            HostTextBox.Size = new Size(150, 23);
            HostTextBox.TabIndex = 8;
            HostTextBox.TabStop = false;
            HostTextBox.Text = "localhost";
            // 
            // LogLevelCombo
            // 
            LogLevelCombo.Anchor = AnchorStyles.Left;
            LogLevelCombo.FormattingEnabled = true;
            LogLevelCombo.Location = new Point(7, 134);
            LogLevelCombo.Name = "LogLevelCombo";
            LogLevelCombo.Size = new Size(121, 23);
            LogLevelCombo.TabIndex = 7;
            LogLevelCombo.TabStop = false;
            LogLevelCombo.SelectedIndexChanged += LogLevelCombo_SelectedIndexChanged;
            // 
            // ScreenFormatCombo
            // 
            ScreenFormatCombo.Anchor = AnchorStyles.Left;
            ScreenFormatCombo.FormattingEnabled = true;
            ScreenFormatCombo.Location = new Point(7, 105);
            ScreenFormatCombo.Name = "ScreenFormatCombo";
            ScreenFormatCombo.Size = new Size(121, 23);
            ScreenFormatCombo.TabIndex = 6;
            ScreenFormatCombo.TabStop = false;
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
            rbWhite.Text = "Vit";
            rbWhite.UseVisualStyleBackColor = true;
            rbWhite.CheckedChanged += rbWhite_CheckedChanged;
            // 
            // statusLine
            // 
            statusLine.AutoSize = false;
            statusLine.BackColor = Color.LimeGreen;
            statusLine.Font = new Font("Consolas", 10F);
            statusLine.Items.AddRange(new ToolStripItem[] { messageLabel, logLabel, onlineLabel, dsrLabel, systemLabel, g0g1Label, insertLabel, scrollLockLabel, capsLockLabel, numLockLabel, clockLabel });
            statusLine.Location = new Point(175, 428);
            statusLine.Name = "statusLine";
            statusLine.Size = new Size(625, 22);
            statusLine.TabIndex = 1;
            // 
            // messageLabel
            // 
            messageLabel.Name = "messageLabel";
            messageLabel.Size = new Size(510, 17);
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
            layoutPanel.ColumnCount = 2;
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175F));
            layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutPanel.Controls.Add(SidePanel, 0, 0);
            layoutPanel.Controls.Add(statusLine, 1, 1);
            layoutPanel.SetRowSpan(SidePanel, 2);
            layoutPanel.Dock = DockStyle.Fill;
            layoutPanel.Location = new Point(0, 0);
            layoutPanel.Margin = new Padding(0);
            layoutPanel.Name = "layoutPanel";
            layoutPanel.RowCount = 2;
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            layoutPanel.Size = new Size(800, 450);
            layoutPanel.TabIndex = 3;
            // 
            // PT200
            // 
            AutoScaleMode = AutoScaleMode.None;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ClientSize = new Size(800, 450);
            Controls.Add(layoutPanel);
            Name = "PT200";
            Text = "PT200";
            SidePanel.ResumeLayout(false);
            SidePanel.PerformLayout();
            rbPanel.ResumeLayout(false);
            rbPanel.PerformLayout();
            statusLine.ResumeLayout(false);
            statusLine.PerformLayout();
            layoutPanel.ResumeLayout(false);
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
        private Button DiagButton;
        private Button DisconnectButton;
        private Button ConnectButton;
        private TableLayoutPanel layoutPanel;
        private Button FullRedrawButton;
    }
}
