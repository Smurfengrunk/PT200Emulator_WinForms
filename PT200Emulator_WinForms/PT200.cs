using PT200_InputHandler;

using PT200_Logging;

using PT200_Parser;

using PT200_Rendering;

using PT200EmulatorWinforms.Controls;
using PT200EmulatorWinforms.Engine;

using Serilog;
using Serilog.Core;
using Serilog.Events;

using System.Globalization;
using System.Windows.Forms.Design;

namespace PT200EmulatorWinforms
{
    /// <summary>
    /// Main Form for the PT200 Emulator
    /// </summary>
    public partial class PT200 : Form
    {
        private System.Windows.Forms.Timer _clockTimer = new();
        private StatusLineController statusController;
        private Transport transport;
        private TerminalParser parser => transport.GetParser();
        private TerminalCtrl terminalCtrl;
        private CancellationTokenSource cts = new();
        private static TerminalState terminalState;
        private readonly LoggingLevelSwitch LevelSwitch;
        private IInputMapper inputMapper;
        private PT200_InputHandler.PT200_InputHandler inputHandler;
        private Panel terminalHost = new Panel();
        public ConfigService configService { get; }
        private TransportConfig transportConfig;
        private UiConfig uiConfig;
        private BindingSource uiConfigBinding;
        private bool initializing;
        private VisualAttributeManager visualAttributeManager;
        /// <summary>
        /// Main form constructor. Initializing form members and initializing Transport and Parser.
        /// The Parameter levelSwitch is initialized in Program.cs, and sets the default logging level
        /// </summary>
        /// <param name="levelSwitch"></param>
        public PT200(LoggingLevelSwitch levelSwitch)
        {
            InitializeComponent();
            layoutPanel.PerformLayout();
            this.PerformLayout();
            LevelSwitch = levelSwitch;
            configService = new ConfigService(Path.Combine(Application.UserAppDataPath, "config"));
            transportConfig = configService.LoadTransportConfig();
            HostTextBox.Text = transportConfig.Host;
            PortTextBox.Text = transportConfig.Port.ToString(CultureInfo.InvariantCulture);
            uiConfig = configService.LoadUiConfig();
            uiConfigBinding = new BindingSource { DataSource = uiConfig };

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
            clockLabel.BackColor = StatusLine.BackColor;
            numLockLabel.BackColor = StatusLine.BackColor;
            capsLockLabel.BackColor = StatusLine.BackColor;
            scrollLockLabel.BackColor = StatusLine.BackColor;
            insertLabel.BackColor = StatusLine.BackColor;
            messageLabel.BackColor = StatusLine.BackColor;
            onlineLabel.BackColor = StatusLine.BackColor;
            dsrLabel.BackColor = StatusLine.BackColor;
            systemLabel.BackColor = StatusLine.BackColor;
            g0g1Label.BackColor = StatusLine.BackColor;
            numLockLabel.ForeColor = Control.IsKeyLocked(Keys.NumLock) ? StatusLine.ForeColor : StatusLine.BackColor;
            capsLockLabel.ForeColor = Control.IsKeyLocked(Keys.CapsLock) ? StatusLine.ForeColor : StatusLine.BackColor;
            scrollLockLabel.ForeColor = Control.IsKeyLocked(Keys.Scroll) ? StatusLine.ForeColor : StatusLine.BackColor;
            insertLabel.ForeColor = Control.IsKeyLocked(Keys.Insert) ? Color.Red : StatusLine.BackColor;
            statusController = new StatusLineController(messageLabel, logLabel, onlineLabel, systemLabel, dsrLabel, g0g1Label, StatusLine);
            statusController.SetOnline(false);
            statusController.SetSystemReady(false, false);
            statusController.SetDsr(false);
            statusController.SetCharset(false);
            StatusLine.Paint += (s, e) =>
            {
                foreach (ToolStripItem item in StatusLine.Items)
                {
                    if (item is ToolStripStatusLabel lbl && lbl != messageLabel)
                    {
                        var r = lbl.Bounds;
                        e.Graphics.DrawLine(Pens.Black, r.Left, r.Top, r.Left, r.Bottom - 1);
                    }
                }
            };

            this.LogInformation("PT200 Emulator");
            InitStatusTimers();
            LogFileCleanup();

            terminalHost.Name = "terminalHost";
            terminalHost.Dock = DockStyle.Fill;
            layoutPanel.Controls.Add(terminalHost, 1, 0);

            transport = new Transport(statusController);
            terminalState = parser.termState;
            terminalState._screenFormat = uiConfig.ScreenFormat;
            terminalState.SetScreenFormat();
            visualAttributeManager = parser.visualAttributeManager;
            visualAttributeManager.DisplayTypeChanged += () => terminalState.Display = uiConfig.DisplayTheme;

            terminalCtrl = new TerminalCtrl(transport, uiConfig);
            statusController.TerminalCtrl = terminalCtrl;
            terminalCtrl.Margin = new Padding(3, 3, 0, 0);
            this.LogDebug(Engine.LocalizationProvider.Current.Get("log.terminal.init"));
            terminalCtrl.Dock = DockStyle.Fill;
            terminalHost.Controls.Add(terminalCtrl);

            SidePanel.Dock = DockStyle.Fill;
            terminalCtrl.Dock = DockStyle.Fill;
            StatusLine.BackColor = terminalCtrl.ForeColor;
            StatusLine.Renderer = new ToolStripProfessionalRenderer(new ProfessionalColorTable
            {
                // neutralisera kantfärger
                UseSystemColors = false
            });
            StatusLine.Dock = DockStyle.Fill;
            layoutPanel.PerformLayout();
            this.PerformLayout();
            ResizeWindowToFit(terminalState.Columns, terminalState.Rows);

            this.KeyPreview = true;
            this.KeyUp += MainForm_KeyUp;

            var formats = Enum.GetValues<TerminalState.ScreenFormat>()
                              .Cast<TerminalState.ScreenFormat>()
                              .Select(f => new { Format = f, Name = EnumHelper.GetDescription(f) })
                              .ToList();

            ScreenFormatCombo.DisplayMember = "Name";
            ScreenFormatCombo.ValueMember = "Format";
            ScreenFormatCombo.DataSource = formats;
            ScreenFormatCombo.DataBindings.Add("SelectedValue", uiConfigBinding, nameof(UiConfig.ScreenFormat), true, DataSourceUpdateMode.OnPropertyChanged);
            initializing = true;
            LogLevelCombo.DataSource = Enum.GetValues<TerminalState.ScreenFormat>()
                                           .Cast<LogEventLevel>()
                                           .ToList();
            LogLevelCombo.SelectedItem = uiConfig.DefaultLogLevel;
            initializing = false;
            LogLevelCombo.DataBindings.Add("SelectedItem", uiConfigBinding,
                                           nameof(UiConfig.DefaultLogLevel),
                                           true, DataSourceUpdateMode.OnPropertyChanged);
            statusController.SetLogLevel(LogLevelCombo.SelectedItem.ToString());

            switch (uiConfig.DisplayTheme)
            {
                case TerminalState.DisplayType.White: rbWhite.Checked = true; break;
                case TerminalState.DisplayType.Blue: rbBlue.Checked = true; break;
                case TerminalState.DisplayType.Green: rbGreen.Checked = true; break;
                case TerminalState.DisplayType.Amber: rbAmber.Checked = true; break;
                case TerminalState.DisplayType.FullColor: rbColor.Checked = true; break;
                default: break;
            }
            initializing = true;
            cursorStyleCombo.DataSource = Enum.GetValues<IRenderTarget.CursorStyle>();
            cursorStyleCombo.SelectedItem = uiConfig.CursorStylePreference;
            initializing = false;
            cursorStyleCombo.DataBindings.Add("SelectedItem", uiConfigBinding,
                                              nameof(UiConfig.CursorStylePreference),
                                              true, DataSourceUpdateMode.OnPropertyChanged);
            terminalCtrl.SetCursorStyle(uiConfig.CursorStylePreference, uiConfig.CursorBlink);
            BlinkBox.Checked = uiConfig.CursorBlink;
            LevelSwitch = levelSwitch;
            inputHandler = new();
            inputMapper = inputHandler.inputMapper;
            terminalCtrl.InputMapper = inputMapper;
            FullRedrawButton.ForeColor = terminalCtrl.AlwaysFullRedraw ? Color.Red : Color.Black;
            InitLocalizedUI();
            if (!uiConfig.DebugControls)
            {
                Program.logForm.Close();
                ReconnectButton.Hide();
                FullRedrawButton.Hide();
                DiagButton.Hide();
            }
            else
            {
                Program.logForm.Show();
                ReconnectButton.Show();
                FullRedrawButton.Show();
                DiagButton.Show();
            }
            terminalCtrl.Focus();
        }

