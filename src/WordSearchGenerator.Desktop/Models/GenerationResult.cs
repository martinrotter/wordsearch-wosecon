using WordSearchGenerator.Common;

namespace WordSearchGenerator.Desktop.Models
{
  public sealed class GenerationResult
  {
    #region Properties

    public int AttemptCount
    {
      get;
    }

    public long Backtrackings
    {
      get;
    }

    public long AmbiguousBoardRejectionCount
    {
      get;
    }

    public int AmbiguityRejectedAttemptCount
    {
      get;
    }

    public int BlackBoxCount
    {
      get;
    }

    public Board Board
    {
      get;
    }

    public int CancelledAttemptCount
    {
      get;
    }

    public long CompletedCandidateCount
    {
      get;
    }

    public PuzzleDefinition Definition
    {
      get;
    }

    public TimeSpan Elapsed
    {
      get;
    }

    public int IntersectionCount => Board.IntersectionCount;

    public int MessageCellCount
    {
      get;
    }

    public long MessageCapacityRejectionCount
    {
      get;
    }

    public int MessageCapacityRejectedAttemptCount
    {
      get;
    }

    public int PlacementFailedAttemptCount
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

    public long TestedPositions
    {
      get;
    }

    public int WinningAttemptBacktrackings
    {
      get;
    }

    public TimeSpan WinningAttemptElapsed
    {
      get;
    }

    public int WinningAttemptNumber
    {
      get;
    }

    public long WinningAttemptTestedPositions
    {
      get;
    }

    public int WinningSeed
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
      long backtrackings,
      int attemptCount,
      int winningAttemptNumber,
      int winningSeed,
      TimeSpan winningAttemptElapsed,
      long winningAttemptTestedPositions,
      int winningAttemptBacktrackings,
      int placementFailedAttemptCount,
      int messageCapacityRejectedAttemptCount,
      int ambiguityRejectedAttemptCount,
      long completedCandidateCount,
      long messageCapacityRejectionCount,
      long ambiguousBoardRejectionCount,
      int cancelledAttemptCount)
    {
      ArgumentNullException.ThrowIfNull(definition);
      ArgumentNullException.ThrowIfNull(board);

      Definition = definition;
      Board = board;
      Elapsed = elapsed;
      TestedPositions = testedPositions;
      Backtrackings = backtrackings;
      AttemptCount = attemptCount;
      WinningAttemptNumber = winningAttemptNumber;
      WinningSeed = winningSeed;
      WinningAttemptElapsed = winningAttemptElapsed;
      WinningAttemptTestedPositions = winningAttemptTestedPositions;
      WinningAttemptBacktrackings = winningAttemptBacktrackings;
      PlacementFailedAttemptCount = placementFailedAttemptCount;
      MessageCapacityRejectedAttemptCount = messageCapacityRejectedAttemptCount;
      AmbiguityRejectedAttemptCount = ambiguityRejectedAttemptCount;
      CompletedCandidateCount = completedCandidateCount;
      MessageCapacityRejectionCount = messageCapacityRejectionCount;
      AmbiguousBoardRejectionCount = ambiguousBoardRejectionCount;
      CancelledAttemptCount = cancelledAttemptCount;

      foreach (var cell in board.Matrix.OfType<Board.Cell>())
      {
        switch (cell.Type)
        {
          case Board.Cell.CellType.CharFromText:
            PuzzleCellCount++;

            if (cell.MessageIndex != null)
            {
              MessageCellCount++;
            }

            break;

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
