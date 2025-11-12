# Transport‑modulen

Transport‑modulen ansvarar för att etablera och hantera anslutningar mot en server, samt skicka och ta emot råa byte‑strömmar.  
Den exponerar ett neutralt interface (`IByteStream`) som kan implementeras med olika transportlager (t.ex. TCP, seriell port, mock för tester).

---

## Publikt API

### Interface: `IByteStream`
```csharp
public interface IByteStream : IDisposable
{
    /// <summary>
    /// Etablerar en anslutning till given host och port.
    /// </summary>
    Task<bool> ConnectAsync(string host, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kopplar ned anslutningen.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Startar vänteloop för data från servern.
    /// </summary>
    Task StartReceiveLoop(CancellationToken cancellationToken = default);

    /// <summary>
    /// Skickar data som råa bytes.
    /// </summary>
    Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default);

    ///<summary>
    /// Event för inkommande data.
    ///</summary>
    event Action<byte[]> DataReceived;

    ///<summary>
    /// Event som triggas när anslutningen stängs.
    ///</summary>
    event Action Disconnected;
}
