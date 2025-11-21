#pragma warning disable CA1707

using PT200_InputHandler;

using PT200_Logging;

using PT200_Parser;

using PT200_Rendering;

using PT200EmulatorWinforms.Engine;

using System.Text;

using static PT200_Rendering.IRenderTarget;

namespace PT200EmulatorWinforms.Controls
{
    /// <summary>
    /// Class to handle the actual terminal control with the content from the rendering module
    /// </summary>
    public class TerminalCtrl : Control
    {
        private RenderCore _core = new();
        private TerminalParser _parser;
        private WinFormsCaretController _caretController;
        public Transport Transport { get; set; }
        public ICaretController CaretController => _caretController;
        private readonly Font _font;
        public int charWidth { get; private set; }
        public int charHeight { get; private set; }
        private ScreenBuffer _buffer;
        public bool ShowDiagnosticOverlay { get; set; }
        public IInputMapper InputMapper { get; set; }
        public bool AlwaysFullRedraw { get; set; } = true;
        private bool _layoutReady;
        private WinFormsRenderTarget _renderTarget;
        private UiConfig _uiConfig;
        private int inputStartCol, inputStartRow;
        private bool inputStart;


        /// <summary>
        /// Dictionary for translation of Windows Forms key codes to actual scan codes, to be able to map them to the keymap json
        /// </summary>
        private static readonly Dictionary<Keys, int> KeyToScanCode = new()
        {
            // Function keys
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

            // Letters
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

            // Digits
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

            // Special characters (swedish layout)
            { Keys.Oem1, 0x1A },     // Å
            { Keys.Oem3, 0x29 },     // §
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

            // Arrow keys (extended)
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
            { Keys.Escape, 0x01 },

            //Pause/Break
            {Keys.Pause, 0xE11D }
        };

        /// <summary>
        /// Constructor for the terminal control, the heart of the application.
        /// Sets properties for Transport and UiConfig, initializes transport, sets font and other styles relevant
        /// </summary>
        /// <param name="transport"></param>
        /// <param name="uiConfig"></param>
        public TerminalCtrl(Transport transport, UiConfig uiConfig)
        {
            Transport = transport;
            _uiConfig = uiConfig;

            // Monospace-font
            _font = new Font("Consolas", 12, FontStyle.Regular, GraphicsUnit.Pixel);

            Initialize(transport);

            this.Width = charWidth * _parser.Screenbuffer.Cols;
            this.Height = charHeight * _parser.Screenbuffer.Rows;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            this.TabStop = true;
            this.Focus();
        }

        /// <summary>
        /// Retreives the actual terminal parser instance, measures cell size, creates the render target that's needed to communicate with the rendering module,
        /// creates the caret controller and attaches the screen buffer instance retreived from terminal parser
        /// </summary>
        /// <param name="transport"></param>
        public void Initialize(Transport transport)
        {
            _parser = transport.GetParser();
            // Mät cellstorlek exakt
            charWidth = MeasureCharWidth(_font, _parser.Screenbuffer.Cols);
            charHeight = _font.Height;
            _renderTarget = new WinFormsRenderTarget(
                                   this.CreateGraphics(), _font, charWidth, charHeight,
                                   this.ForeColor, this.BackColor, _buffer);
            _renderTarget.Terminal = this;
            _caretController = new WinFormsCaretController(this, _renderTarget, _uiConfig.CursorStylePreference, _buffer, this);
            _parser.Screenbuffer.Scrolled += () => _core.ForceFullRender();
            AttachBuffer(_parser.Screenbuffer);
            _parser.Screenbuffer.AttachCaretController(_caretController);
        }

        /// <summary>
        /// Sets the buffer property to the buffer instance received from the terminal parser, also sets the corresponding property for the render target
        /// and adds a handler for the BufferUpdated event
        /// </summary>
        /// <param name="buffer"></param>
        public void AttachBuffer(ScreenBuffer buffer)
        {
            if (buffer != null)
            {
                _buffer = buffer;
                _renderTarget._buffer = buffer;
                _buffer.BufferUpdated += () =>
                {
                    if (this.IsHandleCreated && !this.IsDisposed)
                        this.Invalidate();
                };
            }
        }

