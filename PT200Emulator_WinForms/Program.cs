using PT200Emulator_WinForms;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace PT200Emulator_WinForms
{
    static public class Program
    {
        internal static LogForm logForm;
        private static readonly LoggingLevelSwitch LevelSwitch = new LoggingLevelSwitch(LogEventLevel.Debug);

        [STAThread]
        static void Main()
        {
            Application.SetCompatibleTextRenderingDefault(false);
            logForm = new LogForm();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LevelSwitch) // <-- styrs av switchen
                .WriteTo.File("PT200Emulator.log", rollingInterval: RollingInterval.Minute)
                .WriteTo.Debug()
                .WriteTo.Sink(new TextBoxSink(logForm.logTextBox))
                .CreateLogger();
            //logForm.Show(); // eller Show(this) om du vill att det ska vara ett barnfönster

            try
            {
                Application.EnableVisualStyles();
                Application.Run(new PT200(LevelSwitch)); // skicka in switchen om du vill
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
