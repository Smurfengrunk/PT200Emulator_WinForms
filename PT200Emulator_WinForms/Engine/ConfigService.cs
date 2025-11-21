#pragma warning disable CA1707

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using PT200_Parser;

using PT200_Rendering;

using Serilog.Events;

namespace PT200EmulatorWinforms.Engine
{
    /// <summary>
    /// Main class for configuration purposes
    /// </summary>
    public class ConfigService
    {
        private readonly string _configFolder;

        /// <summary>
        /// Constructor for ConfigService, checks if config directory exists and creates it if not
        /// </summary>
        /// <param name="configFolder"></param>
        public ConfigService(string configFolder)
        {
            _configFolder = configFolder;
            Directory.CreateDirectory(_configFolder);
        }

        /// <summary>
        /// LoadTransportConfig, LoadUiConfig, SaveTransportConfig and SaveUiConfig are lambdas that points to the corresponding method in TransportConfig oc UiConfig
        /// </summary>
        /// <returns></returns>
        public TransportConfig LoadTransportConfig() => TransportConfig.Load(_configFolder);
        public UiConfig LoadUiConfig() => UiConfig.Load(_configFolder);

        public void SaveTransportConfig(TransportConfig cfg) => cfg.Save(_configFolder);
        public void SaveUiConfig(UiConfig cfg) => cfg.Save(_configFolder);
    }

    /// <summary>
    /// Class to handle transport configuration
    /// </summary>
    public class TransportConfig
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 2323;

        /// <summary>
        /// Saves the transport configuration properties to json
        /// </summary>
        /// <param name="configFolder"></param>
        public void Save(string configFolder)
        {
            var filePath = Path.Combine(configFolder, "transportConfig.json");
            var json = JsonSerializer.Serialize(this, JsonCfg.Options);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads json file and populates properties
        /// </summary>
        /// <param name="configFolder"></param>
        /// <returns></returns>
        public static TransportConfig Load(string configFolder)
        {
            var filePath = Path.Combine(configFolder, "transportConfig.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<TransportConfig>(json, JsonCfg.Options) ?? new TransportConfig();
            }
            return new TransportConfig();
        }
    }

    public class UiConfig
    {
        public LogEventLevel DefaultLogLevel { get; set; } = LogEventLevel.Debug;
        public TerminalState.ScreenFormat ScreenFormat { get; set; } = TerminalState.ScreenFormat.S80x24;
        public TerminalState.DisplayType DisplayTheme { get; set; } = TerminalState.DisplayType.Green;
        public IRenderTarget.CursorStyle CursorStylePreference { get; set; } = IRenderTarget.CursorStyle.Block;
        public bool CursorBlink { get; set; }
        public bool DebugControls { get; set; }

        /// <summary>
        /// Saves the ui configuration properties to json
        /// </summary>
        /// <param name="configFolder"></param>
        public void Save(string configFolder)
        {
            var filePath = Path.Combine(configFolder, "uiConfig.json");
            var json = JsonSerializer.Serialize(this, JsonCfg.Options);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads ui configuration properties from json
        /// </summary>
        /// <param name="configFolder"></param>
        /// <returns></returns>
        public static UiConfig Load(string configFolder)
        {
            var filePath = Path.Combine(configFolder, "uiConfig.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var cfg = JsonSerializer.Deserialize<UiConfig>(json, JsonCfg.Options) ?? new UiConfig();
                //Serilog.Log.Debug(LocalizationProvider.Current.Get("cfg.load.dbg"), cfg.DisplayTheme, cfg.ScreenFormat, cfg.DefaultLogLevel, cfg.CursorStylePreference);
                return cfg;
            }
            return new UiConfig();
        }
    }

    /// <summary>
    /// Configuration parameters for json
    /// </summary>
    public static class JsonCfg
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}

