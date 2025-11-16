using PT200_InputHandler;
using PT200_Logging;
using PT200_Parser;
using PT200_Rendering;
using PT200Emulator_WinForms.Engine;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Resources;
using System.Security.Cryptography.Xml;
using System.Security.Policy;
using System.Text;
using System.Windows.Forms;
using static PT200_Rendering.IRenderTarget;
using static PT200Emulator_WinForms.Controls.WinFormsRenderTarget;

namespace PT200Emulator_WinForms.Controls
{
    public class TerminalCtrl : Control
    {
        private RenderCore _core = new();
        private TerminalParser _parser;
        private WinFormsCaretController _caretController;
        public Transport _transport { get; set; }
        public ICaretController CaretController => _caretController;
        private readonly Font _font;
        public int _charWidth { get; private set; }
        public int _charHeight { get; private set; }
        private ScreenBuffer _buffer;
        public bool ShowDiagnosticOverlay { get; set; } = false;
        public IInputMapper InputMapper { get; set; }
        public bool AlwaysFullRedraw { get; set; } = true;
        private bool _layoutReady = false;
        private WinFormsRenderTarget _renderTarget;
        private UiConfig _uiConfig;
        private int inputStartCol, inputStartRow;
        private bool inputStart = false;

        private static readonly Dictionary<Keys, int> KeyToScanCode = new()
        {
            // Funktionstangenter
            { Keys.F1, 0x3B },
            { Keys.F2, 0x3C },
            { Keys.F3, 0x3D },
            { Keys.F4, 0x3E },
            { Keys.F5, 0x3F },
            { Keys.F6, 0x40 },
            { Keys.F7, 0x41 },
            { Keys.F8, 0x42 },
            { Keys.F9, 0x43 },
            { Keys.F10, 0x44 },
            { Keys.F11, 0x57 },
            { Keys.F12, 0x58 },

            // Bokstäver
            { Keys.A, 0x1E },
            { Keys.B, 0x30 },
            { Keys.C, 0x2E },
            { Keys.D, 0x20 },
            { Keys.E, 0x12 },
            { Keys.F, 0x21 },
            { Keys.G, 0x22 },
            { Keys.H, 0x23 },
            { Keys.I, 0x17 },
            { Keys.J, 0x24 },
            { Keys.K, 0x25 },
            { Keys.L, 0x26 },
            { Keys.M, 0x32 },
            { Keys.N, 0x31 },
            { Keys.O, 0x18 },
            { Keys.P, 0x19 },
            { Keys.Q, 0x10 },
            { Keys.R, 0x13 },
            { Keys.S, 0x1F },
            { Keys.T, 0x14 },
            { Keys.U, 0x16 },
            { Keys.V, 0x2F },
            { Keys.W, 0x11 },
            { Keys.X, 0x2D },
            { Keys.Y, 0x15 },
            { Keys.Z, 0x2C },

            // Siffror
            { Keys.D0, 0x0B },
            { Keys.D1, 0x02 },
            { Keys.D2, 0x03 },
            { Keys.D3, 0x04 },
            { Keys.D4, 0x05 },
            { Keys.D5, 0x06 },
            { Keys.D6, 0x07 },
            { Keys.D7, 0x08 },
            { Keys.D8, 0x09 },
            { Keys.D9, 0x0A },

            // Specialtecken (svenska layout)
            { Keys.Oem1, 0x1A },     // Å
            { Keys.Oem3, 0x29 },     // § / ±
            { Keys.Oem5, 0x2B },     // Ä
            { Keys.Oem7, 0x28 },     // Ö
            { Keys.Oemplus, 0x0D },  // +
            { Keys.OemMinus, 0x0C }, // -
            { Keys.Oemcomma, 0x33 }, // ,
            { Keys.OemPeriod, 0x34 },// .
            { Keys.Space, 0x39 },
            { Keys.Enter, 0x1C },
            { Keys.Back, 0x0E },
            { Keys.Tab, 0x0F },

            // Piltangenter (extended)
            { Keys.Left, 0xE04B },
            { Keys.Right, 0xE04D },
            { Keys.Up, 0xE048 },
            { Keys.Down, 0xE050 },

            // Insert/Delete/Home/End/PageUp/PageDown
            { Keys.Insert, 0xE052 },
            { Keys.Delete, 0xE053 },
            { Keys.Home, 0xE047 },
            { Keys.End, 0xE04F },
            { Keys.PageUp, 0xE049 },
            { Keys.PageDown, 0xE051 },

            // Escape
            { Keys.Escape, 0x01 }
        };

