# Logging‑modulen

Logging‑modulen tillhandahåller extension‑metoder för enkel och konsekvent loggning i hela projektet.  
Den bygger på [Serilog](https://serilog.net/) och exponerar ett neutralt API som kan användas från vilken klass som helst.

---

## Publikt API

### Extension‑metoder (klass: `LogExtensions`)
```csharp
public static void LogDebug(this object _, string message, params object[] args);
public static void LogInformation(this object _, string message, params object[] args);
public static void LogWarning(this object _, string message, params object[] args);
public static void LogErr(this object _, string message, params object[] args);
public static void LogTrace(this object _, string message, params object[] args);

public static void LogStackTrace(
    this object caller,
    string message = "Stacktrace",
    bool includeDotNet = false,
    int maxFrames = 20);

public static class CallOriginTracker
{
    public static string GetCallStack(
        bool includeDotNet = false,
        int maxFrames = 20);
}
