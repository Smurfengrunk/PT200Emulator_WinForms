# InputHandler‑modulen

InputHandler ansvarar för att ta emot tangenttryckningar och översätta dem till de byte‑sekvenser som terminalen förväntar sig.  
Modulen är frikopplad från specifika UI‑ramverk (t.ex. WPF eller Console) och exponerar ett neutralt API.

---

## Publikt API

### Interface: `IInputHandler`
```csharp
public interface IInputHandler
{
    Task StartAsync(CancellationToken cancellationToken);
}
namespace InputHandler
{
    public interface IInputMapper
    {
        /// <summary>
        /// Mappar en tangenttryckning (scancode + modifiers) till en escape-sekvens.
        /// Returnerar null om ingen mappning finns.
        /// </summary>
        byte[]? MapKey(KeyEvent e);

        /// <summary>
        /// Mappar en textsträng till motsvarande byte-sekvens (UTF-8).
        /// </summary>
        byte[]? MapText(string text);
    }
}
public class KeyEvent
{
    public int ScanCode { get; }
    public KeyModifiers Modifiers { get; }

    public KeyEvent(int scanCode, KeyModifiers modifiers)
    {
        ScanCode = scanCode;
        Modifiers = modifiers;
    }
}

[Flags]
public enum KeyModifiers
{
    None = 0,
    Shift = 1,
    Ctrl = 2,
    Alt = 3,
    AltGr = 4
}

public enum TerminalModes
{
    Default
}
