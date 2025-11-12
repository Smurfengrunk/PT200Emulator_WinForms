# Rendering‑modulen

Rendering‑modulen ansvarar för att visualisera terminalens skärmbuffert.  
Den tar emot ett `ScreenBuffer` från Parser och ritar ut innehållet i en konsol, inklusive text, färger, attribut och caret‑position.

---

## Publika klasser

### `ConsoleRenderer`
```csharp
public class ConsoleRenderer
{
    public bool Connected { get; set; }
    public bool inEmacs { get; set; }

    public void ForceFullRender();
    public void Render(ScreenBuffer buffer, bool inEmacs);
}

public class CaretController : ICaretController
{
    public int Row { get; private set; }
    public int Col { get; private set; }
    public bool Visible { get; private set; }

    public void SetCaretPosition(int row, int col);
    public void MoveCaret(int rowDelta, int colDelta);
    public void Show();
    public void Hide();
}

public class ConsoleCaretController : ICaretController
{
    public void SetCaretPosition(int row, int col);
    public void MoveCaret(int dRow, int dCol);
    public void Show();
    public void Hide();
}

## Status

För närvarande används `ConsoleRenderer` som en test‑ och referensimplementation.  
Den är inte avsedd att ingå i den färdiga produkten, utan fungerar som ett verktyg för att verifiera Parser och ScreenBuffer under utveckling.