        /// <summary>
        /// Code to change the screen color to classic green
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbGreen_CheckedChanged(object sender, EventArgs e)
        {
            RePaint(StyleInfo.Color.Green, DimColor(Color.LimeGreen), StyleInfo.Color.DarkGreen);
            uiConfig.DisplayTheme = TerminalState.DisplayType.Green;
        }

        /// <summary>
        /// Code to change the screen color to classic amber
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbAmber_CheckedChanged(object sender, EventArgs e)
        {
            RePaint(StyleInfo.Color.Yellow, DimColor(Color.Orange), StyleInfo.Color.DarkYellow);
            uiConfig.DisplayTheme = TerminalState.DisplayType.Amber;
        }

        /// <summary>
        /// Code to change the screen color to classic white (well, almost...)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbWhite_CheckedChanged(object sender, EventArgs e)
        {
            RePaint(StyleInfo.Color.White, DimColor(Color.White), StyleInfo.Color.Black);
            uiConfig.DisplayTheme = TerminalState.DisplayType.White;
        }

        /// <summary>
        /// Code to change the screen color to HP-style "white"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbBlue_CheckedChanged(object sender, EventArgs e)
        {
            RePaint(StyleInfo.Color.Blue, DimColor(Color.FromArgb(180, 200, 230)), StyleInfo.Color.Black);
            uiConfig.DisplayTheme = TerminalState.DisplayType.Blue;
        }

