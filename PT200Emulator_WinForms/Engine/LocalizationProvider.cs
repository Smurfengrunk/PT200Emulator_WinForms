#pragma warning disable CA1707
using PT200_Logging;
using System.Globalization;
using System.Text.Json;

namespace PT200EmulatorWinForms.Engine
{
    /// <summary>
    /// Class to handle localization
    /// </summary>
    public class LocalizationProvider
    {
        private Dictionary<string, string> strings;
        private Dictionary<string, string> fallbackStrings;
        public string Language { get; }

        /// <summary>
        /// Static reference to simplify reference to localization strings
        /// </summary>
        public static LocalizationProvider Current { get; private set; }
            = new LocalizationProvider("en");

       /// <summary>
       /// Constructor for the LocalizationProvider class, sets the Language property, loads the actual localization json for the given language and if that fails load the english messages
       /// </summary>
       /// <param name="languageCode"></param>
       public LocalizationProvider(string languageCode = "sv")
        {
            Language = languageCode;
            strings = Load(languageCode);
            fallbackStrings ??= Load("en");
        }

        /// <summary>
        /// Load the strings from the config file for the given language and populate dictionary
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Retrieves the localized string for the given key with the given args if available
        /// </summary>
        /// <param name="key"></param>
        /// <param name="args"></param>
        /// <returns>Formatted string for the actual language or english if it doesn't exist or an error message if key isn't found at all</returns>
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

        /// <summary>
        /// Formats the return string as either the given message or message with params if supplied
        /// </summary>
        /// <param name="template"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static string Format(string template, object[] args)
        {
            if (args == null || args.Length == 0)
                return template; // ingen formatering behövs
            return String.Format(CultureInfo.InvariantCulture, template, args);
        }
    }
}
