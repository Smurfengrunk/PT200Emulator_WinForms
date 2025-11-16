using Microsoft.VisualBasic;
using PT200_Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PT200Emulator_WinForms.Engine
{
    public class LocalizationProvider
    {
        private Dictionary<string, string> strings;
        private Dictionary<string, string> fallbackStrings;
        public string Language { get; }

        // Statisk "Current" instans som används globalt
        //public static LocalizationProvider Current { get; private set; }
        //    = new LocalizationProvider(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
        public static LocalizationProvider Current { get; private set; }
            = new LocalizationProvider("en");

        public LocalizationProvider(string languageCode = "sv")
        {
            Language = languageCode;
            strings = Load(languageCode);
            fallbackStrings ??= Load("en");
        }

        private Dictionary<string, string> Load(string code)
        {
            var path = Path.Combine("Resources", $"Strings.{code}.json");
            if (!File.Exists(path))
                return new Dictionary<string, string>();

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>();
            }
            catch
            {
                // Om något går fel, returnera tom dictionary
                var strmsg = strings.TryGetValue("loc.file.missing", out var msg) ? msg : "Language file missing: {0} ({1})";
                this.LogWarning(strmsg, path, Language);
                return new Dictionary<string, string>();
            }
        }

        public string Get(string key, params object[] args)
        {
            if (strings.TryGetValue(key, out var template))
                return Format(template, args);

            if (fallbackStrings.TryGetValue(key, out template))
                return Format(template, args);

            var strmsg = strings.TryGetValue("loc.key.missing", out var msg)
                ? msg
                : "Missing localization key: {0}";
            this.LogDebug("[LocalizationProvider.Get" + strmsg, key);
            return key;
        }

        private string Format(string template, object[] args)
        {
            if (args == null || args.Length == 0)
                return template; // ingen formatering behövs
            return string.Format(template, args);
        }
    }
}
