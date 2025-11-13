using PT200_InputHandler;
using PT200_Logging;
using PT200_Parser;
using PT200_Rendering;
using PT200Emulator_WinForms.Controls;
using PT200Emulator_WinForms.Engine;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static PT200_Rendering.IRenderTarget;
using static System.Windows.Forms.AxHost;

namespace PT200Emulator_WinForms
{
    public partial class PT200 : Form
    {
        private System.Windows.Forms.Timer _clockTimer = new();
        private StatusLineController statusController;
        private Transport _transport;
        private TerminalParser _parser => _transport.GetParser();
        private TerminalCtrl terminalCtrl;
        private CancellationTokenSource _cts = new();
        private static TerminalState terminalState;
        private readonly LoggingLevelSwitch _levelSwitch;
        private IInputMapper _inputMapper;
        private PT200_InputHandler.PT200_InputHandler _inputHandler;
        private Panel terminalHost = new Panel();
        private ConfigService _configService;
        private TransportConfig _transportConfig;
        private UiConfig _uiConfig;
        private BindingSource _uiConfigBinding;
        private bool _initializing = false;
        private VisualAttributeManager _visualAttributeManager;
        public PT200(LoggingLevelSwitch levelSwitch)
        {
            InitializeComponent();
            this.Shown += new System.EventHandler(this.MainForm_Shown);
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
            statusController = new StatusLineController(messageLabel, logLabel, onlineLabel, systemLabel, dsrLabel, g0g1Label, statusLine);
            statusController.SetOnline(false);
            statusController.SetSystemReady(false, false);
            statusController.SetDsr(false);
            statusController.SetCharset(false);
            Program.logForm.Show();
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

            InitStatusTimers();
            _configService = new ConfigService(Path.Combine(Application.UserAppDataPath, "config"));
            _transportConfig = _configService.LoadTransportConfig();
            HostTextBox.Text = _transportConfig.Host;
            PortTextBox.Text = _transportConfig.Port.ToString();
            _uiConfig = _configService.LoadUiConfig();
            _uiConfigBinding = new BindingSource { DataSource = _uiConfig };

            _transport = new Transport(statusController, terminalCtrl);
            terminalState = _parser.termState;
            terminalState._screenFormat = _uiConfig.ScreenFormat;
            terminalState.SetScreenFormat();
            _visualAttributeManager = _parser.visualAttributeManager;
            _visualAttributeManager.DisplayTypeChanged += () => terminalState.Display = _uiConfig.DisplayTheme;

            terminalHost.Name = "terminalHost";
            terminalHost.Dock = DockStyle.Fill;
            layoutPanel.Controls.Add(terminalHost, 1, 0);

            terminalCtrl = new TerminalCtrl(_transport, _uiConfig);
            terminalCtrl.SuspendLayout();
            terminalCtrl.Margin = new Padding(3, 3, 0, 0);
            this.LogDebug(Engine.LocalizationProvider.Current.Get("log.terminal.init"));
            terminalCtrl.Dock = DockStyle.Fill;
            terminalHost.Controls.Add(terminalCtrl);

            SidePanel.Dock = DockStyle.Fill;
            terminalCtrl.Dock = DockStyle.Fill;
            statusLine.BackColor = terminalCtrl.ForeColor;
            statusLine.Renderer = new ToolStripProfessionalRenderer(new ProfessionalColorTable
            {
                // neutralisera kantfärger
                UseSystemColors = false
            });
            statusLine.Dock = DockStyle.Fill;
            layoutPanel.PerformLayout();
            this.PerformLayout();
            ResizeWindowToFit(terminalState.Columns, terminalState.Rows);

            this.KeyPreview = true;
            this.KeyUp += MainForm_KeyUp;

            var formats = Enum.GetValues(typeof(TerminalState.ScreenFormat))
                              .Cast<TerminalState.ScreenFormat>()
                              .Select(f => new { Format = f, Name = EnumHelper.GetDescription(f) })
                              .ToList();

            ScreenFormatCombo.DisplayMember = "Name";
            ScreenFormatCombo.ValueMember = "Format";
            ScreenFormatCombo.DataSource = Enum.GetValues(typeof(TerminalState.ScreenFormat));
            ScreenFormatCombo.DataBindings.Add("SelectedItem", _uiConfigBinding, nameof(UiConfig.ScreenFormat), true, DataSourceUpdateMode.OnPropertyChanged);
            _initializing = true;
            LogLevelCombo.DataSource = Enum.GetValues(typeof(LogEventLevel))
                                           .Cast<LogEventLevel>()
                                           .ToList();
            LogLevelCombo.SelectedItem = _uiConfig.DefaultLogLevel;
            _initializing = false;
            LogLevelCombo.DataBindings.Add("SelectedItem", _uiConfigBinding,
                                           nameof(UiConfig.DefaultLogLevel),
                                           true, DataSourceUpdateMode.OnPropertyChanged);
            switch (_uiConfig.DisplayTheme)
            {
                case TerminalState.DisplayType.White: rbWhite.Checked = true; break;
                case TerminalState.DisplayType.Blue: rbBlue.Checked = true; break;
                case TerminalState.DisplayType.Green: rbGreen.Checked = true; break;
                case TerminalState.DisplayType.Amber: rbAmber.Checked = true; break;
                case TerminalState.DisplayType.FullColor: rbColor.Checked = true; break;
                default: break;
            }
            _initializing = true;
            cursorStyleCombo.DataSource = Enum.GetValues(typeof(CursorStyle));
            cursorStyleCombo.SelectedItem = _uiConfig.CursorStylePreference;
            _initializing = false;
            cursorStyleCombo.DataBindings.Add("SelectedItem", _uiConfigBinding,
                                              nameof(UiConfig.CursorStylePreference),
                                              true, DataSourceUpdateMode.OnPropertyChanged);
            terminalCtrl.SetCursorStyle(_uiConfig.CursorStylePreference, _uiConfig.CursorBlink);
            BlinkBox.Checked = _uiConfig.CursorBlink;
            _levelSwitch = levelSwitch;
            _inputHandler = new();
            _inputMapper = _inputHandler.inputMapper;
            terminalCtrl.InputMapper = _inputMapper;
            FullRedrawButton.ForeColor = terminalCtrl.AlwaysFullRedraw ? Color.Red : Color.Black;
            InitLocalizedUI();
            terminalCtrl.ResumeLayout(true);
            terminalCtrl.Focus();
        }