        public TerminalCtrl(Transport transport, UiConfig uiConfig)
        {
            _transport = transport;
            _uiConfig = uiConfig;

            // Monospace-font
            _font = new Font("Consolas", 12, FontStyle.Regular, GraphicsUnit.Pixel);

            Initialize(_transport);

            this.Width = _charWidth * _parser.Screenbuffer.Cols;
            this.Height = _charHeight * _parser.Screenbuffer.Rows;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            this.TabStop = true;
            this.Focus();
        }

        public void Initialize(Transport transport)
        {
            _parser = transport.GetParser();
            // Mät cellstorlek exakt
            _charWidth = MeasureCharWidth(_font, _parser.Screenbuffer.Cols);
            _charHeight = _font.Height;
            _renderTarget = new WinFormsRenderTarget(
                                   this.CreateGraphics(), _font, _charWidth, _charHeight,
                                   this.ForeColor, this.BackColor, _buffer);
            _renderTarget.Terminal = this;
            _caretController = new WinFormsCaretController(this, _renderTarget, _uiConfig.CursorStylePreference, _buffer, this);
            _parser.Screenbuffer.Scrolled += () => _core.ForceFullRender();
            AttachBuffer(_parser.Screenbuffer);
            _parser.Screenbuffer.AttachCaretController(_caretController);
        }

