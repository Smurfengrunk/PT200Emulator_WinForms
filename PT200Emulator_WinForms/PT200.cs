using PT200_InputHandler;

using PT200_Logging;

using PT200_Parser;

using PT200_Rendering;

using PT200EmulatorWinForms.Controls;
using PT200EmulatorWinForms.Engine;

using PT200EmulatorWinForms;

using Serilog;
using Serilog.Core;
using Serilog.Events;

using System.Globalization;
using System.Windows.Forms.Design;

namespace PT200EmulatorWinForms
{
    /// <summary>
    /// Main Form for the PT200 Emulator
    /// </summary>
    public partial class PT200 : Form
    {
        private System.Windows.Forms.Timer _clockTimer = new();
        internal StatusLineController statusController;
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
        private VisualAttributeManager visualAttributeManager;
        private Config Config;
        private bool show_config;

        /// <summary>
        /// Main form constructor. Initializing form members and initializing Transport and Parser.
        /// The Parameter levelSwitch is initialized in Program.cs, and sets the default logging level
        /// </summary>
        /// <param name="levelSwitch"></param>
        public PT200(LoggingLevelSwitch levelSwitch)
        {
            InitializeComponent();
            LevelSwitch = levelSwitch;
            configService = new ConfigService(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config"));
            transportConfig = configService.LoadTransportConfig();
            uiConfig = configService.LoadUiConfig();
            uiConfigBinding = new BindingSource { DataSource = uiConfig };

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
            layoutPanel.Controls.Add(terminalHost, 0, 0);

            transport = new Transport(statusController);
            terminalState = parser.termState;
            terminalState._screenFormat = uiConfig.ScreenFormat;
            terminalState.SetScreenFormat();
            visualAttributeManager = parser.visualAttributeManager;
            visualAttributeManager.DisplayTypeChanged += () => terminalState.Display = uiConfig.DisplayTheme;

            terminalCtrl = new TerminalCtrl(transport, uiConfig);
            Config = new Config(this, levelSwitch, terminalState, terminalCtrl, transport);
            statusController.TerminalCtrl = terminalCtrl;
            terminalCtrl.Margin = new Padding(3, 3, 0, 0);
            this.LogDebug(Engine.LocalizationProvider.Current.Get("log.terminal.init"));
            terminalCtrl.Dock = DockStyle.Fill;
            terminalHost.Controls.Add(terminalCtrl);

            terminalCtrl.Dock = DockStyle.Fill;
            StatusLine.Renderer = new ToolStripProfessionalRenderer(new ProfessionalColorTable
            {
                // neutralisera kantfärger
                UseSystemColors = false
            });
            StatusLine.Dock = DockStyle.Fill;
            ResizeWindowToFit(terminalState.Columns, terminalState.Rows);

            this.KeyPreview = true;
            this.KeyUp += MainForm_KeyUp;

            statusController.SetLogLevel(Config.LogLevelCombo.SelectedItem.ToString());
            terminalCtrl.SetCursorStyle(uiConfig.CursorStylePreference, uiConfig.CursorBlink);
            LevelSwitch = levelSwitch;
            inputHandler = new();
            inputMapper = inputHandler.inputMapper;
            terminalCtrl.InputMapper = inputMapper;
            layoutPanel.PerformLayout();
            this.PerformLayout();
            terminalCtrl.Focus();
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
        /// RePaint needs to be called for changing color on text and status line when changing screen color
        /// </summary>
        /// <param name="TextColor"></param>
        /// <param name="StatusBarColor"></param>
        /// <param name="BackColor"></param>
        public void RePaint(StyleInfo.Color TextColor, Color StatusBarColor, StyleInfo.Color BackColor)
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
            if (e.KeyCode == Keys.S && e.Control && e.Shift)
            {
                if (show_config)
                {
                    Config.Hide();
                    show_config = false;
                }
                else
                {
                    show_config = true;
                    Config.Show();
                }
            }
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
            terminalCtrl.Size = new Size(termWidth, termHeight);

            int statusHeight = StatusLine.GetPreferredSize(Size.Empty).Height;
            int statusWidth = StatusLine.GetPreferredSize(Size.Empty).Width;
            StatusLine.Size = new Size(Math.Max(termWidth, statusWidth), statusHeight);

            int totalHeight = termHeight + statusHeight + layoutPanel.Padding.Vertical + terminalHost.Margin.Vertical;
            this.Size = new Size(termWidth + layoutPanel.Padding.Horizontal + terminalHost.Margin.Horizontal + this.Width - this.ClientSize.Width,
                             totalHeight + this.Height - this.ClientSize.Height);
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