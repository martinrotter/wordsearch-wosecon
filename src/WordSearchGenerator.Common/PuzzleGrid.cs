namespace Wose.Common
{
  public abstract class PuzzleGrid
  {
    #region Properties

    public int Columns
    {
      get;
    }

    public PuzzleMode Mode
    {
      get;
    }

    public bool QuizMode => Mode == PuzzleMode.Quiz;

    public int Rows
    {
      get;
    }

    #endregion

    #region Constructors

    protected PuzzleGrid(PuzzleMode mode, int rows, int columns)
    {
      if (!Enum.IsDefined(mode))
      {
        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
      }

      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);

      Mode = mode;
      Rows = rows;
      Columns = columns;
    }

    #endregion
  }
}