        /// <summary>
        /// Override of the OnPaint event, as there are quite some more things that need to be done.
        /// Chekcs if a full redraw is to be done, checks that the screen buffer is established and returns if not, sets the properties Graphics and _buffer for render target,
        /// calls the rendering module with the actual buffer and rendertarget, checks the status of the caret and draws it if visible and if the Diag overlay is chosen draws a diavnostic string
        /// </summary>
        /// <param name="e"></param>
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


        /// <summary>
        /// Resize of the buffer when screen format is changed
        /// </summary>
        /// <param name="cols"></param>
        /// <param name="rows"></param>
        public void ChangeFormat(int cols, int rows)
        {
            _buffer.Resize(rows, cols);
            this.Invalidate();
        }

        /// <summary>
        /// Override of the OnResize event, returns if cell size is 0 otherwise performs the normal OnResize and then updates the buffer size
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {
            if (this.ClientSize.Height == 0 || this.ClientSize.Width == 0) return;
            base.OnResize(e);
            UpdateBufferFromSize();
        }

        /// <summary>
        /// Forces a complete redraw of the control
        /// </summary>
        public void ForceRepaint()
        {
            _core.ForceFullRender();  // tala om för renderern att allt ska ritas
            this.Invalidate();        // trigga OnPaint
        }

        /// <summary>
        /// Retreives the preferred size
        /// </summary>
        /// <param name="proposedSize"></param>
        /// <returns>Preferred size of the control</returns>
        public override Size GetPreferredSize(Size proposedSize)
        {
            return new Size(_buffer.Cols * charWidth, _buffer.Rows * charHeight);
        }

        /// <summary>
        /// Override of the OnLayout event that first checks if layout is ready and if so calls the base.OnLayout, otherwise just return
        /// </summary>
        /// <param name="levent"></param>
        protected override void OnLayout(LayoutEventArgs levent)
        {
            if (!_layoutReady)
            {
                _layoutReady = true;
                return;
            }

            base.OnLayout(levent);
        }