        /// <summary>
        /// Code to change the screen color to full color (partially implemented)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbColor_CheckedChanged(object sender, EventArgs e)
        {
            RePaint(StyleInfo.Color.Default, DimColor(Color.Wheat), StyleInfo.Color.Black);
            uiConfig.DisplayTheme = TerminalState.DisplayType.FullColor;
        }

        /// <summary>
        /// Returns a Color object with the supplied color reduced to supplied factor
        /// </summary>
        /// <param name="color"></param>
        /// <param name="Factor"></param>
        /// <returns></returns>
        public static Color DimColor(Color color, double Factor = 0.65)
        {
            return Color.FromArgb(
                color.A,
                (int)(color.R * Factor),
                (int)(color.G * Factor),
                (int)(color.B * Factor));
        }

        /// <summary>
        /// Combobox to change Screen format to one of the formats specified in the ScreenFormat enum in TerminalState
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                uiConfig.ScreenFormat = format;
                statusController.SetSystemReady((onlineLabel.Text == "ONLINE") ? true : false, (terminalState.Columns >= 132) ? true : false);
            }
        }

        /// <summary>
        /// Combobox to change the logging level to one of the levels in the LovEventLevel enum
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogLevelCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            switch (LogLevelCombo.SelectedItem.ToString())
            {
                case "Verbose":
                    LevelSwitch.MinimumLevel = LogEventLevel.Verbose;
                    break;
                case "Debug":
                    LevelSwitch.MinimumLevel = LogEventLevel.Debug;
                    break;
                case "Information":
                    LevelSwitch.MinimumLevel = LogEventLevel.Information;
                    break;
                case "Warning":
                    LevelSwitch.MinimumLevel = LogEventLevel.Warning;
                    break;
                case "Error":
                    LevelSwitch.MinimumLevel = LogEventLevel.Error;
                    break;
                case "Fatal":
                    LevelSwitch.MinimumLevel = LogEventLevel.Fatal;
                    break;
            }

            statusController.SetLogLevel(LogLevelCombo.SelectedItem.ToString());
            uiConfig.DefaultLogLevel = LevelSwitch.MinimumLevel;
        }


        /// <summary>
        /// RePaint needs to be called for changing color on text and status line when changing screen color
        /// </summary>
        /// <param name="TextColor"></param>
        /// <param name="StatusBarColor"></param>
        /// <param name="BackColor"></param>
        private void RePaint(StyleInfo.Color TextColor, Color StatusBarColor, StyleInfo.Color BackColor)
        {
            StatusLine.BackColor = StatusBarColor;  // statusraden kan fortfarande följa textfärgen
            StatusLine.ForeColor = RenderCore.TranslateColor(BackColor);       // och textfärgen inverterad
            numLockLabel.ForeColor = Control.IsKeyLocked(Keys.NumLock) ? StatusLine.ForeColor : StatusLine.BackColor;
            capsLockLabel.ForeColor = Control.IsKeyLocked(Keys.CapsLock) ? StatusLine.ForeColor : StatusLine.BackColor;
            scrollLockLabel.ForeColor = Control.IsKeyLocked(Keys.Scroll) ? StatusLine.ForeColor : StatusLine.BackColor;
            insertLabel.ForeColor = Control.IsKeyLocked(Keys.Insert) ? Color.Red : StatusLine.BackColor;

            terminalCtrl.RePaint(RenderCore.TranslateColor(TextColor), RenderCore.TranslateColor(BackColor));
        }

        /// <summary>
        /// Updates the message in the message field in the statusline (not used at the moment)
        /// </summary>
        /// <param name="text"></param>
        public void UpdateStatus(string text)
        {
            messageLabel.Text = text;
        }

        /// <summary>
        /// Starts and changes the clock field every second
        /// </summary>
        private void InitStatusTimers()
        {
            //_clockTimer = new System.Windows.Forms.Timer();
            _clockTimer.Interval = 1000; // 1 second 
            _clockTimer.Tick += (s, e) =>
            {
                clockLabel.Text = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
            };
            _clockTimer.Start();
        }

        /// <summary>
        /// Modifies the respective fields in the status line to reflect the status of numlock, capslock, scrollock and insert keys
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            numLockLabel.ForeColor = Control.IsKeyLocked(Keys.NumLock) ? StatusLine.ForeColor : StatusLine.BackColor;
            capsLockLabel.ForeColor = Control.IsKeyLocked(Keys.CapsLock) ? StatusLine.ForeColor : StatusLine.BackColor;
            scrollLockLabel.ForeColor = Control.IsKeyLocked(Keys.Scroll) ? StatusLine.ForeColor : StatusLine.BackColor;
            insertLabel.ForeColor = Control.IsKeyLocked(Keys.Insert) ? Color.Red : StatusLine.BackColor;
        }

        /// <summary>
        /// Code run when the main form is loaded: Call the base OnLoad method, connect to host and update the statusbar
        /// </summary>
        /// <param name="e"></param>
        protected override async void OnLoad(EventArgs e)
        {
            this.LogDebug(Engine.LocalizationProvider.Current.Get("log.app.form.loaded"));
            base.OnLoad(e);
            await transport.Connect(cts.Token, transportConfig.Host, transportConfig.Port);
            statusController.SetSystemReady(true, (terminalState.Columns >= 132) ? true : false);
            terminalCtrl.Focus();
        }

        /// <summary>
        /// Temporary during development
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DiagButton_Click(object sender, EventArgs e)
        {
            terminalCtrl.ShowDiagnosticOverlay = !terminalCtrl.ShowDiagnosticOverlay;
            terminalCtrl.ForceRepaint();
        }

        /// <summary>
        /// Save config data, cancel connection, flush log and then call the base OnFormClosing to clean upp when closing the application
        /// </summary>
        /// <param name="e"></param>
        protected async override void OnFormClosing(FormClosingEventArgs e)
        {
            this.LogDebug(Engine.LocalizationProvider.Current.Get("log.app.closing"));
            configService.SaveTransportConfig(transportConfig);
            configService.SaveUiConfig(uiConfig);
            cts.Cancel();
            Log.CloseAndFlush();
            await transport.Disconnect();
            base.OnFormClosing(e);
        }

        /// <summary>
        /// Reizes main window to fit the actual screen format of the terminal
        /// </summary>
        /// <param name="cols"></param>
        /// <param name="rows"></param>
        public void ResizeWindowToFit(int cols, int rows)
        {
            int termWidth = cols * terminalCtrl.charWidth;
            int termHeight = rows * terminalCtrl.charHeight;

            int sideWidth = SidePanel.GetPreferredSize(Size.Empty).Width;
            int statusHeight = StatusLine.GetPreferredSize(Size.Empty).Height;

            int totalWidth = sideWidth + termWidth + layoutPanel.Padding.Horizontal + terminalHost.Margin.Horizontal;
            int totalHeight = termHeight + statusHeight + layoutPanel.Padding.Vertical + terminalHost.Margin.Vertical;

            this.MinimumSize = new Size(totalWidth, totalHeight);
            this.ClientSize = new Size(totalWidth, totalHeight);

        }

        /// <summary>
        /// Button to connect to host (partially implenmented)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectButton_Click(object sender, EventArgs e)
        {
            this.LogDebug(Engine.LocalizationProvider.Current.Get("ui.button.connect", DateTime.Now));
            _ = transport.Connect(cts.Token);

        }

        /// <summary>
        /// Button to disconnect from host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisconnectButton_Click(object sender, EventArgs e)
        {
            this.LogDebug(Engine.LocalizationProvider.Current.Get("ui.button.disconnect", DateTime.Now));
            _ = transport.Disconnect();
        }

        /// <summary>
        /// Button to toggle full or partial redraw, will probably be hidden as full redraw is necessary with the current way of handling rendering
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FullRedrawButton_Click(object sender, EventArgs e)
        {
            terminalCtrl.AlwaysFullRedraw = !terminalCtrl.AlwaysFullRedraw;
            FullRedrawButton.ForeColor = terminalCtrl.AlwaysFullRedraw ? Color.Red : Color.Black;
        }

        /// <summary>
        /// Reconnect button (partially implemented but not working properly), will most likely be hidden
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReconnectButton_Click(object sender, EventArgs e)
        {
            this.LogDebug(Engine.LocalizationProvider.Current.Get("ui.button.reconnect", DateTime.Now));
            _ = transport.Reconnect(cts.Token);

        }

        /// <summary>
        /// Name or address of the host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HostTextBox_TextChanged(object sender, EventArgs e)
        {
            transportConfig.Host = HostTextBox.Text;
        }

        /// <summary>
        /// Port number on host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PortTextBox_TextChanged(Object sender, EventArgs e)
        {
            if (int.TryParse(PortTextBox.Text, out int port)) transportConfig.Port = port;
        }

        /// <summary>
        /// Combobox to change cursor format to one of the following: Block, horizontal bar (underscore) och vertical bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cursorStyleCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            switch (cursorStyleCombo.SelectedItem.ToString())
            {
                case "VerticalBar": uiConfig.CursorStylePreference = IRenderTarget.CursorStyle.VerticalBar; break;
                case "HorizontalBar": uiConfig.CursorStylePreference = IRenderTarget.CursorStyle.HorizontalBar; break;
                default: uiConfig.CursorStylePreference = IRenderTarget.CursorStyle.Block; break;
            }
            terminalCtrl.SetCursorStyle(uiConfig.CursorStylePreference, uiConfig.CursorBlink);
        }

        /// <summary>
        /// Checkbox to toggle cursor blink on or off
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BlinkBox_CheckedChanged(object sender, EventArgs e)
        {
            uiConfig.CursorBlink = terminalState.CursorBlink = (BlinkBox.Checked) ? true : false;
            terminalCtrl.SetCursorStyle(uiConfig.CursorStylePreference, uiConfig.CursorBlink);
            terminalCtrl.Focus();
        }

        /// <summary>
        /// UI localization, changes name on screen controls
        /// </summary>
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

        /// <summary>
        /// Check if there are logfiles older than a certain date and deletes those
        /// </summary>
        public static void LogFileCleanup()
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            var files = Directory.GetFiles(logDir, "PT200Emulator*.log");

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                if (info.CreationTime < DateTime.Now.AddDays(-7))
                {
                    try { info.Delete(); }
                    catch { /* Ignorera eller logga fel */ }
                }
            }
        }
    }

    /// <summary>
    /// Class for handling the status bar
    /// </summary>
    public class StatusLineController
    {
        private StatusStrip StatusLine;
        private ToolStripStatusLabel messageLabel, logLabel, onlineLabel, systemLabel, dsrLabel, g0g1Label;
        public TerminalCtrl TerminalCtrl { get; set; }
        public UiConfig UiConfig { get; set; }

        /// <summary>
        /// Constructor for the status line controller
        /// </summary>
        /// <param name="messagelabel"></param>
        /// <param name="loglabel"></param>
        /// <param name="online"></param>
        /// <param name="system"></param>
        /// <param name="dsr"></param>
        /// <param name="g0g1"></param>
        /// <param name="statusLine"></param>
        public StatusLineController(ToolStripStatusLabel messagelabel, ToolStripStatusLabel loglabel, ToolStripStatusLabel online, ToolStripStatusLabel system,
                                    ToolStripStatusLabel dsr, ToolStripStatusLabel g0g1, StatusStrip statusLine)
        {
            messageLabel = messagelabel;
            logLabel = loglabel;
            onlineLabel = online;
            systemLabel = system;
            dsrLabel = dsr;
            g0g1Label = g0g1;
            StatusLine = statusLine;
        }

        /// <summary>
        /// Set content of the Online filed
        /// </summary>
        /// <param name="online"></param>
        public void SetOnline(bool online)
        {
            onlineLabel.Text = online ? "ONLINE" : "OFFLINE";
            onlineLabel.ForeColor = online ? StatusLine.ForeColor : Color.Red;
            RePaint();
        }

        /// <summary>
        /// Set content of the SystemReady field
        /// </summary>
        /// <param name="ready"></param>
        /// <param name="lng"></param>
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
            systemLabel.ForeColor = ready ? StatusLine.ForeColor : StatusLine.BackColor;
            RePaint();
        }

        /// <summary>
        /// Set content of the DSR field
        /// </summary>
        /// <param name="xoff"></param>
        public void SetDsr(bool xoff)
        {
            dsrLabel.Text = xoff ? "XOFF" : "DSR";
            dsrLabel.ForeColor = xoff ? Color.Red : StatusLine.ForeColor;
            RePaint();
        }

        /// <summary>
        /// Set content of the charset field
        /// </summary>
        /// <param name="g1Active"></param>
        public void SetCharset(bool g1Active)
        {
            g0g1Label.Text = g1Active ? "G1" : "G0";
            RePaint();
        }

        /// <summary>
        /// Reflect current logging level on status line
        /// </summary>
        /// <param name="level"></param>
        public void SetLogLevel(string level)
        {
            logLabel.Text = level;
            RePaint();
        }

        /// <summary>
        /// Set content of the message field
        /// </summary>
        /// <param name="message"></param>
        public void SetMessage(string message)
        {
            messageLabel.Text = message;
            RePaint();
        }

        public void RePaint()
        {
            if (TerminalCtrl == null) return;
            TerminalCtrl.ForceRepaint();
        }
    }
}