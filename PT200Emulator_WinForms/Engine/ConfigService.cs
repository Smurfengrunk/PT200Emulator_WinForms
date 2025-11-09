using PT200_Logging;
using PT200_Parser;
using PT200_Rendering;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PT200Emulator_WinForms.Engine
{
    public class ConfigService
    {
        private readonly string _configFolder;

        public ConfigService(string configFolder)
        {
            _configFolder = configFolder;
            Directory.CreateDirectory(_configFolder);
        }

        public TransportConfig LoadTransportConfig() => TransportConfig.Load(_configFolder);
        public UiConfig LoadUiConfig() => UiConfig.Load(_configFolder);

        public void SaveTransportConfig(TransportConfig cfg) => cfg.Save(_configFolder);
        public void SaveUiConfig(UiConfig cfg) => cfg.Save(_configFolder);
    }

    public class TransportConfig
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 2323;

        public void Save(string configFolder)
        {
            var filePath = Path.Combine(configFolder, "transportConfig.json");
            var json = JsonSerializer.Serialize(this, JsonCfg.Options);
            File.WriteAllText(filePath, json);
        }

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

        public void Save(string configFolder)
        {
            var filePath = Path.Combine(configFolder, "uiConfig.json");
            var json = JsonSerializer.Serialize(this, JsonCfg.Options);
            File.WriteAllText(filePath, json);
        }

        public static UiConfig Load(string configFolder)
        {
            var filePath = Path.Combine(configFolder, "uiConfig.json");
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var cfg = JsonSerializer.Deserialize<UiConfig>(json, JsonCfg.Options) ?? new UiConfig();
                Serilog.Log.Debug($"[UiConfig Load] Loaded values: Theme={cfg.DisplayTheme}, Format={cfg.ScreenFormat}, LogLevel={cfg.DefaultLogLevel}, Cursor={cfg.CursorStylePreference}");
                return cfg;
            }
            return new UiConfig();
        }
    }

    public static class JsonCfg
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}

