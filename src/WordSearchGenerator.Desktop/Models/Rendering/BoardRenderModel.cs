using System.Collections.ObjectModel;
using WordSearchGenerator.Common;
using WordSearchGenerator.Common.WoSeCon.Api;

namespace WordSearchGenerator.Desktop.Models.Rendering
{
  public sealed class BoardRenderModel
  {
    #region Properties

    public int BlackBoxCount
    {
      get;
    }

    public IReadOnlyList<BoardRenderCell> Cells
    {
      get;
    }

    public int Columns
    {
      get;
    }

    public IReadOnlyList<BoardRenderEntry> Entries
    {
      get;
    }

    public string EntryListHeading
    {
      get;
    }

    public int IntersectionCount
    {
      get;
    }

    public int MessageCellCount
    {
      get;
    }

    public PuzzleMode Mode
    {
      get;
    }

    public string PuzzleHeading
    {
      get;
    }

    public int PuzzleCellCount
    {
      get;
    }

    public int Rows
    {
      get;
    }

    #endregion

    #region Constructors

    private BoardRenderModel(
      PuzzleMode mode,
      int rows,
      int columns,
      IEnumerable<BoardRenderCell> cells,
      IEnumerable<BoardRenderEntry> entries,
      string puzzleHeading,
      string entryListHeading,
      int puzzleCellCount,
      int messageCellCount,
      int blackBoxCount,
      int intersectionCount)
    {
      Mode = mode;
      Rows = rows;
      Columns = columns;
      Cells = new ReadOnlyCollection<BoardRenderCell>(cells.ToArray());
      Entries = new ReadOnlyCollection<BoardRenderEntry>(entries.ToArray());
      PuzzleHeading = puzzleHeading;
      EntryListHeading = entryListHeading;
      PuzzleCellCount = puzzleCellCount;
      MessageCellCount = messageCellCount;
      BlackBoxCount = blackBoxCount;
      IntersectionCount = intersectionCount;
    }

    #endregion

    #region Other Stuff

    public static BoardRenderModel Create(
      GenerationResult result,
      string? puzzleHeading = null,
      string? entryListHeading = null)
    {
      ArgumentNullException.ThrowIfNull(result);

      var board = result.Board;
      var cells = new List<BoardRenderCell>(board.RowCount * board.ColumnCount);

      for (var row = 0; row < board.RowCount; row++)
      for (var column = 0; column < board.ColumnCount; column++)
      {
        var sourceCell = board.Matrix[row, column];
        var kind = GetCellKind(sourceCell.Type);
        char? character = kind is BoardRenderCellKind.Word or
          BoardRenderCellKind.Message
          ? sourceCell.Char
          : null;
        var directionArrow = kind == BoardRenderCellKind.QuizQuestion
          ? DirectedLocation.GetArrowForDirection(
            sourceCell.QuizWordDirection).ToString()
          : string.Empty;

        cells.Add(new BoardRenderCell(
          row,
          column,
          kind,
          character,
          sourceCell.QuizWordNumber,
          directionArrow,
          sourceCell.Words.Select(word => word.WordNumber)));
      }

      var entries = result.Definition.Entries
        .Select((entry, index) => new BoardRenderEntry(
          index + 1,
          entry.Answer,
          entry.Question));

      return new BoardRenderModel(
        result.Definition.Mode,
        board.RowCount,
        board.ColumnCount,
        cells,
        entries,
        (puzzleHeading ?? result.Definition.PuzzleHeading).Trim(),
        (entryListHeading ?? result.Definition.EntryListHeading).Trim(),
        result.PuzzleCellCount,
        result.MessageCellCount,
        result.BlackBoxCount,
        result.IntersectionCount);
    }

    private static BoardRenderCellKind GetCellKind(Board.Cell.CellType type)
    {
      return type switch
      {
        Board.Cell.CellType.Empty => BoardRenderCellKind.Empty,
        Board.Cell.CellType.CharFromText => BoardRenderCellKind.Word,
        Board.Cell.CellType.CharFromMessage => BoardRenderCellKind.Message,
        Board.Cell.CellType.QuizQuestion =>
          BoardRenderCellKind.QuizQuestion,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
      };
    }

    #endregion
  }
}