        public void AttachBuffer(ScreenBuffer buffer)
        {
            if (buffer != null)
            {
                _buffer = buffer;
                _renderTarget._buffer = buffer;
                _buffer.BufferUpdated += () =>
                {
                    if (this.IsHandleCreated && !this.IsDisposed)
                        //_pendingInvalidate = true;
                        this.Invalidate();
                };
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (AlwaysFullRedraw)
                _core.ForceFullRender();

            if (_buffer == null)
            {
                this.LogDebug(Engine.LocalizationProvider.Current.Get("log.render.buffer.null"));
                return;
            }

            // Uppdatera Graphics för denna frame
            _renderTarget.Graphics = e.Graphics;
            _renderTarget._buffer = _buffer;

            // Rita texten
            _core.Render(_buffer, _renderTarget);

            // Rita caret ovanpå
            if (_renderTarget._caretVisible) _renderTarget.DrawCaret(e.Graphics, ForeColor);

            // Overlay/debug
            if (ShowDiagnosticOverlay)
            {
                using var overlayBrush = new SolidBrush(Color.Yellow);
                e.Graphics.DrawString($"Size: {Width}x{Height}, Chars: {_buffer.Rows}x{_buffer.Cols}", this.Font, overlayBrush, new PointF(2, 2));
            }
        }


        public void ChangeFormat(int cols, int rows)
        {
            _buffer.Resize(rows, cols);
            this.Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            if (this.ClientSize.Height == 0 || this.ClientSize.Width == 0) return;
            base.OnResize(e);
            UpdateBufferFromSize();
        }

        public void ForceRepaint()
        {
            _core.ForceFullRender();  // tala om för renderern att allt ska ritas
            this.Invalidate();        // trigga OnPaint
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            return new Size(_buffer.Cols * _charWidth, _buffer.Rows * _charHeight);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            if (!_layoutReady)
            {
                _layoutReady = true;
                return;
            }

            base.OnLayout(levent);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape:
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                case Keys.Delete:
                case Keys.Tab:
                case Keys.Back:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            // Hoppa ur om det inte är ett skrivbart tecken
            if (char.IsControl(e.KeyChar)) return;
            //this.LogDebug($"Key char: {e.KeyChar}");
            if (!inputStart)
            {
                inputStartCol = _buffer.CursorCol + 1;
                inputStartRow = _buffer.CursorRow;
                inputStart = true;
            }

            base.OnKeyPress(e);

            if ("åäöÅÄÖ".Contains(e.KeyChar))
            {
                var mapped = MapSwedishChar(e.KeyChar);
                _transport.Send(new byte[] { mapped });
            }
            else
            {
                // Hantera svenska tecken eller skicka vidare som text
                var bytes = InputMapper.MapText(e.KeyChar.ToString());
                _transport.Send(bytes);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (IsPrintableKey(e.KeyCode)) return;

            // Översätt WinForms KeyEventArgs till din egen KeyEvent
            if (KeyToScanCode.TryGetValue(e.KeyCode, out int scanCode))
            {
                var mods = KeyModifiers.None;
                if (e.Shift) mods |= KeyModifiers.Shift;
                if (e.Control) mods |= KeyModifiers.Ctrl;
                if (e.Alt) mods |= KeyModifiers.Alt;

                var keyEvent = new KeyEvent(scanCode, mods);
                var bytes = InputMapper.MapKey(keyEvent);

                if (bytes != null)
                {
                    if (e.KeyCode == Keys.Back)
                    {
                        e.SuppressKeyPress = true;
                        if (IsCursorBeyondInputStart())
                        {
                            this.LogDebug("Sending backspace");
                            _buffer.Backspace();
                            _transport.Send(new byte[] { 0x08 });
                        }
                        else this.LogDebug("Backspace detected but cursor position not beyond starting position");
                    }
                    else _transport.Send(bytes);
                    this.LogDebug($"OnKeyDown sent {Encoding.ASCII.GetString(bytes)}");
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    inputStart = false;
                    e.SuppressKeyPress = true;
                    _transport.Send(Encoding.ASCII.GetBytes("\r\n"));
                }
                else if (e.KeyCode == Keys.Tab) _transport.Send(new byte[] { 0x09 });
                else if (e.KeyCode == Keys.Delete) _transport.Send(new byte[] { 0x7F });
                this.LogDebug($"Key pressed is {((mods != KeyModifiers.None) ? mods : null)} {e.KeyCode}");
                base.OnKeyDown(e);
            }
        }

        private bool IsCursorBeyondInputStart()
        {
            return _buffer.CursorRow > inputStartRow ||
                  (_buffer.CursorRow == inputStartRow && _buffer.CursorCol >= inputStartCol);
        }

        private bool IsPrintableKey(Keys key)
        {
            return
                (key >= Keys.A && key <= Keys.Z) ||
                (key >= Keys.D0 && key <= Keys.D9) ||
                key == Keys.Space ||
                key == Keys.OemPeriod ||
                key == Keys.Oemcomma ||
                key == Keys.OemMinus ||
                key == Keys.Oemplus ||
                (key >= Keys.Oem1 && key <= Keys.Oem102); // täcker åäö och andra symboler
        }

        public void UpdateBufferFromSize()
        {
            if (_charWidth == 0 || _charHeight == 0) return;

            int cols = this.ClientSize.Width / _charWidth;
            int rows = this.ClientSize.Height / _charHeight;

            _buffer.Resize(rows, cols);
        }

        public void RePaint(Color fg, Color bg)
        {
            this.ForeColor = fg;
            this.BackColor = bg;
            _renderTarget.ChangeColor(fg, bg);
            ForceRepaint();
        }

        public void SetCursorStyle(CursorStyle style, bool blink)
        {
            _caretController.SetCursorStyle(style, blink);
        }

        private int MeasureCharWidth(Font font, int columns)
        {
            // Bygg en teststräng med lika många tecken som kolumner
            string test = new string('W', columns);

            var size = TextRenderer.MeasureText(
                test,
                font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.NoClipping);

            // Dividera med antalet tecken för att få "advance width"
            return size.Width / columns;
        }

        private static byte MapSwedishChar(char c)
        {
            return c switch
            {
                'Å' => (byte)'[',  // 0x5B
                'Ö' => (byte)'\\', // 0x5C
                'Ä' => (byte)']',  // 0x5D
                'å' => (byte)'{',  // 0x7B
                'ö' => (byte)'|',  // 0x7C
                'ä' => (byte)'}',  // 0x7D
                _ => (byte)c
            };
        }

    }

    internal class WinFormsRenderTarget : IRenderTarget
    {
        public Graphics Graphics { get; set; }
        internal TerminalCtrl Terminal { get; set; }
        private readonly Font _font;
        private int _charWidth;
        private int _charHeight;
        private Color _fore;
        private Color _back;
        public ScreenBuffer _buffer;
        internal bool _caretVisible;
        internal int _caretRow = 0;
        internal int _caretCol = 0;
        private CursorStyle _caretStyle;

        public WinFormsRenderTarget(Graphics g, Font font, int charWidth, int charHeight,
                                    Color fore, Color back, ScreenBuffer buffer)
        {
            _buffer = buffer;
            Graphics = g;
            _font = font;
            _charWidth = charWidth;
            _charHeight = charHeight;
            _fore = fore;
            _back = back;

            if (_font == null) this.LogWarning(Engine.LocalizationProvider.Current.Get("log.render.font.null"));
        }

        public void Clear() => Graphics.Clear(_back);

        public void DrawRun(RenderRun run)
        {
            int x = run.StartCol * _charWidth;
            int y = run.Row * _charHeight;

            var attr = _buffer.ZoneAttributes[run.Row, run.StartCol];
            var reverse = attr.ReverseVideo;
            var low = attr.LowIntensity;

            var fgBase = reverse ? _back : _fore;
            var bgBase = reverse ? _fore : _back;

            var fg = low ? DimColor(fgBase) : fgBase;
            var bg = bgBase;

            // 1. Fyll hela cellens bakgrund
            using (var bgBrush = new SolidBrush(bg))
            {
                Graphics.FillRectangle(bgBrush, x, y, run.Chars.Length * _charWidth, _charHeight);
            }

            // 2. Rita texten ovanpå, utan bakgrund
            TextRenderer.DrawText(
                Graphics,
                new string(run.Chars),
                _font,
                new Point(x, y),
                fg,
                Color.Transparent,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.NoClipping
            );
        }

        internal void ChangeColor(Color fg, Color bg)
        {
            _fore = fg;
            _back = bg;
        }

        public Color DimColor(Color color, double Factor = 0.5)
        {
            return Color.FromArgb(
                color.A,
                (int)(color.R * Factor),
                (int)(color.G * Factor),
                (int)(color.B * Factor));
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

        public void SetCaret(int row, int col)
        {
            _caretRow = row;
            _caretCol = col;
        }

        public void DrawCaret(Graphics g, Color caretColor)
        {
            if (!_caretVisible || _buffer == null) return;

            int x = _caretCol * _charWidth;
            int y = _caretRow * _charHeight;

            switch (_caretStyle)
            {
                case CursorStyle.Block:
                    //using (var overlay = new SolidBrush(TranslateColor(_buffer.GetCell(_buffer.CursorRow, _buffer.CursorCol).Foreground)))
                    using (var overlay = new SolidBrush(caretColor))
                    {
                        g.FillRectangle(overlay, x, y, _charWidth, _charHeight);
                    }
                    break;

                case CursorStyle.HorizontalBar:
                    g.FillRectangle(new SolidBrush(caretColor), x, y + _charHeight - 2, _charWidth, 2);
                    break;

                case CursorStyle.VerticalBar:
                    g.FillRectangle(new SolidBrush(caretColor), x, y, 2, _charHeight);
                    break;
            }
        }

        public void SetCaretStyle(CursorStyle style)
        {
            _caretStyle = style;
        }
    }


    internal class WinFormsCaretController : ICaretController
    {
        private readonly Control _control;
        private readonly TerminalCtrl _terminalCtrl;
        private readonly WinFormsRenderTarget _renderTarget;
        private readonly System.Windows.Forms.Timer _blinkTimer;
        private int _row, _col;
        internal IRenderTarget.CursorStyle _style = IRenderTarget.CursorStyle.HorizontalBar;
        private ScreenBuffer _buffer;
        private bool _cursorBlink = true;
        public WinFormsCaretController(Control control) => _control = control;

        public WinFormsCaretController(Control control, WinFormsRenderTarget renderTarget, IRenderTarget.CursorStyle style, ScreenBuffer buffer, TerminalCtrl ctrl)
        {
            _control = control;
            _renderTarget = renderTarget;
            _buffer = buffer;
            _terminalCtrl = ctrl;
            SetCursorStyle(style, _cursorBlink);

            _blinkTimer = new System.Windows.Forms.Timer();
            _blinkTimer.Interval = 500;
            _blinkTimer.Tick += (s, e) =>
            {
                _renderTarget._caretVisible = !_renderTarget._caretVisible;
                _control.Invalidate(); // trigga omritning
            };
            if (_cursorBlink) _blinkTimer.Start();
        }

        public void SetCaretPosition(int row, int col)
        {
            _row = row;
            _col = col;
            _renderTarget.SetCaret(_row, _col);
            _control.Invalidate();
        }

        public void SetCursorStyle(IRenderTarget.CursorStyle style, bool blink)
        {
            _style = style;
            _cursorBlink = blink;
            _renderTarget.SetCaretStyle(_style);
            if (_blinkTimer != null)
                if (_cursorBlink) _blinkTimer.Start();
                else
                {
                    _blinkTimer.Stop();
                    _renderTarget._caretVisible = true;
                }
            _control.Invalidate();

        }
        public void MoveCaret(int dRow, int dCol)
        {
            _row = _row + dRow;
            _col = _col + dCol;
            _buffer.MarkDirty();
            _control.Invalidate();
        }
        public void Show()
        {
            _renderTarget._caretVisible = true;
            _blinkTimer.Start();
        }

        public void Hide()
        {
            _renderTarget._caretVisible = false;
            _blinkTimer.Stop();
            _control.Invalidate();
        }
    }
}