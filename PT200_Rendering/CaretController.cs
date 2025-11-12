using PT200_Parser;

namespace PT200_Rendering
{
    public class CaretController : ICaretController
    {
        public int Row { get; private set; }
        public int Col { get; private set; }
        public bool Visible { get; private set; }
        private ICaretController target;

        public CaretController(ICaretController target)
        {
            this.target = target;
            Row = 0;
            Col = 0;
            Visible = false;
        }

        public void SetCaretPosition(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public void MoveCaret(int rowDelta, int colDelta)
        {
            Row += rowDelta;
            Col += colDelta;
        }

        public void Show() => Visible = true;
        public void Hide() => Visible = false;
    }
}
