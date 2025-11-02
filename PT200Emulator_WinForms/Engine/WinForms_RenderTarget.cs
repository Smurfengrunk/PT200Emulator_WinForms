using PT200_InputHandler;
using PT200_Logging;
using PT200_Parser;
using PT200_Rendering;
using PT200Emulator_WinForms.Engine;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Security.Policy;
using System.Text;
using System.Windows.Forms;

namespace PT200Emulator_WinForms.Controls
{
    public class TerminalCtrl : Control
    {
        private readonly RenderCore _core = new();
        private readonly Font _font;
        public int _charWidth { get; private set; }
        public int _charHeight { get; private set; }
        private ScreenBuffer _buffer;
        private bool _pendingInvalidate;
        private readonly System.Windows.Forms.Timer _throttleTimer;
        public bool ShowDiagnosticOverlay { get; set; } = false;
        private bool _suppressResize = false;
        public IInputMapper InputMapper { get; set; }
        public Transport _transport { get; set; }
        public bool AlwaysFullRedraw { get; set; } = true;
        private bool _layoutReady = false;

        private static readonly Dictionary<Keys, byte> KeyToScanCode = new()
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
    { Keys.Left, 0x4B },
    { Keys.Right, 0x4D },
    { Keys.Up, 0x48 },
    { Keys.Down, 0x50 },

    // Insert/Delete/Home/End/PageUp/PageDown
    { Keys.Insert, 0x52 },
    { Keys.Delete, 0x53 },
    { Keys.Home, 0x47 },
    { Keys.End, 0x4F },
    { Keys.PageUp, 0x49 },
    { Keys.PageDown, 0x51 },

    // Escape
    { Keys.Escape, 0x01 }
};

        public TerminalCtrl()
        {
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
            this.Size = new Size(_charWidth * 80, _charHeight * 24);
            this.LogDebug($"TerminalCtrl initialized with char size {_charWidth}x{_charHeight}, control size {this.Size}");
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            this.TabStop = true;
            this.Focus();
        }

