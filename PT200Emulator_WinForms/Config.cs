#pragma warning disable CA1707

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using PT200_Logging;

using PT200_Parser;

using PT200_Rendering;

using PT200EmulatorWinForms;
using PT200EmulatorWinForms.Controls;
using PT200EmulatorWinForms.Engine;

using Serilog.Core;
using Serilog.Events;

using static System.Windows.Forms.DataFormats;

namespace PT200EmulatorWinForms
{
    public partial class Config : Form
    {
        private ConfigService configService = new ConfigService(AppDomain.CurrentDomain.BaseDirectory + "Config");
        private TransportConfig transportConfig;
        private UiConfig uiConfig;
        private PT200 PT200;
        private LoggingLevelSwitch LevelSwitch;
        private TerminalState terminalState;
        private TerminalCtrl terminalCtrl;
        private Transport Transport;
        private CancellationTokenSource cts = new();

        private bool initializing;
        private StatusLineController statusController => PT200.statusController;
        private BindingSource uiConfigBinding;

        public Config(PT200 pt200, LoggingLevelSwitch levelSwitch, TerminalState state, TerminalCtrl ctrl, Transport transport)
        {
            PT200 = pt200;
            LevelSwitch = levelSwitch;
            terminalState = state;
            terminalCtrl = ctrl;
            Transport = transport;

            InitializeComponent();
            transportConfig = configService.LoadTransportConfig();
            uiConfig = configService.LoadUiConfig();
            uiConfigBinding = new BindingSource();
            uiConfigBinding.DataSource = uiConfig;

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
            HostTextBox.Text = transportConfig.Host;
            PortTextBox.Text = transportConfig.Port.ToString(CultureInfo.InvariantCulture);
            InitLocalizedUI();

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
            initializing = true;
            CursorStyleCombo.DataSource = Enum.GetValues<IRenderTarget.CursorStyle>();
            CursorStyleCombo.SelectedItem = uiConfig.CursorStylePreference;
            initializing = false;
            CursorStyleCombo.DataBindings.Add("SelectedItem", uiConfigBinding,
                                              nameof(UiConfig.CursorStylePreference),
                                              true, DataSourceUpdateMode.OnPropertyChanged);
            TerminalState.DisplayType displayType = uiConfig.DisplayTheme;
            switch (uiConfig.DisplayTheme)
            {
                case TerminalState.DisplayType.White:
                    {
                        rbWhite.Checked = true;
                        break;
                    }
                case TerminalState.DisplayType.Blue:
                    {
                        rbBlue.Checked = true;
                        break;
                    }
                case TerminalState.DisplayType.Green:
                    {
                        rbGreen.Checked = true;
                        break;
                    }
                case TerminalState.DisplayType.Amber:
                    {
                        rbAmber.Checked = true;
                        break;
                    }
                case TerminalState.DisplayType.FullColor:
                    {
                        rbColor.Checked = true;
                        break;
                    }
                default: break;
            }

            BlinkBox.Checked = uiConfig.CursorBlink;
            FullRedrawButton.ForeColor = terminalCtrl.AlwaysFullRedraw ? Color.Red : Color.Black;
            Transport = transport;
            terminalCtrl.Focus();
        }

        /// <summary>
        /// Code to change the screen color to classic green
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbGreen_CheckedChanged(object sender, EventArgs e)
        {
            PT200.RePaint(StyleInfo.Color.Green, DimColor(Color.LimeGreen), StyleInfo.Color.DarkGreen);
            uiConfig.DisplayTheme = TerminalState.DisplayType.Green;
        }

        /// <summary>
        /// Code to change the screen color to classic amber
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbAmber_CheckedChanged(object sender, EventArgs e)
        {
            PT200.RePaint(StyleInfo.Color.Yellow, DimColor(Color.Orange), StyleInfo.Color.DarkYellow);
            uiConfig.DisplayTheme = TerminalState.DisplayType.Amber;
        }

