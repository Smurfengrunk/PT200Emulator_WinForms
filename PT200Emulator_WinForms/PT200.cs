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

        public PT200(LoggingLevelSwitch levelSwitch)
        {
            InitializeComponent();
            _levelSwitch = levelSwitch;

            rbGreen.ForeColor = Color.LimeGreen;
            rbAmber.ForeColor = Color.DarkOrange;
            rbWhite.ForeColor = Color.White;
            rbBlue.ForeColor = Color.Blue;
            rbColor.ForeColor = Color.Purple;
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
            insertLabel.ForeColor = Control.IsKeyLocked(Keys.Insert) ? Color.Green : statusLine.BackColor;
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
            terminalCtrl.BackColor = Color.Black;
            terminalCtrl.Dock = DockStyle.None;
            TerminalPanel.Controls.Add(terminalCtrl);
            this.LogDebug("Calculated terminal size: {Size}", terminalCtrl.Size);
            this.ClientSize = CalculateTerminalSize(80, 24 + 1); // extra rad för statusfält
            this.LogDebug("Set MainForm client size to: {Size}", this.ClientSize);
            terminalCtrl.Focus();
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
            statusLine.Dock = DockStyle.Bottom;
            terminalCtrl.Size = CalculateTerminalSize(80, 24);
            terminalCtrl.Anchor = AnchorStyles.None;
            terminalCtrl.Dock = DockStyle.None;
            TerminalPanel.ResumeLayout(true);
            terminalCtrl.ResumeLayout(true);

            this.KeyPreview = true;
            this.KeyUp += MainForm_KeyUp;
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
        }

        private Size CalculateTerminalSize(int cols, int rows)
        {
            SizeF charSize = TextRenderer.MeasureText("W", terminalCtrl.Font);
            int terminalWidth = (int)Math.Ceiling((charSize.Width * cols));
            int terminalHeight = (int)Math.Ceiling((charSize.Height * rows));
            //this.ClientSize = new Size(terminalWidth + SidePanel.Width, terminalHeight + statusLine.Height);

            return new Size(terminalWidth, terminalHeight);
        }

        private void rbGreen_CheckedChanged(object sender, EventArgs e)
        {
            rePaint(Color.LimeGreen);
        }

        private void rbAmber_CheckedChanged(object sender, EventArgs e)
        {
            rePaint(Color.DarkOrange);
        }

        private void rbWhite_CheckedChanged(object sender, EventArgs e)
        {
            rePaint(Color.White);
        }

        private void rbBlue_CheckedChanged(object sender, EventArgs e)
        {
            rePaint(Color.Blue);
        }

        private void rbColor_CheckedChanged(object sender, EventArgs e)
        {
            rePaint(Color.Wheat);
        }

        private void ScreenFormatCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ScreenFormatCombo.SelectedValue is TerminalState.ScreenFormat format)
            {
                terminalState.screenFormat = format;
                terminalState.SetScreenFormat();
                terminalCtrl.ChangeFormat(terminalState.Columns, terminalState.Rows);
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


        private void rePaint(Color textColor)
        {
            terminalCtrl.ForeColor = textColor;
            terminalCtrl.BackColor = Color.Black; // alltid svart bakgrund
            statusLine.BackColor = textColor;     // statusraden kan fortfarande följa textfärgen
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
            insertLabel.ForeColor = Control.IsKeyLocked(Keys.Insert) ? Color.Green : statusLine.BackColor;
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

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.LogDebug("Form loaded, starting transport connection...");
            terminalCtrl.ChangeFormat(terminalState.Columns, terminalState.Rows);
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            this.LogDebug("Button clicked at {Time}", DateTime.Now);

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
