using WordSearchGenerator.Common;

namespace WordSearchGenerator.Desktop.Models
{
  public sealed class GenerationResult
  {
    #region Properties

    public int BlackBoxCount
    {
      get;
    }

    public Board Board
    {
      get;
    }

    public int IntersectionCount => Board.IntersectionCount;

    public int MessageCellCount
    {
      get;
    }

    public int PuzzleCellCount
    {
      get;
    }

    public double PuzzleOccupancyPercentage =>
      Board.RowCount == 0 || Board.ColumnCount == 0
        ? 0
        : 100.0 * PuzzleCellCount / (Board.RowCount * Board.ColumnCount);

    public PuzzleDefinition Definition
    {
      get;
    }

    public TimeSpan Elapsed
    {
      get;
    }

    public long TestedPositions
    {
      get;
    }

    public int Backtrackings
    {
      get;
    }

    #endregion

    #region Constructors

    public GenerationResult(
      PuzzleDefinition definition,
      Board board,
      TimeSpan elapsed,
      long testedPositions,
      int backtrackings)
    {
      ArgumentNullException.ThrowIfNull(definition);
      ArgumentNullException.ThrowIfNull(board);

      Definition = definition;
      Board = board;
      Elapsed = elapsed;
      TestedPositions = testedPositions;
      Backtrackings = backtrackings;

      foreach (var cell in board.Matrix.OfType<Board.Cell>())
      {
        switch (cell.Type)
        {
          case Board.Cell.CellType.CharFromText:
          case Board.Cell.CellType.QuizQuestion:
            PuzzleCellCount++;
            break;

          case Board.Cell.CellType.CharFromMessage:
            MessageCellCount++;
            break;

          case Board.Cell.CellType.Empty:
            BlackBoxCount++;
            break;
        }
      }
    }

    #endregion
  }
}
