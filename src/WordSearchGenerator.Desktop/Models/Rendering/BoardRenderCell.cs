using System.Collections.ObjectModel;
using Wose.Common;

namespace Wose.Desktop.Models.Rendering
{
  public sealed class BoardRenderCell
  {
    #region Properties

    public char? Character
    {
      get;
    }

    public int Column
    {
      get;
    }

    public string DirectionArrow
    {
      get;
    }

    public bool IsIntersection => WordNumbers.Count > 1;

    public Board.Cell.CellType Kind
    {
      get;
    }

    public int? MessageIndex
    {
      get;
    }

    public int QuizQuestionNumber
    {
      get;
    }

    public int Row
    {
      get;
    }

    public IReadOnlyList<int> WordNumbers
    {
      get;
    }

    #endregion

    #region Constructors

    public BoardRenderCell(
      int row,
      int column,
      Board.Cell.CellType kind,
      char? character,
      int? messageIndex,
      int quizQuestionNumber,
      string? directionArrow,
      IEnumerable<int> wordNumbers)
    {
      ArgumentNullException.ThrowIfNull(wordNumbers);

      Row = row;
      Column = column;
      Kind = kind;
      Character = character;
      MessageIndex = messageIndex;
      QuizQuestionNumber = quizQuestionNumber;
      DirectionArrow = directionArrow ?? string.Empty;
      WordNumbers = new ReadOnlyCollection<int>(
        wordNumbers.Distinct().Order().ToArray());
    }

    #endregion
  }
}