        private void rbGreen_CheckedChanged(object sender, EventArgs e)
        {
            RePaint(StyleInfo.Color.Green, DimColor(Color.LimeGreen), StyleInfo.Color.DarkGreen);
            _uiConfig.DisplayTheme = TerminalState.DisplayType.Green;
        }

        private void rbAmber_CheckedChanged(object sender, EventArgs e)
        {
            RePaint(StyleInfo.Color.Yellow, DimColor(Color.Orange), StyleInfo.Color.DarkYellow);
            _uiConfig.DisplayTheme = TerminalState.DisplayType.Amber;
        }

        private void rbWhite_CheckedChanged(object sender, EventArgs e)
        {
            RePaint(StyleInfo.Color.White, DimColor(Color.White), StyleInfo.Color.Black);
            _uiConfig.DisplayTheme = TerminalState.DisplayType.White;
        }

        private void rbBlue_CheckedChanged(object sender, EventArgs e)
        {
            RePaint(StyleInfo.Color.Blue, DimColor(Color.FromArgb(180, 200, 230)), StyleInfo.Color.Black);
            _uiConfig.DisplayTheme = TerminalState.DisplayType.Blue;
        }

        private void rbColor_CheckedChanged(object sender, EventArgs e)
        {
            RePaint(StyleInfo.Color.Default, DimColor(Color.Wheat), StyleInfo.Color.Black);
            _uiConfig.DisplayTheme = TerminalState.DisplayType.FullColor;
        }

