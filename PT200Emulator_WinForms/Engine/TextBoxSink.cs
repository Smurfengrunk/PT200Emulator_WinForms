using Serilog.Core;
using Serilog.Events;

namespace PT200EmulatorWinforms
{
    /// <summary>
    /// Textbox sink for the logging form
    /// </summary>
    public class TextBoxSink : ILogEventSink
    {
        private readonly TextBox _textBox;
        private readonly IFormatProvider _formatProvider;

        /// <summary>
        /// Constructor for the sink, sets relevant properties to values supplied
        /// </summary>
        /// <param name="textBox"></param>
        /// <param name="formatProvider"></param>
        public TextBoxSink(TextBox textBox, IFormatProvider formatProvider = null)
        {
            _textBox = textBox;
            _formatProvider = formatProvider;
        }

        /// <summary>
        /// Checks that the form is open, and enters the supplied log information to the textbox either directly or with invoke depending on log source
        /// </summary>
        /// <param name="logEvent"></param>
        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage(_formatProvider);

            // Eftersom loggning kan ske från bakgrundstrådar:
            if (!Program.logForm.IsDisposed)
            {
                if (_textBox.InvokeRequired)
                {
                    _textBox.BeginInvoke(new Action(() =>
                        _textBox.AppendText($"{logEvent.Timestamp:HH:mm:ss} [{logEvent.Level}] {message}{Environment.NewLine}")
                    ));
                }
                else
                {
                    _textBox.AppendText($"{logEvent.Timestamp:HH:mm:ss} [{logEvent.Level}] {message}{Environment.NewLine}");
                }
            }
        }
    }
}