        /// <summary>
        /// Code to change the screen color to classic white (well, almost...)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbWhite_CheckedChanged(object sender, EventArgs e)
        {
            PT200.RePaint(StyleInfo.Color.White, DimColor(Color.White), StyleInfo.Color.Black);
            uiConfig.DisplayTheme = TerminalState.DisplayType.White;
        }

        /// <summary>
        /// Code to change the screen color to HP-style "white"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbBlue_CheckedChanged(object sender, EventArgs e)
        {
            PT200.RePaint(StyleInfo.Color.Blue, DimColor(Color.FromArgb(180, 200, 230)), StyleInfo.Color.Black);
            uiConfig.DisplayTheme = TerminalState.DisplayType.Blue;
        }

        /// <summary>
        /// Code to change the screen color to full color (partially implemented)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbColor_CheckedChanged(object sender, EventArgs e)
        {
            PT200.RePaint(StyleInfo.Color.Default, DimColor(Color.Wheat), StyleInfo.Color.Black);
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
                if (terminalState.Columns >= 132) PT200.statusController.SetSystemReady(true, true);
                else PT200.statusController.SetSystemReady(true, false);
                PT200.layoutPanel.PerformLayout();
                this.PerformLayout();
                PT200.ResizeWindowToFit(terminalState.Columns, terminalState.Rows); // beräknar storlek baserat på layout
                uiConfig.ScreenFormat = format;
                PT200.statusController.SetSystemReady((PT200.onlineLabel.Text == "ONLINE") ? true : false, (terminalState.Columns >= 132) ? true : false);
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
        /// Button to connect to host (partially implenmented)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectButton_Click(object sender, EventArgs e)
        {
            this.LogDebug(PT200EmulatorWinForms.Engine.LocalizationProvider.Current.Get("ui.button.connect", DateTime.Now));
            _ = Transport.Connect(cts.Token);

        }

        /// <summary>
        /// Button to disconnect from host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisconnectButton_Click(object sender, EventArgs e)
        {
            this.LogDebug(PT200EmulatorWinForms.Engine.LocalizationProvider.Current.Get("ui.button.disconnect", DateTime.Now));
            _ = Transport.Disconnect();
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
            this.LogDebug(PT200EmulatorWinForms.Engine.LocalizationProvider.Current.Get("ui.button.reconnect", DateTime.Now));
            _ = Transport.Reconnect(cts.Token);

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
        private void CursorStyleCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            switch (CursorStyleCombo.SelectedItem.ToString())
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
        /// UI localization, changes name on screen controls
        /// </summary>
        private void InitLocalizedUI()
        {
            rbAmber.Text = PT200EmulatorWinForms.Engine.LocalizationProvider.Current.Get("ui.color.amber");
            rbBlue.Text = PT200EmulatorWinForms.Engine.LocalizationProvider.Current.Get("ui.color.blue");
            rbColor.Text = PT200EmulatorWinForms.Engine.LocalizationProvider.Current.Get("ui.color.color");
            rbGreen.Text = PT200EmulatorWinForms.Engine.LocalizationProvider.Current.Get("ui.color.green");
            rbWhite.Text = PT200EmulatorWinForms.Engine.LocalizationProvider.Current.Get("ui.color.white");
            ConnectButton.Text = PT200EmulatorWinForms.Engine.LocalizationProvider.Current.Get("ui.button.connect");
            DisconnectButton.Text = PT200EmulatorWinForms.Engine.LocalizationProvider.Current.Get("ui.button.disconnect");
            ReconnectButton.Text = PT200EmulatorWinForms.Engine.LocalizationProvider.Current.Get("ui.button.reconnect");
            BlinkBox.Text = PT200EmulatorWinForms.Engine.LocalizationProvider.Current.Get("ui.blinkbox.label");
            FullRedrawButton.Text = PT200EmulatorWinForms.Engine.LocalizationProvider.Current.Get("ui.button.fullredraw");
            DiagButton.Text = PT200EmulatorWinForms.Engine.LocalizationProvider.Current.Get("ui.button.diag");
        }
    }
}