        public void AttachBuffer(ScreenBuffer buffer)
        {
            _buffer = buffer;
            _buffer.BufferUpdated += () =>
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                    _pendingInvalidate = true;
            };
            UpdateSizeFromBuffer();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_buffer != null)
            {
                if (AlwaysFullRedraw)
                    _core.ForceFullRender();

                var target = new WinFormsRenderTarget(
                    e.Graphics, _font, _charWidth, _charHeight,
                    this.ForeColor, this.BackColor, _buffer);

                _core.Render(_buffer, target);
            }

            // --- Diagnostic overlay ---
            string diag = $"Size: {Width}x{Height}, " +
                              $"Chars: {_buffer?.Rows}x{_buffer?.Cols}, " +
                              $"Fore: {ForeColor}, Back: {BackColor}, " +
                              $"Time: {DateTime.Now:HH:mm:ss.fff}";
            if (ShowDiagnosticOverlay)
            {
                using var overlayBrush = new SolidBrush(Color.Yellow);
                e.Graphics.DrawString(diag, this.Font, overlayBrush, new PointF(2, 2));
            }
            e.Graphics.DrawRectangle(Pens.Red, 0, 0, this.ClientSize.Width - 1, this.ClientSize.Height - 1);
        }

        public void ChangeFormat(int cols, int rows)
        {
            _suppressResize = true;

            _buffer.Resize(rows, cols);
            this.PerformLayout();
            this.MinimumSize = this.Size;


            this.LogDebug($"charWidth: {_charWidth}, charHeight: {_charHeight}");
            this.LogDebug($"Expected size: {_charWidth * cols} x {_charHeight * rows}");
            this.LogDebug($"Actual ClientSize: {this.ClientSize.Width} x {this.ClientSize.Height}");
            this.LogDebug($"Actual Size: {this.Size.Width} x {this.Size.Height}");
        }

        protected override void OnResize(EventArgs e)
        {
            if (_suppressResize)
            {
                //_suppressResize = false;
                return;
            }
            base.OnResize(e);
            this.LogDebug($"[OnResize] TerminalCtrl resized to {this.Size}");
            Invalidate();
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

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (_buffer == null)
            {
                base.SetBoundsCore(x, y, width, height, specified);
                return;
            }
            if (_buffer != null)
            {
                int desiredW = _buffer.Cols * _charWidth;
                int desiredH = _buffer.Rows * _charHeight;
                //this.LogDebug($"[SetBoundsCore] Requested size: {width}x{height}, Desired size: {desiredW}x{desiredH}");

                if (width != desiredW || height != desiredH)
                {
                    width = desiredW;
                    height = desiredH;
                }
                if (this.Width == width && this.Height == height) return;
                //this.LogDebug($"base.SetBoundsCore({x}, {y}, {width}, {height}, {specified});");
                base.SetBoundsCore(x, y, width, height, specified);
            }
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
            if (KeyToScanCode.TryGetValue(e.KeyCode, out byte scanCode))
            {
                var mods = KeyModifiers.None;
                if (e.Shift) mods |= KeyModifiers.Shift;
                if (e.Control) mods |= KeyModifiers.Ctrl;
                if (e.Alt) mods |= KeyModifiers.Alt;

                var keyEvent = new KeyEvent(scanCode, mods);
                var bytes = InputMapper.MapKey(keyEvent);
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

        public void UpdateSizeFromBuffer()
        {
            if (_buffer == null) return;

            int cols = _buffer.Cols;
            int rows = _buffer.Rows;

            int width = Margin.Left + Margin.Right + cols * _charWidth;
            int height = Margin.Top + Margin.Bottom + rows * _charHeight;

            this.Size = new Size(width, height);
            this.MinimumSize = this.Size;

            this.LogDebug($"TerminalCtrl resized to {width}x{height} based on buffer {cols}x{rows}");
        }
    }

    internal class WinFormsRenderTarget : IRenderTarget
    {
        private readonly Graphics _g;
        private readonly Font _font;
        private int _charWidth;
        private int _charHeight;
        private readonly Color _fore;
        private readonly Color _back;
        private ScreenBuffer _buffer;

        public WinFormsRenderTarget(Graphics g, Font font, int charWidth, int charHeight,
                                    Color fore, Color back, ScreenBuffer buffer)
        {
            _buffer = buffer;
            _g = g;
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
        }

        public void Clear() => _g.Clear(_back);

        public void DrawRun(RenderRun run)
        {
            //var fg = TranslateConsoleColor(run.Fg);
            //var bg = TranslateConsoleColor(run.Bg);

            int x = run.StartCol * _charWidth;
            int y = run.Row * _charHeight;

            var reverse = _buffer.ZoneAttributes[run.Row, run.StartCol].ReverseVideo;
            var low = _buffer.ZoneAttributes[run.Row, run.StartCol].LowIntensity;

            var fgBase = reverse ? this._back : this._fore;
            var bgBase = reverse ? this._fore : this._back;

            // Justera foreground om lowintensity är satt
            var fg = low ? DimColor(fgBase) : fgBase;
            var bg = bgBase;

            TextRenderer.DrawText(_g, new string(run.Chars), _font,
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

        private static Color TranslateConsoleColor(ConsoleColor color) => color switch
        {
            ConsoleColor.Black => Color.Black,
            ConsoleColor.DarkBlue => Color.DarkBlue,
            ConsoleColor.DarkGreen => Color.DarkGreen,
            ConsoleColor.DarkCyan => Color.DarkCyan,
            ConsoleColor.DarkRed => Color.DarkRed,
            ConsoleColor.DarkMagenta => Color.DarkMagenta,
            ConsoleColor.DarkYellow => Color.FromArgb(128, 128, 0),
            ConsoleColor.Gray => Color.Gray,
            ConsoleColor.DarkGray => Color.DarkGray,
            ConsoleColor.Blue => Color.Blue,
            ConsoleColor.Green => Color.LimeGreen,
            ConsoleColor.Cyan => Color.Cyan,
            ConsoleColor.Red => Color.Red,
            ConsoleColor.Magenta => Color.Magenta,
            ConsoleColor.Yellow => Color.Yellow,
            ConsoleColor.White => Color.White,
            _ => Color.White
        };

        public void SetCaret(int row, int col)
        {
            // Här kan du rita en egen caret om du vill
        }

        public class WinFormsCaretController : ICaretController
        {
            private readonly Control _control;
            public WinFormsCaretController(Control control) => _control = control;

            public void SetCaretPosition(int row, int col) { /* rita caret i kontrollen */ }
            public void MoveCaret(int dRow, int dCol) { /* flytta caret */ }
            public void Show() { /* visa caret */ }
            public void Hide() { /* dölj caret */ }
        }
    }
}