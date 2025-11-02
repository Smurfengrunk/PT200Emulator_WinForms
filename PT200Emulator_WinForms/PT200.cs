using PT200_InputHandler;
using PT200_Logging;
using PT200_Parser;
using PT200_Rendering;
using PT200Emulator_WinForms.Controls;
using PT200Emulator_WinForms.Engine;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;

namespace PT200Emulator_WinForms
{
    public partial class PT200 : Form
    {
        private System.Windows.Forms.Timer _clockTimer = new();
        private StatusLineController statusController;
        private Transport _transport;
        private TerminalParser _parser;
        private TerminalCtrl terminalCtrl;
        private CancellationTokenSource _cts = new();
        private static TerminalState terminalState = new TerminalState();
        private readonly LoggingLevelSwitch _levelSwitch;
        private IInputMapper _inputMapper;
        private PT200_InputHandler.PT200_InputHandler _inputHandler;

        public PT200(LoggingLevelSwitch levelSwitch)
        {
            InitializeComponent();
            layoutPanel.PerformLayout();
            this.PerformLayout();
            _levelSwitch = levelSwitch;

            rbGreen.ForeColor = Color.LimeGreen;
            rbGreen.Click += (s, e) => terminalCtrl.Focus();
            rbAmber.ForeColor = Color.DarkOrange;
            rbAmber.Click += (s, e) => terminalCtrl.Focus();
            rbWhite.ForeColor = Color.White;
            rbWhite.Click += (s, e) => terminalCtrl.Focus();
            rbBlue.ForeColor = Color.Blue;
            rbBlue.Click += (s, e) => terminalCtrl.Focus();
            rbColor.ForeColor = Color.Purple;
            rbColor.Click += (s, e) => terminalCtrl.Focus();
            ScreenFormatCombo.DropDownClosed += (s, e) => terminalCtrl.Focus();
            LogLevelCombo.DropDownClosed += (s, e) => terminalCtrl.Focus();
            ConnectButton.Click += (s, e) => terminalCtrl.Focus();
            DisconnectButton.Click += (s, e) => terminalCtrl.Focus();
            DiagButton.Click += (s, e) => terminalCtrl.Focus();
            FullRedrawButton.Click += (s, e) => terminalCtrl.Focus();
            clockLabel.BackColor = Color.Transparent;
            numLockLabel.BackColor = Color.Transparent;
            capsLockLabel.BackColor = Color.Transparent;
            scrollLockLabel.BackColor = Color.Transparent;
            insertLabel.BackColor = Color.Transparent;
            messageLabel.BackColor = Color.Transparent;
            onlineLabel.BackColor = Color.Transparent;
            dsrLabel.BackColor = Color.Transparent;
            systemLabel.BackColor = Color.Transparent;
            g0g1Label.BackColor = Color.Transparent;
            numLockLabel.ForeColor = Control.IsKeyLocked(Keys.NumLock) ? statusLine.ForeColor : statusLine.BackColor;
            capsLockLabel.ForeColor = Control.IsKeyLocked(Keys.CapsLock) ? statusLine.ForeColor : statusLine.BackColor;
            scrollLockLabel.ForeColor = Control.IsKeyLocked(Keys.Scroll) ? statusLine.ForeColor : statusLine.BackColor;
            insertLabel.ForeColor = Control.IsKeyLocked(Keys.Insert) ? Color.Red : statusLine.BackColor;
            //Log.Logger = CreateLogger();
            statusController = new StatusLineController(logLabel, onlineLabel, systemLabel, dsrLabel, g0g1Label, statusLine);
            statusController.SetOnline(false);
            statusController.SetSystemReady(false);
            statusController.SetDsr(false);
            statusController.SetCharset(false);
            Program.logForm.Show();

            InitStatusTimers();

            terminalCtrl = new TerminalCtrl();
            terminalCtrl.SuspendLayout();
            this.LogDebug("Terminal Control initialized and buffer attached.");
            _transport = new Transport(statusController, terminalCtrl);
            _parser = _transport.GetParser();

            statusLine.BackColor = terminalCtrl.ForeColor;
            statusLine.Renderer = new ToolStripProfessionalRenderer(new ProfessionalColorTable
            {
                // neutralisera kantfärger
                UseSystemColors = false
            });
            statusLine.Paint += (s, e) =>
            {
                foreach (ToolStripItem item in statusLine.Items)
                {
                    if (item is ToolStripStatusLabel lbl && lbl != messageLabel)
                    {
                        var r = lbl.Bounds;
                        e.Graphics.DrawLine(Pens.Black, r.Left, r.Top, r.Left, r.Bottom - 1);
                    }
                }
            };
            terminalCtrl.BackColor = Color.Black;
            terminalCtrl.Margin = new Padding(3, 3, 0, 0);
            this.LogErr($"Terminal Control margin set to: {terminalCtrl.Margin}");
            layoutPanel.Controls.Add(terminalCtrl, 1, 0);
            terminalCtrl.ResumeLayout(true);
            SidePanel.Dock = DockStyle.Fill;
            terminalCtrl.Dock = DockStyle.Fill;
            statusLine.Dock = DockStyle.Fill;
            this.PerformLayout();
            layoutPanel.PerformLayout();
            ResizeWindowToFit();
            this.LogDebug("Terminal Control size set to: {Size}", terminalCtrl.Size);
            this.LogDebug($"MainForm ClientSize: {this.ClientSize}, SidePanel: {SidePanel.Size}, statusLine: {statusLine.Size}");
            this.LogDebug("Terminal Control location set to: {Location}", terminalCtrl.Location);

            this.KeyPreview = true;
            this.KeyUp += MainForm_KeyUp;
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.LogDebug("MainForm KeyUp event handler attached.");

            var formats = Enum.GetValues(typeof(TerminalState.ScreenFormat))
                              .Cast<TerminalState.ScreenFormat>()
                              .Select(f => new { Format = f, Name = EnumHelper.GetDescription(f) })
                              .ToList();

            ScreenFormatCombo.DisplayMember = "Name";
            ScreenFormatCombo.ValueMember = "Format";
            ScreenFormatCombo.DataSource = formats;
            ScreenFormatCombo.SelectedValue = terminalState.screenFormat;
            LogLevelCombo.DataSource = Enum.GetValues(typeof(Serilog.Events.LogEventLevel));
            LogLevelCombo.SelectedIndex = 1;
            _levelSwitch = levelSwitch;
            _inputHandler = new();
            _inputMapper = _inputHandler.inputMapper;
            terminalCtrl.InputMapper = _inputMapper;
            terminalCtrl._transport = _transport;
            rePaint(Color.LimeGreen, Color.DarkGreen, Color.Black); rbGreen.Select();
            FullRedrawButton.ForeColor = terminalCtrl.AlwaysFullRedraw ? Color.Red : Color.Black;
            terminalCtrl.Focus();
        }

