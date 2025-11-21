using PT200_Logging;
using PT200_Parser;
using PT200_Transport;
using PT200EmulatorWinforms.Controls;

namespace PT200EmulatorWinforms.Engine
{
    /// <summary>
    /// Class that acts as bridge between the UI and PT200_Transport for Telnet connection
    /// </summary>
    public class Transport : IDisposable
    {
        private TerminalParser _parser;
        private TerminalState _state = new(TerminalState.ScreenFormat.S80x48, TerminalState.DisplayType.Green);
        private TelnetByteStream byteStream = new PT200_Transport.TelnetByteStream();
        private DataPathProvider _basePath = new DataPathProvider(AppDomain.CurrentDomain.BaseDirectory);
        private ModeManager modeManager = new ModeManager(new PT200_Parser.LocalizationProvider());
        private StatusLineController _statusLine;
        private static string _host = "localhost";
        private static int _port = 2323;
        private static bool _connected;

        /// <summary>
        /// Constructor for Transport class, establish link to the statusline to update conmmunocation fields
        /// </summary>
        /// <param name="statusLine"></param>
        public Transport(StatusLineController statusLine)
        {
            _statusLine = statusLine;
            _state._screenFormat = TerminalState.ScreenFormat.S80x24;
            _state.SetScreenFormat();
            _parser = new TerminalParser(_basePath, _state, modeManager);
            _parser.DcsResponse += (bytes) => byteStream.WriteAsync(bytes);

            byteStream.Disconnected += async () =>
            {
                this.LogInformation(LocalizationProvider.Current.Get("dialog.disconnect.error.connected", _host, _port));
                await Disconnected(_connected);
            };
        }

        /// <summary>
        /// Connect establishes the Telnet connection and uses the supplied byte stream to start receive loop
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task Connect(CancellationToken cancellationToken, string host = "localhost", int port = 2323)
        {
            this.LogDebug(LocalizationProvider.Current.Get("log.transport.connecting", host, port));
            _host = host;
            _port = port;
            if (await byteStream.ConnectAsync(host, port, cancellationToken))
            {
                _statusLine.SetOnline(true);
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
                    _statusLine.SetSystemReady(true, (_state.Columns >= 132) ? true : false);
                }
                catch (Exception ex)
                {
                    this.LogDebug(LocalizationProvider.Current.Get("log.transport.loop.error"), ex);
                    _statusLine.SetSystemReady(false, false);
                }
            }
            this.LogDebug(LocalizationProvider.Current.Get("log.transport.loop.started"));
        }

        /// <summary>
        /// Sends the supplied byte array to the established byte stream
        /// </summary>
        /// <param name="data"></param>
        public void Send(byte[] data)
        {
            if (byteStream == null) return;
            byteStream.WriteAsync(data);
        }

        /// <summary>
        /// Disconnects the Telnet connection
        /// </summary>
        /// <returns></returns>
        public async Task Disconnect()
        {
            string strmsg = null;
            try
            {
                var disconnectTask = byteStream.DisconnectAsync();
                var completed = await Task.WhenAny(disconnectTask, Task.Delay(500));
                if (completed != disconnectTask)
                {
                    strmsg = LocalizationProvider.Current.Get("log.transport.disconnect.hang");
                    this.LogDebug(strmsg);
                }
                else _statusLine.SetSystemReady(false, false);
            }
            catch (Exception ex)
            {
                strmsg = LocalizationProvider.Current.Get("log.transport.loop.starting", ex);
                this.LogDebug(strmsg);
            }
        }

        /// <summary>
        /// Opens a MessageBox to inform the user of the closed down connection, waits for five seconds
        /// </summary>
        /// <param name="connected"></param>
        /// <returns></returns>
        private async static Task Disconnected(bool connected)
        {
            MessageBox.Show(LocalizationProvider.Current.Get("dialog.disconnect.error.connected", _host, _port), LocalizationProvider.Current.Get("dialog.disconnect.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            await Task.Delay(5000);
        }

        /// <summary>
        /// Disconnects the current Telnet connection, waits for a half second and then tries to re-establish a connection with the actual host and port
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Reconnect(CancellationToken cancellationToken)
        {
            this.LogInformation(LocalizationProvider.Current.Get("log.transport.reconnecting", _host, _port));
            await Disconnect();
            await Task.Delay(500, cancellationToken); // liten paus för att frigöra socket
            await Connect(cancellationToken, _host, _port);
        }

        /// <summary>
        /// Returns the current terminal parser instance for use in other modules
        /// </summary>
        /// <returns></returns>
        public TerminalParser GetParser()
        {
            return _parser;
        }

        /// <summary>
        /// Clean up after disconnect
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
