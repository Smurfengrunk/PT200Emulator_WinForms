using System;
using System.Drawing;
using System.Windows.Forms;
using PT200_Logging;
using PT200_Parser;
using PT200_Rendering;

namespace PT200Emulator_WinForms.Controls
{
    public class TerminalCtrl : Control
    {
        private readonly RenderCore _core = new();
        private readonly Font _font;
        private int _charWidth;
        private int _charHeight;
        private ScreenBuffer _buffer;
        private bool _pendingInvalidate;
        private readonly System.Windows.Forms.Timer _throttleTimer;
        public bool ShowDiagnosticOverlay { get; set; } = false;
        private bool _suppressResize = false;

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
            _font = new Font("Consolas", 10, FontStyle.Regular, GraphicsUnit.Pixel);

            // Mät cellstorlek exakt
            using (var g = this.CreateGraphics())
            {
                var size = g.MeasureString("W", _font, int.MaxValue, StringFormat.GenericTypographic);
                _charWidth = (int)Math.Ceiling(size.Width);
                _charHeight = (int)Math.Ceiling(size.Height);
            }
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        public void AttachBuffer(ScreenBuffer buffer)
        {
            _buffer = buffer;
            _buffer.BufferUpdated += () =>
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                    _pendingInvalidate = true;
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

            if (_buffer != null)
            {
                var target = new WinFormsRenderTarget(e.Graphics, _font, _charWidth, _charHeight,
                                                      this.ForeColor, this.BackColor);
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
        }

        public void ChangeFormat(int cols, int rows)
        {
            _suppressResize = true;
            this.LogDebug($"Changing terminal format to {cols}x{rows}");
            this.LogDebug($"this.Size = new Size({cols} * {_charWidth}, {rows} * {_charHeight}) -> this.Size({cols * _charWidth}, {rows * _charHeight})");
            _buffer.Resize(rows, cols);
            Invalidate();
            Parent.PerformLayout();
            this.LogDebug($"Changed terminal format to {cols}x{rows}, TerminalCtrl size {this.Size}");
            this.LogDebug($"PreferredSize is now {GetPreferredSize(Size.Empty)}");
            this.LogDebug($"Panel size after ChangeFormat: {Parent.Size}");
            this.LogDebug($"ClientSize is now {this.ClientSize}");
            this.LogDebug($"Bounds is now {this.Bounds}");
            this.LogDebug($"DisplayRectangle is now {this.DisplayRectangle}");
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
                this.LogErr("_buffer is null in SetBoundsCore");
                this.LogStackTrace();
            }
            if (_buffer != null)
            {
                int desiredW = _buffer.Cols * _charWidth;
                int desiredH = _buffer.Rows * _charHeight;
                this.LogDebug($"[SetBoundsCore] Requested size: {width}x{height}, Desired size: {desiredW}x{desiredH}");

                if (width != desiredW || height != desiredH)
                {
                    width = desiredW;
                    height = desiredH;
                }
                if (this.Width == width && this.Height == height) return;
                this.LogDebug($"base.SetBoundsCore({x}, {y}, {width}, {height}, {specified});");
                base.SetBoundsCore(x, y, width, height, specified);
            }
        }

        internal class WinFormsRenderTarget : IRenderTarget
        {
            private readonly Graphics _g;
            private readonly Font _font;
            private readonly int _charWidth;
            private readonly int _charHeight;
            private readonly Color _fore;
            private readonly Color _back;

            public WinFormsRenderTarget(Graphics g, Font font, int charWidth, int charHeight,
                                        Color fore, Color back)
            {
                _g = g;
                _font = font;
                _charWidth = charWidth;
                _charHeight = charHeight;
                _fore = fore;
                _back = back;
            }

            public void Clear() => _g.Clear(_back);

            public void DrawRun(RenderRun run)
            {
                float x = run.StartCol * _charWidth;
                float y = run.Row * _charHeight;

                using var brush = new SolidBrush(_fore);
                _g.DrawString(new string(run.Chars), _font, brush, x, y);
                //this.LogDebug($"Drew run at row {run.Row}, col {run.StartCol}: '{new string(run.Chars)}'");
            }

            public void SetCaret(int row, int col)
            {
                // Här kan du rita en egen caret om du vill
            }

            private static Color MapColor(ConsoleColor c) => c switch
            {
                ConsoleColor.Black => Color.Black,
                ConsoleColor.DarkBlue => Color.DarkBlue,
                ConsoleColor.DarkGreen => Color.DarkGreen,
                ConsoleColor.DarkCyan => Color.DarkCyan,
                ConsoleColor.DarkRed => Color.DarkRed,
                ConsoleColor.DarkMagenta => Color.DarkMagenta,
                ConsoleColor.DarkYellow => Color.Olive,
                ConsoleColor.Gray => Color.Gray,
                ConsoleColor.DarkGray => Color.DarkGray,
                ConsoleColor.Blue => Color.Blue,
                ConsoleColor.Green => Color.Green,
                ConsoleColor.Cyan => Color.Cyan,
                ConsoleColor.Red => Color.Red,
                ConsoleColor.Magenta => Color.Magenta,
                ConsoleColor.Yellow => Color.Yellow,
                ConsoleColor.White => Color.White,
                _ => Color.White
            };
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