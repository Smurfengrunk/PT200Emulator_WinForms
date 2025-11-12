using PT200_Logging;
using static PT200_Parser.TerminalState;

namespace PT200_Parser
{
    public class VisualAttributeManager
    {
        public event Action DisplayTypeChanged;

        public void ChangeDisplayAttributes(int scope, string[] parameters, ScreenBuffer buffer, TerminalState terminal)
        {
            // Uppdatera aktuell stil
            if (parameters.Length > 0) HandleSGR(parameters, buffer, terminal);

            // Måla om området med den nya stilen
            ApplyStyleToScope(scope, buffer, buffer.CurrentStyle.Clone());
        }

        public void ApplyStyleToScope(int scope, ScreenBuffer buffer, StyleInfo style)
        {
            (int startRow, int startCol, int endRow, int endCol) = CalculateScope(scope, buffer);

            for (int row = startRow; row <= endRow; row++)
            {
                for (int col = (row == startRow ? startCol : 0);
                         col <= (row == endRow ? endCol : buffer.Cols - 1);
                         col++)
                {
                    buffer.ZoneAttributes[row, col] = style;
                }
            }
        }

        private (int, int, int, int) CalculateScope(int scope, ScreenBuffer buffer)
        {
            int startRow, startCol, endRow, endCol;

            switch (scope)
            {
                case 0: // från cursor till slutet
                    startRow = buffer.CursorRow;
                    startCol = buffer.CursorCol;
                    endRow = buffer.Rows - 1;
                    endCol = buffer.Cols - 1;
                    break;
                case 1: // från början till cursor
                    startRow = 0;
                    startCol = 0;
                    endRow = buffer.CursorRow;
                    endCol = buffer.CursorCol;
                    break;
                case 2: // hela skärmen
                default:
                    startRow = 0;
                    startCol = 0;
                    endRow = buffer.Rows - 1;
                    endCol = buffer.Cols - 1;
                    break;
            }
            return (startRow, startCol, endRow, endCol);
        }

        public void HandleSGR(string[] parameters, ScreenBuffer buffer, TerminalState terminal)
        {
            bool redOn = true, greenOn = true, blueOn = true;

            if (parameters.Length == 0)
            {
                buffer.CurrentStyle.Reset();
                buffer.forceRedraw = true;
                return;
            }

            foreach (var p in parameters)
            {
                switch (p)
                {
                    case "0":
                        buffer.CurrentStyle.Reset();
                        buffer.forceRedraw = true;
                        break;
                    case "2":
                        buffer.CurrentStyle.LowIntensity = true;
                        break;
                    case "4":
                        blueOn = false;
                        buffer.CurrentStyle.Underline = true;
                        break;
                    case "5":
                        greenOn = false;
                        buffer.CurrentStyle.Blink = true;
                        break;
                    case ">1":
                        redOn = false;
                        buffer.CurrentStyle.StrikeThrough = true;
                        break;
                    case "7":
                        buffer.CurrentStyle.ReverseVideo = true;
                        break;
                    case ">2":
                        buffer.CurrentStyle.Foreground = buffer.CurrentStyle.Background;
                        break;
                    case ">3":
                        this.LogDebug($"Line Drawing Graphics, ESC [{p}m");
                        break;
                    case ">4":
                        this.LogDebug($"Block Drawing Graphics, ESC [{p}m");
                        break;
                }
        }

            // Mappa färgkanaler till StyleInfo.Color
            StyleInfo.Color resolvedColor = ResolvePT200Color(redOn, greenOn, blueOn, buffer.CurrentStyle.LowIntensity);
            if (!(terminal.Display == TerminalState.DisplayType.FullColor) && terminal.GlobalColorOverride != StyleInfo.Color.Default)
            {
                buffer.CurrentStyle.Foreground = terminal.GlobalColorOverride;
            }
            else
            {
                buffer.CurrentStyle.Foreground = ResolvePT200Color(redOn, greenOn, blueOn, buffer.CurrentStyle.LowIntensity);
            }
        }

