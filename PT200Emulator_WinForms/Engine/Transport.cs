using PT200_Logging;
using PT200_Parser;
using PT200_Rendering;
using PT200_Transport;
using PT200Emulator_WinForms.Controls;
using PT200Emulator_WinForms.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;
using static PT200Emulator_WinForms.Controls.TerminalCtrl;
using static PT200Emulator_WinForms.Controls.WinFormsRenderTarget;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace PT200Emulator_WinForms.Engine
{
    public class Transport
    {
        private TerminalParser _parser;
        private TerminalState _state = new(TerminalState.ScreenFormat.S80x48, TerminalState.DisplayType.Green);
        private IByteStream byteStream = new PT200_Transport.TelnetByteStream();
        private DataPathProvider _basePath = new DataPathProvider(AppDomain.CurrentDomain.BaseDirectory);
        private ModeManager modeManager = new ModeManager(new PT200_Parser.LocalizationProvider());
        private StatusLineController statusLine;
        private static string _host = "localhost";
        private static int _port = 2323;
        private static bool _connected = false;

        public Transport(StatusLineController _statusLine, TerminalCtrl _terminalCtrl)
        {
            statusLine = _statusLine;
            _state._screenFormat = TerminalState.ScreenFormat.S80x24;
            _state.SetScreenFormat();
            _parser = new TerminalParser(_basePath, _state, modeManager);
            _parser.DcsResponse += (bytes) => byteStream.WriteAsync(bytes);

            byteStream.Disconnected += async () =>
            {
                statusLine.SetOnline(false);
                this.LogInformation(LocalizationProvider.Current.Get("dialog.disconnect.error.connected", _host, _port));
                await Disconnected(_connected);
            };
        }

        public async Task Connect(CancellationToken cancellationToken, string host = "localhost", int port = 2323)
        {
            this.LogDebug(LocalizationProvider.Current.Get("log.transport.connecting", host, port));
            _host = host;
            _port = port;
            if (await byteStream.ConnectAsync(host, port, cancellationToken))
            {
                statusLine.SetOnline(true);
                _connected = true;
                this.LogDebug(LocalizationProvider.Current.Get("log.transport.connected", host, port));
                if (byteStream == null) throw new InvalidOperationException("byteStream is null in Connect");
                try
                {
                    byteStream.DataReceived += bytes =>
                    {
                        var parser = _parser ?? throw new InvalidOperationException("_parser is null when handling data");
                        parser.Feed(bytes);
                    };
                }
                catch (Exception ex)
                {
                    this.LogErr(LocalizationProvider.Current.Get("log.transport.receive.error", ex));
                }
                this.LogDebug(LocalizationProvider.Current.Get("log.transport.loop.starting"));
                try
                {
                    _ = byteStream.StartReceiveLoop(cancellationToken);
                    statusLine.SetSystemReady(true, (_state.Columns >= 132) ? true : false);
                }
                catch (Exception ex)
                {
                    this.LogDebug(LocalizationProvider.Current.Get("log.transport.loop.error"), ex);
                    statusLine.SetSystemReady(false, false);
                }
            }
            this.LogDebug(LocalizationProvider.Current.Get("log.transport.loop.started"));
        }

        public void Send(byte[] data)
        {
            if (byteStream == null) return;
            byteStream.WriteAsync(data);
        }

        public async Task Disconnect()
        {
            string strmsg = null;
            try
            {
                var disconnectTask = byteStream.DisconnectAsync();
                var completed = await Task.WhenAny(disconnectTask, Task.Delay(2000));
                if (completed != disconnectTask)
                {
                    strmsg = LocalizationProvider.Current.Get("log.transport.disconnect.hang");
                    this.LogDebug(strmsg);
                }
                else statusLine.SetSystemReady(false, false);
            }
            catch (Exception ex)
            {
                strmsg = LocalizationProvider.Current.Get("log.transport.loop.starting", ex);
                this.LogDebug(strmsg);
            }
        }

        private async static Task Disconnected(bool connected)
        {
            MessageBox.Show(LocalizationProvider.Current.Get("dialog.disconnect.error.connected", _host, _port), LocalizationProvider.Current.Get("dialog.disconnect.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            await Task.Delay(5000);
            Environment.Exit(0);
        }

        public async Task Reconnect(CancellationToken cancellationToken)
        {
            this.LogInformation(LocalizationProvider.Current.Get("log.transport.reconnecting", _host, _port));
            await Disconnect();
            await Task.Delay(500); // liten paus för att frigöra socket
            await Connect(cancellationToken, _host, _port);
        }

        public TerminalParser GetParser()
        {
            return _parser;
        }
    }
}
