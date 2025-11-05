using PT200_InputHandler;
using PT200_Logging;
using PT200_Parser;
using PT200_Rendering;
using PT200Emulator_WinForms.Engine;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Resources;
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
        private bool _pendingInvalidate;
        private readonly System.Windows.Forms.Timer _throttleTimer;
        public bool ShowDiagnosticOverlay { get; set; } = false;
        public IInputMapper InputMapper { get; set; }
        public bool AlwaysFullRedraw { get; set; } = true;
        private bool _layoutReady = false;
        private WinFormsRenderTarget _renderTarget;

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

        public TerminalCtrl(Transport transport)
        {
            _transport = transport;
            Initialize(_transport);
            _throttleTimer = new System.Windows.Forms.Timer { Interval = 33 }; // ~30 fps
            _throttleTimer.Tick += (s, e) =>
            {
                if (_pendingInvalidate)
                {
                    base.Invalidate();
                    _pendingInvalidate = false;
                }
            };
            _throttleTimer.Start();

            // Monospace-font
            _font = new Font("Consolas", 12, FontStyle.Regular, GraphicsUnit.Pixel);

            // Mät cellstorlek exakt
            using (var g = this.CreateGraphics())
            {
                var size = g.MeasureString("W", _font, int.MaxValue, StringFormat.GenericTypographic);
                _charWidth = (int)Math.Ceiling(size.Width);
                _charHeight = (int)Math.Ceiling(size.Height);
            }
            this.LogDebug($"TerminalCtrl initialized with char size {_charWidth}x{_charHeight}, control size {this.Size}");
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            this.TabStop = true;
            this.Focus();
        }

        public void Initialize(Transport transport)
        {
            _parser = transport.GetParser();
            _renderTarget = new WinFormsRenderTarget(
                                   this.CreateGraphics(), _font, _charWidth, _charHeight,
                                   this.ForeColor, this.BackColor, _buffer);
            _caretController = new WinFormsCaretController(this, _renderTarget, IRenderTarget.CursorStyle.Block, _buffer);
            _parser.Screenbuffer.AttachCaretController(_caretController);
            _parser.Screenbuffer.Scrolled += () => _core.ForceFullRender();
        }
        public void AttachBuffer(ScreenBuffer buffer)
        {
            _buffer = buffer;
            _renderTarget._buffer = buffer;
            _buffer.BufferUpdated += () =>
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                    _pendingInvalidate = true;
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_buffer == null) return;

            if (AlwaysFullRedraw)
                _core.ForceFullRender();

            // Uppdatera Graphics för denna frame
            _renderTarget.Graphics = e.Graphics;
            _renderTarget._buffer = _buffer;

            // Rita texten
            _core.Render(_buffer, _renderTarget);

            // Rita caret ovanpå
            if (_renderTarget._caretVisible)
            {
                _renderTarget.SetCaret(_buffer.CursorRow, _buffer.CursorCol, _renderTarget._caretVisible, _renderTarget._caretStyle);
                _renderTarget.DrawCaret(e.Graphics);
            }

            // Overlay/debug
            if (ShowDiagnosticOverlay)
            {
                using var overlayBrush = new SolidBrush(Color.Yellow);
                e.Graphics.DrawString($"Size: {Width}x{Height}, Chars: {_buffer.Rows}x{_buffer.Cols}",
                                      this.Font, overlayBrush, new PointF(2, 2));
            }
        }


        public void ChangeFormat(int cols, int rows)
        {
            _buffer.Resize(rows, cols);
            this.LogDebug($"ChangeFormat called: cols={cols}, rows={rows}, content of buffer cell (1,1)='{_buffer.GetCell(1, 1).Char}'. Calling this.PerformLayout.");
            this.Invalidate();

            this.LogDebug($"charWidth: {_charWidth}, charHeight: {_charHeight}");
            this.LogDebug($"Expected size: {_charWidth * cols} x {_charHeight * rows}");
            this.LogDebug($"Actual ClientSize: {this.ClientSize.Width} x {this.ClientSize.Height}");
            this.LogDebug($"Actual Size: {this.Size.Width} x {this.Size.Height}");
        }

        protected override void OnResize(EventArgs e)
        {
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
            // Gör så att piltangenter, Tab osv. inte “äts upp” av WinForms
            return true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (IsPrintableKey(e.KeyCode))
                return;
            base.OnKeyDown(e);

            // Översätt WinForms KeyEventArgs till din egen KeyEvent
            if (KeyToScanCode.TryGetValue(e.KeyCode, out int scanCode))
            {
                var mods = KeyModifiers.None;
                if (e.Shift) mods |= KeyModifiers.Shift;
                if (e.Control) mods |= KeyModifiers.Ctrl;
                if (e.Alt) mods |= KeyModifiers.Alt;

                var keyEvent = new KeyEvent(scanCode, mods);
                var bytes = InputMapper.MapKey(keyEvent);
                if (bytes == null)
                {
                    switch (e.KeyCode)
                    {
                        case Keys.Up:
                            this.LogDebug("No mapping found – sending fallback for Up");
                            bytes = new byte[] { 0x1B, (byte)'A' }; // eller PT200-specifik sekvens
                            break;
                            // fler fall...
                    }
                }
                this.LogDebug($"Mapped to bytes: {(bytes != null ? BitConverter.ToString(bytes) : "null")}");
                if (bytes != null)
                    _transport.Send(bytes);
                this.LogDebug($"[TerminalCtrl.OnKeyDown] KeyValue: {e.KeyValue}, KeyData {e.KeyData}, Key event: scan code {keyEvent.ScanCode}, Modifiers {keyEvent.Modifiers}");
            }
        }

        private bool IsPrintableKey(Keys key)
        {
            return
                (key >= Keys.A && key <= Keys.Z) ||
                (key >= Keys.D0 && key <= Keys.D9) ||
                key == Keys.Space ||
                key == Keys.Enter ||
                key == Keys.Back ||
                key == Keys.OemPeriod ||
                key == Keys.Oemcomma ||
                key == Keys.OemMinus ||
                key == Keys.Oemplus ||
                (key >= Keys.Oem1 && key <= Keys.Oem102); // täcker åäö och andra symboler
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            var bytes = InputMapper.MapText(e.KeyChar.ToString());
            if (bytes != null)
            {
                _transport.Send(bytes);
                _buffer.MarkDirty();
            }
        }

        public void UpdateBufferFromSize()
        {
            if (_charWidth == 0 || _charHeight == 0) return;

            int cols = this.ClientSize.Width / _charWidth;
            int rows = this.ClientSize.Height / _charHeight;

            _buffer.Resize(rows, cols);
            this.LogDebug($"Buffer resized to {cols}x{rows} based on ClientSize {this.ClientSize.Width}x{this.ClientSize.Height}");
        }
    }

    internal class WinFormsRenderTarget : IRenderTarget
    {
        public Graphics Graphics { get; set; }
        private readonly Font _font;
        private int _charWidth;
        private int _charHeight;
        private readonly Color _fore;
        private readonly Color _back;
        public ScreenBuffer _buffer;
        private IRenderTarget.CursorStyle _style;
        internal bool _caretVisible = true;
        internal int _caretRow = 0;
        internal int _caretCol = 0;
        internal IRenderTarget.CursorStyle _caretStyle;

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

            Size size = TextRenderer.MeasureText("W", _font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            _charWidth = size.Width + 1; // justera med 1 pixel
            _charHeight = size.Height;
            if (_font != null) this.LogDebug($"WinFormsRenderTarget initialized with char size {_charWidth}x{_charHeight} and Font {_font.Name} size {_font.Size}");
            else this.LogWarning("Font is null in WinFormsRenderTarget constructor!");
        }

        public void Clear() => Graphics.Clear(_back);

        public void DrawRun(RenderRun run)
        {
            //var fg = TranslateConsoleColor(run.Fg);
            //var bg = TranslateConsoleColor(run.Bg);
            if (_buffer == null) Debugger.Break();

            int x = run.StartCol * _charWidth;
            int y = run.Row * _charHeight;

            var reverse = _buffer.ZoneAttributes[run.Row, run.StartCol].ReverseVideo;
            var low = _buffer.ZoneAttributes[run.Row, run.StartCol].LowIntensity;

            var fgBase = reverse ? this._back : this._fore;
            var bgBase = reverse ? this._fore : this._back;

            // Justera foreground om lowintensity är satt
            var fg = low ? DimColor(fgBase) : fgBase;
            var bg = bgBase;

            TextRenderer.DrawText(Graphics, new string(run.Chars), _font,
                                  new Point(x, y), fg, bg,
                                  TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            //_g.FillRectangle(bgBrush, x, y, run.Chars.Length * _charWidth, _charHeight);
            //_g.DrawString(new string(run.Chars), _font, fgBrush, x, y);
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

        public void SetCaret(int row, int col, bool visible, CursorStyle style)
        {
            _caretRow = row;
            _caretCol = col;
            _caretVisible = visible;
            _caretStyle = style;
        }

        public void DrawCaret(Graphics g)
        {
            if (!_caretVisible || _buffer == null) return;

            int x = _caretCol * _charWidth;
            int y = _caretRow * _charHeight;

            switch (_caretStyle)
            {
                case CursorStyle.Block:
                    using (var overlay = new SolidBrush(Color.LimeGreen))
                    {
                        this.LogDebug($"Filling rectangle at ({x}, {y}) with char width {_charWidth}, char height {_charHeight} using overlay color {overlay.Color.Name}");
                        g.FillRectangle(overlay, x, y, _charWidth, _charHeight);
                    }
                    break;

                case CursorStyle.HorizontalBar:
                    g.FillRectangle(Brushes.LimeGreen, x, y + _charHeight - 2, _charWidth, 2);
                    break;

                case CursorStyle.VerticalBar:
                    g.FillRectangle(Brushes.LimeGreen, x, y, 2, _charHeight);
                    break;
            }
        }
    }


    internal class WinFormsCaretController : ICaretController
    {
        private readonly Control _control;
        private readonly WinFormsRenderTarget _renderTarget;
        private readonly System.Windows.Forms.Timer _blinkTimer;
        private bool _showCaret = true;
        private int _row, _col;
        private IRenderTarget.CursorStyle _style = IRenderTarget.CursorStyle.HorizontalBar;
        private ScreenBuffer _buffer;
        public WinFormsCaretController(Control control) => _control = control;

        public WinFormsCaretController(Control control, WinFormsRenderTarget renderTarget, IRenderTarget.CursorStyle style, ScreenBuffer buffer)
        {
            _control = control;
            _renderTarget = renderTarget;
            _style = style;
            _buffer = buffer;

            _blinkTimer = new System.Windows.Forms.Timer();
            _blinkTimer.Interval = 500;
            _blinkTimer.Tick += (s, e) =>
            {
                _showCaret = !_showCaret;
                _control.Invalidate(); // trigga omritning
            };
            _blinkTimer.Start();
        }

        public void SetCaretPosition(int row, int col)
        {
            _row = row;
            _col = col;
            _renderTarget.SetCaret(_row, _col, _showCaret, _style);
            _control.Invalidate();
        }

        public void SetCursorStyle(IRenderTarget.CursorStyle style)
        {
            if (_style == style) return;
            _style = style;
            _renderTarget.SetCaret(_row, _col, _showCaret, _style);
            _control.Invalidate();
        }
        public void MoveCaret(int dRow, int dCol)
        {
            _row = _row + dRow;
            _col = _col + dCol;
            _buffer.MarkDirty();
        }
        public void Show()
        {
            _showCaret = true;
            _blinkTimer.Start();
            _control.Invalidate();
        }

        public void Hide()
        {
            _showCaret = false;
            _blinkTimer.Stop();
            _control.Invalidate();
        }
    }
}