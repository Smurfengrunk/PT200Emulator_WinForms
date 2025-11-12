using PT200_Parser;
using PT200_Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using PT200_Logging;
using System.Diagnostics;

namespace PT200_Rendering
{
    public class RenderCore
    {
        private RenderSnapshot[,] _lastFrame;
        private bool _initialized;
        private char RawChar;
        private Color Fg, Bg;

        private record struct RenderSnapshot(char RawChar, char OutChar, Color Fg, Color Bg)
        {
            public bool Equals(RenderSnapshot other)
            {
                return RawChar == other.RawChar &&
                       Fg == other.Fg &&
                       Bg == other.Bg;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(RawChar, Fg, Bg);
            }
        }

        public void ForceFullRender()
        {
            _initialized = false;
            _lastFrame = null;
        }

        public void Render(ScreenBuffer buffer, IRenderTarget target)
        {
            if (buffer.clearScreen)
            {
                _initialized = false;
                target.Clear();
                buffer.ScreenCleared();
            }

            if (_lastFrame == null || _lastFrame.GetLength(0) != buffer.Rows || _lastFrame.GetLength(1) != buffer.Cols)
            {
                _lastFrame = new RenderSnapshot[buffer.Rows, buffer.Cols];
                _initialized = false;
            }

            for (int row = 0; row < buffer.Rows; row++)
            {
                int col = 0;
                while (col < buffer.Cols)
                {
                    var cell = buffer.GetCell(row, col);
                    var zone = buffer.ZoneAttributes[row, col];

                    var fg = zone?.Foreground ?? cell.Style.Foreground;
                    var bg = zone?.Background ?? cell.Style.Background;
                    bool reverse = zone?.ReverseVideo ?? cell.Style.ReverseVideo;
                    bool lowintensity = zone?.LowIntensity ?? cell.Style.LowIntensity;

                    if (reverse) (fg, bg) = (bg, fg);
                    if (lowintensity)
                    {
                        if (reverse) bg = bg.MakeDim();
                        else fg = fg.MakeDim();
                    }

                    var rawChar = cell.Char;
                    var outCh = cell.Char == '\0' ? ' ' : cell.Char;
                    var snap = new RenderSnapshot(rawChar, outCh, TranslateColor(fg), TranslateColor(bg));

                    if (!_initialized || !_lastFrame[row, col].Equals(snap))
                    {
                        int runStart = col;
                        var runFg = snap.Fg;
                        var runBg = snap.Bg;
                        var runChars = new List<char>();

                        while (col < buffer.Cols)
                        {
                            var c = buffer.GetCell(row, col);
                            var z = buffer.ZoneAttributes[row, col];
                            var f = z?.Foreground ?? c.Style.Foreground;
                            var b = z?.Background ?? c.Style.Background;
                            var r = z?.ReverseVideo ?? c.Style.ReverseVideo;
                            var l = z?.LowIntensity ?? c.Style.LowIntensity;

                            if (r) (f, b) = (b, f);
                            if (l)
                            {
                                if (r) b = b.MakeDim();
                                else f = f.MakeDim();
                            }

                            var ch = c.Char == '\0' ? ' ' : c.Char;
                            var s = new RenderSnapshot(rawChar, ch, TranslateColor(f), TranslateColor(b));

                            if (!_initialized && col == runStart) { }
                            else if (!s.Equals(snap)) break;

                            runChars.Add(ch);
                            if (_lastFrame != null) _lastFrame[row, col] = s;
                            col++;
                        }

                        target.DrawRun(new RenderRun(row, runStart, runFg, runBg, runChars.ToArray()));
                    }
                    else
                    {
                        col++;
                    }
                }
            }

            _initialized = true;
            buffer.forceRedraw = false;
            buffer.ClearDirty();
            target.SetCaret(buffer.CursorRow, buffer.CursorCol);
        }

        private static Color TranslateColor(StyleInfo.Color color) => color switch
        {
            StyleInfo.Color.Black => Color.Black,
            StyleInfo.Color.DarkBlue => Color.DarkBlue,
            StyleInfo.Color.DarkGreen => Color.DarkGreen,
            StyleInfo.Color.DarkCyan => Color.DarkCyan,
            StyleInfo.Color.DarkRed => Color.DarkRed,
            StyleInfo.Color.DarkMagenta => Color.DarkMagenta,
            StyleInfo.Color.DarkYellow => Color.FromArgb(128, 128, 0),
            StyleInfo.Color.Gray => Color.Gray,
            StyleInfo.Color.Blue => Color.Blue,
            StyleInfo.Color.Green => Color.LimeGreen,
            StyleInfo.Color.Cyan => Color.Cyan,
            StyleInfo.Color.Red => Color.Red,
            StyleInfo.Color.Magenta => Color.Magenta,
            StyleInfo.Color.Yellow => Color.Yellow,
            StyleInfo.Color.White => Color.White,
            _ => Color.Wheat
        };
    }
}