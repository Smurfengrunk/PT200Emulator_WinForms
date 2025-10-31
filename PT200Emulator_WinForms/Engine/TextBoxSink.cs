using Serilog.Core;
using Serilog.Events;
using System;
using System.Windows.Forms;

public class TextBoxSink : ILogEventSink
{
    private readonly TextBox _textBox;
    private readonly IFormatProvider _formatProvider;

    public TextBoxSink(TextBox textBox, IFormatProvider formatProvider = null)
    {
        _textBox = textBox;
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage(_formatProvider);

        // Eftersom loggning kan ske från bakgrundstrådar:
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