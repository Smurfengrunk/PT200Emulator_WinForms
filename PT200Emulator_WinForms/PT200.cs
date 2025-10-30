using PT200Emulator_WinForms.Engine;
using PT200_Logging;
using PT200_Parser;
using PT200_Rendering;
using PT200Emulator_WinForms.Controls;
using Serilog;
using System.Windows.Forms;
using Serilog.Events;

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

        public PT200()
        {
            InitializeComponent();
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
            Log.Logger = CreateLogger();
            statusController = new StatusLineController(logLabel, onlineLabel, systemLabel, dsrLabel, g0g1Label, statusLine);
            statusController.SetOnline(false);
            statusController.SetSystemReady(false);
            statusController.SetDsr(false);
            statusController.SetCharset(false);

            InitStatusTimers();
            this.LogDebug("Status timers initialized.");
            InitTerminal();
            this.LogDebug("Initializing Terminal Control...");

            terminalCtrl = new TerminalCtrl(_parser.Screenbuffer)
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            TerminalPanel.Controls.Add(terminalCtrl);
            terminalCtrl.AttachBuffer(_parser.Screenbuffer);
            //terminalCtrl.Size = CalculateTerminalSize(80, 24);
            this.ClientSize = CalculateTerminalSize(80, 24 + 1); // extra rad för statusfält
            terminalCtrl.Focus();
            this.LogDebug("Terminal Control initialized and buffer attached.");

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
            terminalCtrl.Dock = DockStyle.Fill;

            this.KeyPreview = true;
            this.KeyUp += MainForm_KeyUp;
            this.LogDebug("MainForm KeyUp event handler attached.");

            ScreenFormatCombo.DataSource = Enum.GetValues(typeof(TerminalState.ScreenFormat));
            LogLevelCombo.DataSource = Enum.GetValues(typeof(Serilog.Events.LogEventLevel));
            LogLevelCombo.SelectedIndex = 1;
        }

        private Size CalculateTerminalSize(int cols, int rows)
        {
            using var g = terminalCtrl.CreateGraphics();
            SizeF charSize = g.MeasureString("W", terminalCtrl.Font);
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
            if (ScreenFormatCombo.SelectedItem is TerminalState.ScreenFormat format)
            {
                terminalState.screenFormat = format;
                terminalState.SetScreenFormat();
                terminalCtrl.Resize(_parser.Screenbuffer.Cols, _parser.Screenbuffer.Rows);
                this.ClientSize = new Size(terminalCtrl.Width, terminalCtrl.Height + statusLine.Height);
                this.LogDebug($"Screen format changed to {format}, resized terminal to {terminalCtrl.Size}.");
            }
        }

        private void LogLevelCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (LogLevelCombo.SelectedItem is Serilog.Events.LogEventLevel level)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Is(level)
                    .WriteTo.Debug()
                    .CreateLogger();
                this.LogDebug($"Log level changed to {level}.");
                statusController.SetLogLevel(level.ToString());
            }
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
        private ILogger CreateLogger()
        {
            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .CreateLogger();
        }

        private void InitTerminal()
        {
            _transport = new Transport(statusController);
            _parser = _transport.GetParser();
            //_renderer = new RenderCore();
            //_input = new Input();
        }

        protected override async void OnLoad(EventArgs e)
        {
            this.LogDebug("Form loaded, starting transport connection...");
            base.OnLoad(e);
            await _transport.Connect(_cts.Token);
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