        public Color DimColor(Color color, double Factor = 0.75)
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
                terminalState._screenFormat = format;
                terminalState.SetScreenFormat();
                terminalCtrl.ChangeFormat(terminalState.Columns, terminalState.Rows);
                if (terminalState.Columns >= 132) statusController.SetSystemReady(true, true);
                else statusController.SetSystemReady(true, false);
                layoutPanel.PerformLayout();
                this.PerformLayout();
                ResizeWindowToFit(terminalState.Columns, terminalState.Rows); // beräknar storlek baserat på layout
                _uiConfig.ScreenFormat = format;
                statusController.SetSystemReady((onlineLabel.Text == "ONLINE") ? true : false, (terminalState.Columns >= 132) ? true : false);
            }
        }

        private void LogLevelCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
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

            statusController.SetLogLevel(LogLevelCombo.SelectedItem.ToString());
            _uiConfig.DefaultLogLevel = _levelSwitch.MinimumLevel;
        }


        private void RePaint(StyleInfo.Color TextColor, Color StatusBarColor, StyleInfo.Color BackColor)
        {
            statusLine.BackColor = StatusBarColor;  // statusraden kan fortfarande följa textfärgen
            statusLine.ForeColor = RenderCore.TranslateColor(BackColor);       // och textfärgen inverterad
            numLockLabel.ForeColor = Control.IsKeyLocked(Keys.NumLock) ? statusLine.ForeColor : statusLine.BackColor;
            capsLockLabel.ForeColor = Control.IsKeyLocked(Keys.CapsLock) ? statusLine.ForeColor : statusLine.BackColor;
            scrollLockLabel.ForeColor = Control.IsKeyLocked(Keys.Scroll) ? statusLine.ForeColor : statusLine.BackColor;
            insertLabel.ForeColor = Control.IsKeyLocked(Keys.Insert) ? Color.Red : statusLine.BackColor;

            terminalCtrl.RePaint(RenderCore.TranslateColor(TextColor), RenderCore.TranslateColor(BackColor));
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
                }
            }
        }

        protected override async void OnLoad(EventArgs e)
        {
            this.LogDebug(Engine.LocalizationProvider.Current.Get("log.app.form.loaded"));
            base.OnLoad(e);
            await _transport.Connect(_cts.Token, _transportConfig.Host, _transportConfig.Port);
            statusController.SetSystemReady(true, (terminalState.Columns >= 132) ? true : false);
            terminalCtrl.Focus();
        }

        private void DiagButton_Click(object sender, EventArgs e)
        {
            terminalCtrl.ShowDiagnosticOverlay = !terminalCtrl.ShowDiagnosticOverlay;
            terminalCtrl.ForceRepaint();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            this.LogDebug(Engine.LocalizationProvider.Current.Get("log.app.closing"));
            _configService.SaveTransportConfig(_transportConfig);
            _configService.SaveUiConfig(_uiConfig);
            _cts.Cancel();
            Log.CloseAndFlush();
            base.OnFormClosing(e);
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
        }

        public void ResizeWindowToFit(int cols, int rows)
        {
            int termWidth = cols * terminalCtrl._charWidth;
            int termHeight = rows * terminalCtrl._charHeight;

            int sideWidth = SidePanel.GetPreferredSize(Size.Empty).Width;
            int statusHeight = statusLine.GetPreferredSize(Size.Empty).Height;

            int totalWidth = sideWidth + termWidth + layoutPanel.Padding.Horizontal + terminalHost.Margin.Horizontal;
            int totalHeight = termHeight + statusHeight + layoutPanel.Padding.Vertical + terminalHost.Margin.Vertical;

            this.MinimumSize = new Size(totalWidth, totalHeight);
            this.ClientSize = new Size(totalWidth, totalHeight);

        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            this.LogDebug(Engine.LocalizationProvider.Current.Get("ui.button.connect", DateTime.Now));
            _ = _transport.Connect(_cts.Token);

        }

        private void DisconnectButton_Click(object sender, EventArgs e)
        {
            this.LogDebug(Engine.LocalizationProvider.Current.Get("ui.button.disconnect", DateTime.Now));
            _ = _transport.Disconnect();
        }

        private void FullRedrawButton_Click(object sender, EventArgs e)
        {
            terminalCtrl.AlwaysFullRedraw = !terminalCtrl.AlwaysFullRedraw;
            FullRedrawButton.ForeColor = terminalCtrl.AlwaysFullRedraw ? Color.Red : Color.Black;
        }

        private void ReconnectButton_Click(object sender, EventArgs e)
        {
            this.LogDebug(Engine.LocalizationProvider.Current.Get("ui.button.reconnect", DateTime.Now));
            _ = _transport.Reconnect(_cts.Token);

        }

        private void HostTextBox_TextChanged(object sender, EventArgs e)
        {
            _transportConfig.Host = HostTextBox.Text;
        }

        private void PortTextBox_TextChanged(Object sender, EventArgs e)
        {
            if (int.TryParse(PortTextBox.Text, out int port)) _transportConfig.Port = port;
        }

        private void cursorStyleCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_initializing) return;
            switch (cursorStyleCombo.SelectedItem.ToString())
            {
                case "VerticalBar": _uiConfig.CursorStylePreference = IRenderTarget.CursorStyle.VerticalBar; break;
                case "HorizontalBar": _uiConfig.CursorStylePreference = IRenderTarget.CursorStyle.HorizontalBar; break;
                default: _uiConfig.CursorStylePreference = IRenderTarget.CursorStyle.Block; break;
            }
            terminalCtrl.SetCursorStyle(_uiConfig.CursorStylePreference, _uiConfig.CursorBlink);
        }

        private void BlinkBox_CheckedChanged(object sender, EventArgs e)
        {
            _uiConfig.CursorBlink = terminalState.CursorBlink = (BlinkBox.Checked) ? true : false;
            terminalCtrl.SetCursorStyle(_uiConfig.CursorStylePreference, _uiConfig.CursorBlink);
        }

        private void InitLocalizedUI()
        {
            rbAmber.Text = Engine.LocalizationProvider.Current.Get("ui.color.amber");
            rbBlue.Text = Engine.LocalizationProvider.Current.Get("ui.color.blue");
            rbColor.Text = Engine.LocalizationProvider.Current.Get("ui.color.color");
            rbGreen.Text = Engine.LocalizationProvider.Current.Get("ui.color.green");
            rbWhite.Text = Engine.LocalizationProvider.Current.Get("ui.color.white");
            ConnectButton.Text = Engine.LocalizationProvider.Current.Get("ui.button.connect");
            DisconnectButton.Text = Engine.LocalizationProvider.Current.Get("ui.button.disconnect");
            ReconnectButton.Text = Engine.LocalizationProvider.Current.Get("ui.button.reconnect");
            BlinkBox.Text = Engine.LocalizationProvider.Current.Get("ui.blinkbox.label");
            FullRedrawButton.Text = Engine.LocalizationProvider.Current.Get("ui.button.fullredraw");
            DiagButton.Text = Engine.LocalizationProvider.Current.Get("ui.button.diag");
        }
    }

    public class StatusLineController
    {
        private StatusStrip statusLine;
        private ToolStripStatusLabel messageLabel, logLabel, onlineLabel, systemLabel, dsrLabel, g0g1Label;

        public StatusLineController(ToolStripStatusLabel messagelabel, ToolStripStatusLabel loglabel, ToolStripStatusLabel online, ToolStripStatusLabel system,
                                    ToolStripStatusLabel dsr, ToolStripStatusLabel g0g1, StatusStrip _statusLine)
        {
            messageLabel = messagelabel;
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

        public void SetSystemReady(bool ready, bool lng)
        {
            if (!ready) return;
            if (lng)
            {
                systemLabel.Text = "SYSTEM RDY";
                systemLabel.Width = 90;
            }
            else
            {
                systemLabel.Text = "RDY";
                systemLabel.Width = 30;
            }
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

        public void SetMessage(string message)
        {
            messageLabel.Text = message;
        }
    }
}
