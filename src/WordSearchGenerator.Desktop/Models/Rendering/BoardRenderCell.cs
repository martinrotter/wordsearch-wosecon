using System.Collections.ObjectModel;

namespace WordSearchGenerator.Desktop.Models.Rendering
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

    public BoardRenderCellKind Kind
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
      BoardRenderCellKind kind,
      char? character,
      int quizQuestionNumber,
      string? directionArrow,
      IEnumerable<int> wordNumbers)
    {
      ArgumentNullException.ThrowIfNull(wordNumbers);

      Row = row;
      Column = column;
      Kind = kind;
      Character = character;
      QuizQuestionNumber = quizQuestionNumber;
      DirectionArrow = directionArrow ?? string.Empty;
      WordNumbers = new ReadOnlyCollection<int>(
        wordNumbers.Distinct().Order().ToArray());
    }

    #endregion
  }
}
