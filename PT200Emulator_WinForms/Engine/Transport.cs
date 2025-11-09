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
using static System.Windows.Forms.AxHost;

namespace PT200Emulator_WinForms.Engine
{
    public class Transport
    {
        private TerminalParser _parser;
        private TerminalState _state = new();
        private IByteStream byteStream = new PT200_Transport.TelnetByteStream();
        private DataPathProvider _basePath = new DataPathProvider(AppDomain.CurrentDomain.BaseDirectory);
        private static LocalizationProvider _localization = new();
        private ModeManager modeManager = new ModeManager(_localization);
        private StatusLineController statusLine;
        private static string _host = "localhost";
        private static int _port = 2323;
        private static bool _connected = false;

        public Transport(StatusLineController _statusLine, TerminalCtrl _terminalCtrl)
        {
            statusLine = _statusLine;
            _state.screenFormat = TerminalState.ScreenFormat.S80x24;
            _state.SetScreenFormat();
            _parser = new TerminalParser(_basePath, _state, modeManager);
            _parser.DcsResponse += (bytes) => byteStream.WriteAsync(bytes);

            byteStream.Disconnected += async () =>
            {
                statusLine.SetOnline(false);
                this.LogInformation("Servern kopplade ner, stänger programmet.");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Servern kopplade ner, stänger programmet.");
                await Disconnected(_connected);
            };
            this.LogDebug("Transport initialiserad.");
        }

        public async Task Connect(CancellationToken cancellationToken, string host = "localhost", int port = 2323)
        {
            this.LogDebug($"Försöker ansluta till {host}:{port}...");
            _host = host;
            _port = port;
            if (await byteStream.ConnectAsync(host, port, cancellationToken))
            {
                statusLine.SetOnline(true);
                _connected = true;
                this.LogDebug($"Ansluten till {host}:{port}");
                if (byteStream == null) throw new InvalidOperationException("byteStream is null in Connect");
                this.LogDebug($"Registrerar mottagningshanterare. byteStream {byteStream}:{byteStream.GetHashCode()}, parser {_parser}:{_parser.GetHashCode()}");
                this.LogDebug($"Screenbuffer {_parser.Screenbuffer}:{_parser.Screenbuffer.GetHashCode()}, Size {_parser.Screenbuffer.Cols}x{_parser.Screenbuffer.Rows}");
                try
                {
                    byteStream.DataReceived += bytes =>
                    {
                        var parser = _parser ?? throw new InvalidOperationException("_parser is null when handling data");
                        this.LogDebug($"Mottagna data: längd {bytes.Length}, data \"{Encoding.ASCII.GetString(bytes)}\", bytes {BitConverter.ToString(bytes)}");
                        parser.Feed(bytes);
                    };
                }
                catch (Exception ex)
                {
                    this.LogErr($"Fel vid mottagning av data: {ex}");
                }
                this.LogDebug("Startar mottagningsloop...");
                try
                {
                    _ = byteStream.StartReceiveLoop(cancellationToken);
                    statusLine.SetSystemReady(true, (_state.Columns >= 132) ? true : false);
                }
                catch (Exception ex)
                {
                    this.LogErr($"Fel vid start av mottagningsloop: {ex}");
                    statusLine.SetSystemReady(false, false);
                }
            }
            this.LogDebug("Mottagningsloop startad.");
        }

        public void Send(byte[] data)
        {
            if (byteStream == null)
            {
                this.LogErr("byteStream är null vid försök att skicka data.");
                return;
            }
            this.LogDebug($"Skickar data: \"{Encoding.ASCII.GetString(data)}\" {BitConverter.ToString(data)}");
            byteStream.WriteAsync(data);
        }

        public async Task Disconnect()
        {
            try
            {
                var disconnectTask = byteStream.DisconnectAsync();
                var completed = await Task.WhenAny(disconnectTask, Task.Delay(2000));
                if (completed != disconnectTask)
                    this.LogWarning("Disconnect hängde, fortsätter ändå...");
                else statusLine.SetSystemReady(false, false);
            }
            catch (Exception ex)
            {
                this.LogErr($"Frånkoppling misslyckades: {ex}");
            }
        }

        private async static Task Disconnected(bool connected)
        {
            var msgboxText = connected ? $"Anslutningen till {_host}:{_port} har brutits. Programmet stängs ner." : $"Kunde inte ansluta till {_host}:{_port}. Programmet stängs ner.";
            MessageBox.Show(msgboxText, "Nedkoppling", MessageBoxButtons.OK, MessageBoxIcon.Error);
            await Task.Delay(5000);
            Environment.Exit(0);
        }

        public async Task Reconnect(CancellationToken cancellationToken)
        {
            this.LogInformation($"Försöker återansluta till {_host}:{_port}...");
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
