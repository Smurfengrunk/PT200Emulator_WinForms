namespace PT200EmulatorWinForms
{
    partial class Config
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
            CursorStyleCombo = new ComboBox();
            BlinkBox = new CheckBox();
            ReconnectButton = new Button();
            FullRedrawButton = new Button();
            DiagButton = new Button();
            DisconnectButton = new Button();
            ConnectButton = new Button();
            PortTextBox = new TextBox();
            HostTextBox = new TextBox();
            LogLevelCombo = new ComboBox();
            SidePanel = new Panel();
            ScreenFormatCombo = new ComboBox();
            rbPanel = new Panel();
            rbGreen = new RadioButton();
            rbColor = new RadioButton();
            rbBlue = new RadioButton();
            rbAmber = new RadioButton();
            rbWhite = new RadioButton();
            SidePanel.SuspendLayout();
            rbPanel.SuspendLayout();
            SuspendLayout();
            // 
            // CursorStyleCombo
            // 
            CursorStyleCombo.FormattingEnabled = true;
            CursorStyleCombo.Location = new Point(-2, 250);
            CursorStyleCombo.Name = "CursorStyleCombo";
            CursorStyleCombo.Size = new Size(104, 23);
            CursorStyleCombo.TabIndex = 16;
            CursorStyleCombo.SelectedIndexChanged += CursorStyleCombo_SelectedIndexChanged;
            // 
            // BlinkBox
            // 
            BlinkBox.AutoSize = true;
            BlinkBox.Location = new Point(113, 254);
            BlinkBox.Name = "BlinkBox";
            BlinkBox.Size = new Size(52, 19);
            BlinkBox.TabIndex = 15;
            BlinkBox.Text = "Blink";
            BlinkBox.UseVisualStyleBackColor = true;
            BlinkBox.CheckedChanged += BlinkBox_CheckedChanged;
            // 
            // ReconnectButton
            // 
            ReconnectButton.Anchor = AnchorStyles.Left;
            ReconnectButton.Location = new Point(3, 344);
            ReconnectButton.Name = "ReconnectButton";
            ReconnectButton.Size = new Size(75, 23);
            ReconnectButton.TabIndex = 14;
            ReconnectButton.TabStop = false;
            ReconnectButton.Text = "Reconnect";
            ReconnectButton.UseVisualStyleBackColor = true;
            ReconnectButton.Visible = false;
            ReconnectButton.Click += ReconnectButton_Click;
            // 
            // FullRedrawButton
            // 
            FullRedrawButton.Anchor = AnchorStyles.Left;
            FullRedrawButton.Location = new Point(2, 402);
            FullRedrawButton.Name = "FullRedrawButton";
            FullRedrawButton.Size = new Size(76, 23);
            FullRedrawButton.TabIndex = 13;
            FullRedrawButton.TabStop = false;
            FullRedrawButton.Text = "Fullredraw";
            FullRedrawButton.UseVisualStyleBackColor = true;
            FullRedrawButton.Visible = false;
            FullRedrawButton.Click += FullRedrawButton_Click;
            // 
            // DiagButton
            // 
            DiagButton.Anchor = AnchorStyles.Left;
            DiagButton.Location = new Point(3, 373);
            DiagButton.Name = "DiagButton";
            DiagButton.Size = new Size(86, 23);
            DiagButton.TabIndex = 12;
            DiagButton.TabStop = false;
            DiagButton.Text = "Diag";
            DiagButton.UseVisualStyleBackColor = true;
            DiagButton.Visible = false;
            DiagButton.Click += DiagButton_Click;
            // 
            // DisconnectButton
            // 
            DisconnectButton.Anchor = AnchorStyles.Left;
            DisconnectButton.Location = new Point(79, 203);
            DisconnectButton.Name = "DisconnectButton";
            DisconnectButton.Size = new Size(75, 23);
            DisconnectButton.TabIndex = 11;
            DisconnectButton.TabStop = false;
            DisconnectButton.Text = "Disconnect";
            DisconnectButton.UseVisualStyleBackColor = true;
            DisconnectButton.Click += DisconnectButton_Click;
            // 
            // ConnectButton
            // 
            ConnectButton.Anchor = AnchorStyles.Left;
            ConnectButton.Location = new Point(-2, 203);
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
            PortTextBox.Location = new Point(2, 174);
            PortTextBox.Name = "PortTextBox";
            PortTextBox.Size = new Size(100, 23);
            PortTextBox.TabIndex = 9;
            PortTextBox.TabStop = false;
            PortTextBox.Text = "2323";
            // 
            // HostTextBox
            // 
            HostTextBox.Anchor = AnchorStyles.Left;
            HostTextBox.Location = new Point(3, 145);
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
            LogLevelCombo.Location = new Point(2, 315);
            LogLevelCombo.Name = "LogLevelCombo";
            LogLevelCombo.Size = new Size(121, 23);
            LogLevelCombo.TabIndex = 7;
            LogLevelCombo.TabStop = false;
            LogLevelCombo.SelectedIndexChanged += LogLevelCombo_SelectedIndexChanged;
            // 
            // SidePanel
            // 
            SidePanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            SidePanel.BackColor = Color.DarkGray;
            SidePanel.BorderStyle = BorderStyle.Fixed3D;
            SidePanel.CausesValidation = false;
            SidePanel.Controls.Add(CursorStyleCombo);
            SidePanel.Controls.Add(BlinkBox);
            SidePanel.Controls.Add(ReconnectButton);
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
            SidePanel.MinimumSize = new Size(180, 450);
            SidePanel.Name = "SidePanel";
            SidePanel.Size = new Size(180, 450);
            SidePanel.TabIndex = 3;
            // 
            // ScreenFormatCombo
            // 
            ScreenFormatCombo.Anchor = AnchorStyles.Left;
            ScreenFormatCombo.FormattingEnabled = true;
            ScreenFormatCombo.Location = new Point(3, 105);
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
            rbGreen.Size = new Size(56, 19);
            rbGreen.TabIndex = 0;
            rbGreen.Text = "Green";
            rbGreen.UseVisualStyleBackColor = true;
            rbGreen.CheckedChanged += rbGreen_CheckedChanged;
            // 
            // rbColor
            // 
            rbColor.AutoSize = true;
            rbColor.Location = new Point(0, 57);
            rbColor.Name = "rbColor";
            rbColor.Size = new Size(54, 19);
            rbColor.TabIndex = 4;
            rbColor.Text = "Color";
            rbColor.UseVisualStyleBackColor = true;
            rbColor.CheckedChanged += rbColor_CheckedChanged;
            // 
            // rbBlue
            // 
            rbBlue.AutoSize = true;
            rbBlue.ForeColor = Color.Blue;
            rbBlue.Location = new Point(78, 32);
            rbBlue.Name = "rbBlue";
            rbBlue.Size = new Size(48, 19);
            rbBlue.TabIndex = 1;
            rbBlue.Text = "Blue";
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
            rbWhite.Size = new Size(56, 19);
            rbWhite.TabIndex = 2;
            rbWhite.Text = "White";
            rbWhite.UseVisualStyleBackColor = true;
            rbWhite.CheckedChanged += rbWhite_CheckedChanged;
            // 
            // Config
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(181, 450);
            Controls.Add(SidePanel);
            Name = "Config";
            Text = "Config";
            SidePanel.ResumeLayout(false);
            SidePanel.PerformLayout();
            rbPanel.ResumeLayout(false);
            rbPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        internal ComboBox CursorStyleCombo;
        internal CheckBox BlinkBox;
        internal Button DisconnectButton;
        internal Button ConnectButton;
        internal TextBox PortTextBox;
        internal TextBox HostTextBox;
        internal ComboBox LogLevelCombo;
        internal Panel SidePanel;
        internal ComboBox ScreenFormatCombo;
        internal Panel rbPanel;
        internal RadioButton rbGreen;
        internal RadioButton rbColor;
        internal RadioButton rbBlue;
        internal RadioButton rbAmber;
        internal RadioButton rbWhite;
        internal Button ReconnectButton;
        internal Button FullRedrawButton;
        internal Button DiagButton;
    }
}