        private void rbGreen_CheckedChanged(object sender, EventArgs e)
        {
            rePaint(Color.LimeGreen, DimColor(Color.LimeGreen), Color.Black);
        }

        private void rbAmber_CheckedChanged(object sender, EventArgs e)
        {
            rePaint(Color.Orange, DimColor(Color.Orange), Color.Black);
        }

        private void rbWhite_CheckedChanged(object sender, EventArgs e)
        {
            rePaint(Color.White, DimColor(Color.White), Color.Black);
        }

        private void rbBlue_CheckedChanged(object sender, EventArgs e)
        {
            rePaint(Color.LightBlue, DimColor(Color.LightBlue), Color.Black);
        }

        private void rbColor_CheckedChanged(object sender, EventArgs e)
        {
            rePaint(Color.Wheat, DimColor(Color.Wheat), Color.Black);
        }

        public Color DimColor(Color color, double Factor = 0.5)
        {
            return Color.FromArgb(
                color.A,
                (int)(color.R * Factor),
                (int)(color.G * Factor),
                (int)(color.B * Factor));
        }

        private void ScreenFormatCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ScreenFormatCombo.SelectedValue is TerminalState.ScreenFormat format)
            {
                terminalState.screenFormat = format;
                terminalState.SetScreenFormat();
                terminalCtrl.ChangeFormat(terminalState.Columns, terminalState.Rows);
                layoutPanel.PerformLayout();
                this.PerformLayout();
                ResizeWindowToFit(); // beräknar storlek baserat på layout
            }
        }

        private void LogLevelCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (LogLevelCombo.SelectedItem.ToString())
            {
                case "Verbose":
                    _levelSwitch.MinimumLevel = LogEventLevel.Verbose;
                    break;
                case "Debug":
                    _levelSwitch.MinimumLevel = LogEventLevel.Debug;
                    break;
                case "Information":
                    _levelSwitch.MinimumLevel = LogEventLevel.Information;
                    break;
                case "Warning":
                    _levelSwitch.MinimumLevel = LogEventLevel.Warning;
                    break;
                case "Error":
                    _levelSwitch.MinimumLevel = LogEventLevel.Error;
                    break;
                case "Fatal":
                    _levelSwitch.MinimumLevel = LogEventLevel.Fatal;
                    break;
            }

            this.LogDebug($"Log level changed to {_levelSwitch.MinimumLevel}");
        }


        private void rePaint(Color TextColor, Color StatusBarColor, Color BackColor)
        {
            terminalCtrl.ForeColor = TextColor;
            terminalCtrl.BackColor = BackColor;     // alltid svart bakgrund
            statusLine.BackColor = StatusBarColor;  // statusraden kan fortfarande följa textfärgen
            statusLine.ForeColor = BackColor;       // och textfärgen inverterad
            numLockLabel.ForeColor = Control.IsKeyLocked(Keys.NumLock) ? statusLine.ForeColor : statusLine.BackColor;
            capsLockLabel.ForeColor = Control.IsKeyLocked(Keys.CapsLock) ? statusLine.ForeColor : statusLine.BackColor;
            scrollLockLabel.ForeColor = Control.IsKeyLocked(Keys.Scroll) ? statusLine.ForeColor : statusLine.BackColor;
            insertLabel.ForeColor = Control.IsKeyLocked(Keys.Insert) ? Color.Red : statusLine.BackColor;

            terminalCtrl.ForceRepaint();
        }

        public void UpdateStatus(string text)
        {
            messageLabel.Text = text;
        }

        private void InitStatusTimers()
        {
            //_clockTimer = new System.Windows.Forms.Timer();
            _clockTimer.Interval = 1000; // 1 second 
            _clockTimer.Tick += (s, e) =>
            {
                clockLabel.Text = DateTime.Now.ToString("HH:mm");
            };
            _clockTimer.Start();
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            numLockLabel.ForeColor = Control.IsKeyLocked(Keys.NumLock) ? statusLine.ForeColor : statusLine.BackColor;
            capsLockLabel.ForeColor = Control.IsKeyLocked(Keys.CapsLock) ? statusLine.ForeColor : statusLine.BackColor;
            scrollLockLabel.ForeColor = Control.IsKeyLocked(Keys.Scroll) ? statusLine.ForeColor : statusLine.BackColor;
            insertLabel.ForeColor = Control.IsKeyLocked(Keys.Insert) ? Color.Red : statusLine.BackColor;
        }

        private void StatusLine_Paint(object sender, PaintEventArgs e)
        {
            foreach (ToolStripItem item in statusLine.Items)
            {
                if (item is ToolStripStatusLabel lbl && lbl != messageLabel)
                {
                    var r = lbl.Bounds;
                    // Rita en svart linje till vänster om fältet
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    e.Graphics.DrawLine(Pens.Red, r.Left, r.Top, r.Left, r.Bottom - 1);
                    this.LogDebug($"Drew line at {r.Left},{r.Top} to {r.Left},{r.Bottom - 1} with Pen {Pens.Black.Color}");
                }
            }
        }

        protected override async void OnLoad(EventArgs e)
        {
            this.LogDebug("Form loaded, starting transport connection...");
            base.OnLoad(e);
            await _transport.Connect(_cts.Token);
            terminalCtrl.Focus();
        }

        private void DiagButton_Click(object sender, EventArgs e)
        {
            terminalCtrl.ShowDiagnosticOverlay = !terminalCtrl.ShowDiagnosticOverlay;
            terminalCtrl.ForceRepaint();
        }

        public void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            _cts.Cancel();
            Log.CloseAndFlush();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            terminalCtrl.ChangeFormat(terminalState.Columns, terminalState.Rows);
            this.BeginInvoke(new Action(() =>
            {
                layoutPanel.PerformLayout();
                this.PerformLayout();
                ResizeWindowToFit();

                this.LogDebug($"[Post-Layout] TerminalCtrl.Location = {terminalCtrl.Location}, Size = {terminalCtrl.Size}");
            }));
            this.LogDebug($"[Shown] TerminalCtrl.Location = {terminalCtrl.Location}, Size = {terminalCtrl.Size}");
            this.LogDebug($"[Shown] layoutPanel.GetColumn = {layoutPanel.GetColumn(terminalCtrl)}, GetRow = {layoutPanel.GetRow(terminalCtrl)}");
        }

        public void ResizeWindowToFit()
        {
            layoutPanel.PerformLayout();
            this.PerformLayout();
            var terminalSize = terminalCtrl.GetPreferredSize(Size.Empty);
            var sideSize = SidePanel.GetPreferredSize(Size.Empty);
            var statusSize = statusLine.GetPreferredSize(Size.Empty);

            int totalWidth = terminalSize.Width + sideSize.Width;
            int totalHeight = terminalSize.Height + statusSize.Height;

            this.ClientSize = new Size(totalWidth, totalHeight);
            this.MinimumSize = this.Size;

            this.LogDebug($"TerminalCtrl final location: {terminalCtrl.Location}, size: {terminalCtrl.Size}");
            this.LogDebug($"MainForm resized to ClientSize {totalWidth}x{totalHeight}, Minimum size set to {this.Size.Width}x{this.Size.Height}");
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            this.LogDebug("Button clicked at {Time}", DateTime.Now);

        }

        private void FullRedrawButton_Click(object sender, EventArgs e)
        {
            terminalCtrl.AlwaysFullRedraw = !terminalCtrl.AlwaysFullRedraw;
            FullRedrawButton.ForeColor = terminalCtrl.AlwaysFullRedraw ? Color.Red : Color.Black;
        }
    }

    public class StatusLineController
    {
        private StatusStrip statusLine;
        private ToolStripStatusLabel logLabel, onlineLabel, systemLabel, dsrLabel, g0g1Label;

        public StatusLineController(ToolStripStatusLabel loglabel, ToolStripStatusLabel online, ToolStripStatusLabel system,
                                    ToolStripStatusLabel dsr, ToolStripStatusLabel g0g1, StatusStrip _statusLine)
        {
            logLabel = loglabel;
            onlineLabel = online;
            systemLabel = system;
            dsrLabel = dsr;
            g0g1Label = g0g1;
            statusLine = _statusLine;
        }

        public void SetOnline(bool online)
        {
            onlineLabel.Text = online ? "ONLINE" : "OFFLINE";
            onlineLabel.ForeColor = online ? statusLine.ForeColor : Color.Red;
        }

        public void SetSystemReady(bool ready)
        {
            systemLabel.Text = ready ? "SYSTEM RDY" : "";
            systemLabel.ForeColor = ready ? statusLine.ForeColor : statusLine.BackColor;
        }

        public void SetDsr(bool xoff)
        {
            dsrLabel.Text = xoff ? "XOFF" : "DSR";
            dsrLabel.ForeColor = xoff ? Color.Red : statusLine.ForeColor;
        }

        public void SetCharset(bool g1Active)
        {
            g0g1Label.Text = g1Active ? "G1" : "G0";
        }

        public void SetLogLevel(string level)
        {
            logLabel.Text = level;
        }
    }
}
