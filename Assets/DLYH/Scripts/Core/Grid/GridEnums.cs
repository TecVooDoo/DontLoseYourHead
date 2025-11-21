namespace TecVooDoo.DontLoseYourHead.Core
{
    public enum CellState
    {
        Hidden,
        Revealed,
        Miss,
        PartiallyKnown
    }

    public enum WordDirection
    {
        Horizontal,
        Vertical,
        DiagonalDownRight,
        DiagonalDownLeft,
        HorizontalReverse,
        VerticalReverse,
        DiagonalUpRight,
        DiagonalUpLeft
    }
}