        public void HandleColorModeCommand(string[] parameters, TerminalState terminal)
        {
            if (parameters.Length == 0)
            {
                // Återgå till senaste monochrome-läge (default: White)
                terminal.Display = DisplayType.Default;
                this.LogDebug("Switched to Monochrome mode");
                return;
            }

            switch (parameters[0])
            {
                case "0": terminal.Display = DisplayType.White; break;
                case "1": terminal.Display = DisplayType.Blue; break;
                case "2": terminal.Display = DisplayType.Green; break;
                case "3": terminal.Display = DisplayType.Amber; break;
                case "8": terminal.Display = DisplayType.FullColor; break;
                default:
                    this.LogDebug($"Unknown DisplayType argument: {parameters[0]}");
                    break;
            }
            DisplayTypeChanged.Invoke();
        }

        private StyleInfo.Color ResolvePT200Color(bool red, bool green, bool blue, bool low)
        {
            if (!red && !green && !blue) return low ? StyleInfo.Color.Black_Low : StyleInfo.Color.Black;
            if (red && green && blue) return low ? StyleInfo.Color.White_Low : StyleInfo.Color.White;
            if (!red && green && blue) return low ? StyleInfo.Color.Cyan_Low : StyleInfo.Color.Cyan;
            if (red && !green && blue) return low ? StyleInfo.Color.Magenta_Low : StyleInfo.Color.Magenta;
            if (red && green && !blue) return low ? StyleInfo.Color.Yellow_Low : StyleInfo.Color.Yellow;
            if (red && !green && !blue) return low ? StyleInfo.Color.Red_Low : StyleInfo.Color.Red;
            if (!red && green && !blue) return low ? StyleInfo.Color.Green_Low : StyleInfo.Color.Green;
            if (!red && !green && blue) return low ? StyleInfo.Color.Blue_Low : StyleInfo.Color.Blue;

            return low ? StyleInfo.Color.Gray_Low : StyleInfo.Color.Gray;
        }

        public enum GlyphMode
        {
            Normal,
            LineDrawing,
            BlockDrawing
        }

        public class GlyphModeInterpreter
        {
            public void Apply(string[] parameters, TerminalState terminal)
            {
                foreach (var p in parameters)
                {
                    switch (p)
                    {
                        case ">3":
                            this.LogDebug($"Glyphmode {p} => Line Drawing");
                            break;
                        case ">4":
                            this.LogDebug($"Glyphmode {p} => Block Drawing");
                            break;
                        case "0":
                            this.LogDebug($"Glyphmode {p} => Normal");
                            break;
                            // Lägg till fler om PT200 har fler glyphlägen
                    }
                }
            }
        }
    }

    public class VisualAttributes
    {
        public bool Bold { get; set; }
        public bool Underline { get; set; }
        public bool ReverseVideo { get; set; }
        public bool LowIntensity { get; set; }
        public bool Blink { get; set; }
        public bool StrikeThrough { get; set; }

        public ConsoleColor Foreground { get; set; }
        public ConsoleColor Background { get; set; }

        // PT200 färgkanaler
        public bool RedOn { get; set; } = true;
        public bool GreenOn { get; set; } = true;
        public bool BlueOn { get; set; } = true;

        public void Reset()
        {
            Bold = false;
            Underline = false;
            ReverseVideo = false;
            LowIntensity = false;
            Blink = false;
            StrikeThrough = false;

            Foreground = ConsoleColor.Gray;
            Background = ConsoleColor.Black;

            RedOn = true;
            GreenOn = true;
            BlueOn = true;
        }

        public ConsoleColor GetPT200Color()
        {
            if (!RedOn && !GreenOn && !BlueOn) return ConsoleColor.Black;
            if (RedOn && GreenOn && BlueOn) return ConsoleColor.White;
            if (!RedOn && GreenOn && BlueOn) return ConsoleColor.Cyan;
            if (RedOn && !GreenOn && BlueOn) return ConsoleColor.Magenta;
            if (RedOn && GreenOn && !BlueOn) return ConsoleColor.Yellow;
            if (RedOn && !GreenOn && !BlueOn) return ConsoleColor.Red;
            if (!RedOn && GreenOn && !BlueOn) return ConsoleColor.Green;
            if (!RedOn && !GreenOn && BlueOn) return ConsoleColor.Blue;

            return ConsoleColor.Gray; // fallback
        }
    }
}
