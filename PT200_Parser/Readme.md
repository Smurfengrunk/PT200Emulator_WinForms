# Parser‑modulen

Parsern ansvarar för att tolka inkommande byte‑strömmar och översätta dem till semantiska åtgärder (*TerminalActions*).  
Den exponerar följande publika API:

```csharp
public interface ITerminalParser
{
    void Feed(ReadOnlySpan<byte> data);

    event Action<byte[]> DcsResponse;

    ScreenBuffer Screenbuffer { get; }
}

// TerminalActions
public abstract record TerminalAction;
public record PrintText(string Text) : TerminalAction;
public record MoveCursor(int Row, int Col) : TerminalAction;
public record ClearScreen() : TerminalAction;
public record SetModeAction(string Mode) : TerminalAction;
public record DcsAction(string Content) : TerminalAction;
