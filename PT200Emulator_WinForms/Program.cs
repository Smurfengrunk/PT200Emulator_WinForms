#pragma warning disable CA1707

using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Globalization;
using System.Runtime.InteropServices;

namespace PT200EmulatorWinforms
{
    /// <summary>
    /// Initializing logging configuration and starting application
    /// </summary>
    static class Program
    {
        internal static LogForm logForm;
        private static readonly LoggingLevelSwitch LevelSwitch = new LoggingLevelSwitch(LogEventLevel.Debug);

        [STAThread]
        
        static void Main(string[] args)
        {
            var loggerConfiguration = new LoggerConfiguration();
            Application.SetCompatibleTextRenderingDefault(false);
            logForm = new LogForm();
            logForm.Hide();
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            var logFile = Path.Combine(logDir, "PT200Emulator.log");
            Log.Logger = loggerConfiguration
                .MinimumLevel.ControlledBy(LevelSwitch)
                .WriteTo.File(logFile,
                              rollingInterval: RollingInterval.Minute,
                              formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.Debug(formatProvider: CultureInfo.CurrentCulture)
                .WriteTo.Sink(new TextBoxSink(logForm.logTextBox))
                .CreateLogger();            //logForm.Show(); // eller Show(this) om du vill att det ska vara ett barnfönster

            try
            {
                Application.EnableVisualStyles();
                Log.Information("Logger initialized"); // test
                Application.Run(new PT200(LevelSwitch)); // skicka in switchen om du vill
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
