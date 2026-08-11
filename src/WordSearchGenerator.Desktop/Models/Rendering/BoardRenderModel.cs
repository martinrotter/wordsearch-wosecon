using System.Collections.ObjectModel;
using Wose.Common;
using Wose.Common.WoSeCon.Api;

namespace Wose.Desktop.Models.Rendering
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

    public string SecretMessage
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
      string secretMessage,
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
      SecretMessage = secretMessage;
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
      var cells = new List<BoardRenderCell>(board.Rows * board.Columns);

      for (var row = 0; row < board.Rows; row++)
      for (var column = 0; column < board.Columns; column++)
      {
        var sourceCell = board.Matrix[row, column];
        var kind = sourceCell.Type;
        char? character = kind is Board.Cell.CellType.CharFromText or
          Board.Cell.CellType.CharFromMessage
          ? sourceCell.Char
          : null;
        var directionArrow = kind == Board.Cell.CellType.QuizQuestion
          ? DirectedLocation.GetArrowForDirection(
            sourceCell.QuizWordDirection).ToString()
          : string.Empty;

        cells.Add(new BoardRenderCell(
          row,
          column,
          kind,
          character,
          sourceCell.MessageIndex,
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
        board.Rows,
        board.Columns,
        cells,
        entries,
        (puzzleHeading ?? result.Definition.PuzzleHeading).Trim(),
        (entryListHeading ?? result.Definition.EntryListHeading).Trim(),
        result.Definition.SecretMessage,
        result.PuzzleCellCount,
        result.MessageCellCount,
        result.BlackBoxCount,
        result.IntersectionCount);
    }

    #endregion
  }
}