        /// <summary>
        /// Checks if key pressed is a non-character key that should be forwarded otherwise runs the base IsInputKey
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns>true if the key is a non-character key to return otherwise the base IsInputKey</returns>
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
                case Keys.Pause:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        /// <summary>
        /// Override of the OnKeyPress event, checks if the key pressed is a control character and returns if it is as those are handled in OnKeyDown.
        /// If input start column is not established sets the actual properties to avoid backing over prompts and other text supplied from the host and then persorms the base OnKeyPress
        /// Checks if the key pressed is one of the swedish letters and calls MapSwedishChar if so, else sends the actual key char to InputMapper
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            // Hoppa ur om det inte är ett skrivbart tecken
            if (char.IsControl(e.KeyChar)) return;
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
                Transport.Send(new byte[] { mapped });
            }
            else
            {
                // Hantera svenska tecken eller skicka vidare som text
                var bytes = InputMapper.MapText(e.KeyChar.ToString());
                Transport.Send(bytes);
            }
        }

        /// <summary>
        /// Override of the OnKeyDown event. Returns if the actual key pressed is Ctrl, Shift or Menu
        /// When a character is received we first check if one of the previously ignored modifiers is pressed and sets the mods variable accordingly,
        /// then we check if the modifier is Ctrl and whether the actual key pressed is one of the letters A...Z, makes a logical and with hex 1F (0001 1111) to get the actual control char to send.
        /// After that we check to see if the character is an unmodified character key and returns if it is, then we translate the WinForms key code to the corresponding scan code and send it to InputMapper.
        /// If InputMapper returns a byte and it's a backspace check the cursor position, if it's beyond the starting column perform the screen buffers backspace handling,
        /// then pass the actual byte on to the host. After that we check for Enter, Tab, Delete or Pause and sends the corresponding chars to the host for those keys.
        /// Last, but not least, we perform the base OnKeyDown.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // 1) Ignorera rena modifierare
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu)
                return;

            // 2) Sätt mods
            var mods = KeyModifiers.None;
            if (e.Shift) mods |= KeyModifiers.Shift;
            if (e.Control) mods |= KeyModifiers.Ctrl;
            if (e.Alt) mods |= KeyModifiers.Alt;

            // 3) Hantera Ctrl+A–Z innan vi kortsluter på printable keys
            if ((mods & KeyModifiers.Ctrl) != 0 && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
            {
                // Maskning ska ske på int; byt till byte först efter maskningen
                byte ctrlChar = (byte)((int)e.KeyCode & 0x1F);
                Transport.Send(new byte[] { ctrlChar });
                e.SuppressKeyPress = true; // hindra OnKeyPress från att skicka vanlig bokstav
                return;
            }

            // 4) Nu får printable keys kortslutas (vi tar dem i OnKeyPress)
            if (IsPrintableKey(e.KeyCode)) return;

            // 5) Din befintliga scancode-/specialhantering
            if (KeyToScanCode.TryGetValue(e.KeyCode, out int scanCode))
            {
                var keyEvent = new KeyEvent(scanCode, mods);
                var bytes = InputMapper.MapKey(keyEvent);

                if (bytes != null)
                {
                    if (e.KeyCode == Keys.Back && mods == KeyModifiers.None)
                    {
                        e.SuppressKeyPress = true;
                        if (IsCursorBeyondInputStart())
                        {
                            _buffer.Backspace();
                            Transport.Send(new byte[] { 0x08 });
                        }
                    }
                    else
                    {
                        Transport.Send(bytes);
                    }
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    inputStart = false;
                    e.SuppressKeyPress = true;
                    Transport.Send(Encoding.ASCII.GetBytes("\r\n"));
                }
                else if (e.KeyCode == Keys.Tab) Transport.Send(new byte[] { 0x09 });
                else if (e.KeyCode == Keys.Delete) Transport.Send(new byte[] { 0x7F });
                else if (e.KeyCode == Keys.Pause)
                    Transport.Send(new byte[] { 0x16 });

                base.OnKeyDown(e);
            }
        }

        /// <summary>
        /// Check to see if cursor is beyond the starting position of input
        /// </summary>
        /// <returns>true either if cursor row is higher than the starting row or both the cursor row and column is higher</returns>
        private bool IsCursorBeyondInputStart()
        {
            return _buffer.CursorRow > inputStartRow ||
                  (_buffer.CursorRow == inputStartRow && _buffer.CursorCol >= inputStartCol);
        }

        /// <summary>
        /// Check to see if the key pressed is a letter or not
        /// </summary>
        /// <param name="key"></param>
        /// <returns>true if key is a character, digit, space, dot, comma, minus, plus or any of the swedish characters</returns>
        private static bool IsPrintableKey(Keys key)
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

        /// <summary>
        /// Resize screen buffer accordint to the control size
        /// </summary>
        public void UpdateBufferFromSize()
        {
            if (charWidth == 0 || charHeight == 0) return;

            int cols = this.ClientSize.Width / charWidth;
            int rows = this.ClientSize.Height / charHeight;

            _buffer.Resize(rows, cols);
        }

        /// <summary>
        /// Sets the foreground and background color for the control to the supplied colors, then does the same for the render target and performs a force repaint for the colors to be applied
        /// </summary>
        /// <param name="fg"></param>
        /// <param name="bg"></param>
        public void RePaint(Color fg, Color bg)
        {
            try
            {
                this.ForeColor = fg;
                this.BackColor = bg;
            }
            catch (Exception ex)
            {
                this.LogWarning($"Control {this}, foreground {fg}, background {bg} generated exception {ex}");
            }
            _renderTarget.ChangeColor(fg, bg);
            ForceRepaint();
        }

        /// <summary>
        /// Sets the cursor style to the supplied style with blink on or off
        /// </summary>
        /// <param name="style"></param>
        /// <param name="blink"></param>
        public void SetCursorStyle(CursorStyle style, bool blink)
        {
            _caretController.SetCursorStyle(style, blink);
        }

        /// <summary>
        /// Measures character width according to supplied font and columns
        /// </summary>
        /// <param name="font"></param>
        /// <param name="columns"></param>
        /// <returns>character width</returns>
        private static int MeasureCharWidth(Font font, int columns)
        {
            // Make a test string with as many characters as there are columns and fill them with "W"
            string test = new string('W', columns);

            var size = TextRenderer.MeasureText(
                test,
                font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.NoClipping);

            // Divide size with columns to get actual character width
            return size.Width / columns;
        }

        /// <summary>
        /// Handle swedish characters on the keyboard
        /// </summary>
        /// <param name="c"></param>
        /// <returns>the corresponding ASCII character</returns>
        private static byte MapSwedishChar(char c)
        {
            return c switch
            {
                'Å' => (byte)']',  // 0x5B
                'Ö' => (byte)'\\', // 0x5C
                'Ä' => (byte)'[',  // 0x5D
                'å' => (byte)'}',  // 0x7B
                'ö' => (byte)'|',  // 0x7C
                'ä' => (byte)'{',  // 0x7D
                _ => (byte)c
            };
        }

        /// <summary>
        /// Translate colors from StyleInfo to Color
        /// </summary>
        /// <param name="color"></param>
        /// <returns>color equivalent of the StyleInfo color enum</returns>
        public static Color TranslateColor(StyleInfo.Color color) => color switch
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

    /// <summary>
    /// Render target for Windows Form that is supplied to the renderer
    /// </summary>
    public sealed class WinFormsRenderTarget : IRenderTarget
    {
        public Graphics Graphics { get; set; }
        internal TerminalCtrl Terminal { get; set; }
        private readonly Font _font;
        private int CharWidth;
        private int CharHeight;
        private Color _fore;
        private Color _back;
        public ScreenBuffer _buffer;
        internal bool _caretVisible;
        internal int _caretRow;
        internal int _caretCol;
        private CursorStyle _caretStyle;

        /// <summary>
        /// Constructor for the WinForm render target, sets the corresponding properties for the supplied parameters and flags if the font is null
        /// </summary>
        /// <param name="g"></param>
        /// <param name="font"></param>
        /// <param name="charWidth"></param>
        /// <param name="charHeight"></param>
        /// <param name="fore"></param>
        /// <param name="back"></param>
        /// <param name="buffer"></param>
        public WinFormsRenderTarget(Graphics g, Font font, int charWidth, int charHeight,
                                    Color fore, Color back, ScreenBuffer buffer)
        {
            _buffer = buffer;
            Graphics = g;
            _font = font;
            CharWidth = charWidth;
            CharHeight = charHeight;
            _fore = fore;
            _back = back;

            if (_font == null) this.LogWarning(Engine.LocalizationProvider.Current.Get("log.render.font.null"));
        }

        /// <summary>
        /// Clears screen
        /// </summary>
        public void Clear() => Graphics.Clear(_back);

        /// <summary>
        /// The main method to communicate with the renderer. Measures the width and height of the actual cell, retrieves attributes from ZoneAttributes.
        /// Sets flags for reverse video, low intensity, foreground and background color depending on the status of reverse video and low intensity.
        /// Paint the cell background, and then add the actual character.
        /// </summary>
        /// <param name="run"></param>
        public void DrawRun(RenderRun run)
        {
            int x = run.StartCol * CharWidth;
            int y = run.Row * CharHeight;

            var attr = _buffer.ZoneAttributes[run.Row, run.StartCol];
            var reverse = attr.ReverseVideo;
            var low = attr.LowIntensity;

            var fgBase = reverse ? _back : _fore;
            var bgBase = reverse ? _fore : _back;

            var fg = low ? DimColor(fgBase) : fgBase;
            var bg = bgBase;

            // 1. Fill the cell background
            using (var bgBrush = new SolidBrush(bg))
            {
                Graphics.FillRectangle(bgBrush, x, y, run.Chars.Length * CharWidth, CharHeight);
            }

            // 2. Paint the text without background
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

        /// <summary>
        /// Change control control colors to those supplied
        /// </summary>
        /// <param name="fg"></param>
        /// <param name="bg"></param>
        internal void ChangeColor(Color fg, Color bg)
        {
            _fore = fg;
            _back = bg;
        }

        /// <summary>
        /// Low intensity color
        /// </summary>
        /// <param name="color"></param>
        /// <param name="Factor"></param>
        /// <returns>the supplied color multiplied with the supplied factor (default 0.5)</returns>
        public static Color DimColor(Color color, double Factor = 0.5)
        {
            return Color.FromArgb(
                color.A,
                (int)(color.R * Factor),
                (int)(color.G * Factor),
                (int)(color.B * Factor));
        }

        /// <summary>
        /// Moves the caret to supplied row and column
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public void SetCaret(int row, int col)
        {
            _caretRow = row;
            _caretCol = col;
        }

        /// <summary>
        /// Draw caret att current position as either block, horizontal or vertical bar.
        /// </summary>
        /// <param name="g"></param>
        /// <param name="caretColor"></param>
        public void DrawCaret(Graphics g, Color caretColor)
        {
            if (!_caretVisible || _buffer == null) return;

            int x = _caretCol * CharWidth;
            int y = _caretRow * CharHeight;

            switch (_caretStyle)
            {
                case CursorStyle.Block:
                    using (var overlay = new SolidBrush(caretColor))
                    {
                        g.FillRectangle(overlay, x, y, CharWidth, CharHeight);
                    }
                    break;

                case CursorStyle.HorizontalBar:
                    g.FillRectangle(new SolidBrush(caretColor), x, y + CharHeight - 2, CharWidth, 2);
                    break;

                case CursorStyle.VerticalBar:
                    g.FillRectangle(new SolidBrush(caretColor), x, y, 2, CharHeight);
                    break;
            }
        }

        /// <summary>
        /// Sets caret style to that chosen
        /// </summary>
        /// <param name="style"></param>
        public void SetCaretStyle(CursorStyle style)
        {
            _caretStyle = style;
        }
    }


    /// <summary>
    /// Caret controller
    /// </summary>
    public sealed class WinFormsCaretController : ICaretController, IDisposable
    {
        private readonly Control _control;
        private readonly TerminalCtrl _terminalCtrl;
        private readonly WinFormsRenderTarget _renderTarget;
        private readonly System.Windows.Forms.Timer _blinkTimer;
        private int _row, _col;
        internal IRenderTarget.CursorStyle _style = IRenderTarget.CursorStyle.HorizontalBar;
        private ScreenBuffer _buffer;
        private bool _cursorBlink = true;

        /// <summary>
        /// Base constructor that just sets the control property to the one supplied
        /// </summary>
        /// <param name="control"></param>
        public WinFormsCaretController(Control control) => _control = control;

        /// <summary>
        /// Main constructor. Sets the corresponding properties to the arguments supplied and starts the blink timer for the cursor.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="renderTarget"></param>
        /// <param name="style"></param>
        /// <param name="buffer"></param>
        /// <param name="ctrl"></param>
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

        /// <summary>
        /// Moves caret to the specified position, calls the render target equivalent and flags the control for repaint
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        public void SetCaretPosition(int row, int col)
        {
            _row = row;
            _col = col;
            _renderTarget.SetCaret(_row, _col);
            _control.Invalidate();
        }

        /// <summary>
        /// Sets the cursor style to the one supplied and forwards that to the render target and if the cursor blink is turned off it stops the timer
        /// </summary>
        /// <param name="style"></param>
        /// <param name="blink"></param>
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
        /// <summary>
        /// Move the cursor supplied parameters rows and columns from the current position, mark the screen buffer as "dirty" and flag the control for repaint
        /// </summary>
        /// <param name="dRow"></param>
        /// <param name="dCol"></param>
        public void MoveCaret(int dRow, int dCol)
        {
            _row = _row + dRow;
            _col = _col + dCol;
            _buffer.MarkDirty();
            _control.Invalidate();
        }
        /// <summary>
        /// Makes the cursor visible
        /// </summary>
        public void Show()
        {
            _renderTarget._caretVisible = true;
            _blinkTimer.Start();
        }

        /// <summary>
        /// Makes the cursor invisible
        /// </summary>
        public void Hide()
        {
            _renderTarget._caretVisible = false;
            _blinkTimer.Stop();
            _control.Invalidate();
        }

        /// <summary>
        /// Clean up when closed
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}