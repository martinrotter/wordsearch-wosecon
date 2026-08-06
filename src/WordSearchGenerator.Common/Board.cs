using System.Text;
using WordSearchGenerator.Common.WoSeCon.Api;

namespace WordSearchGenerator.Common
{
  public class Board
  {
    #region Properties

    public int ColumnCount
    {
      get;
    }

    public bool QuizMode
    {
      get;
    }

    public Cell[,] Matrix
    {
      get;
      private set;
    }

    public int CharCellCount
    {
      get => Matrix.OfType<Cell>().Count(cell => cell.Words.Count > 0);
    }

    public double PercentageOccupied
    {
      get => RowCount == 0 || ColumnCount == 0
        ? 0
        : 100 * (double)CharCellCount / (RowCount * ColumnCount);
    }

    public int IntersectionCount
    {
      get => Matrix.OfType<Cell>().Count(cl => cl.Intersections >= 2);
    }

    public string Message
    {
      get;
    }

    public int RowCount
    {
      get;
    }

    public List<WordInfo> Words
    {
      get;
    }

    #endregion

    #region Constructors

    public Board(
      List<WordInfo> words,
      int rowCount,
      int columnCount,
      bool quizMode,
      string message = null)
    {
      Words = words;
      RowCount = rowCount;
      ColumnCount = columnCount;
      QuizMode = quizMode;
      Message = message;

      FillBoard();
    }

    #endregion

    #region Other Stuff

    public string PrintBoard()
    {
      StringBuilder bldr = new StringBuilder();

      bldr.Append("    ");

      for (int column = 0; column < ColumnCount; column++)
      {
        bldr.Append($"{column,2}");
      }

      bldr.AppendLine();
      bldr.Append("   +");
      bldr.AppendLine(new string('-', ColumnCount * 2));

      for (int row = 0; row < RowCount; row++)
      {
        bldr.Append($"{row,2} | ");

        for (int column = 0; column < ColumnCount; column++)
        {
          Cell cell = Matrix[row, column];

          bldr.Append(cell.Type switch
          {
            Cell.CellType.Empty => ". ",
            Cell.CellType.QuizWordPlaceholder =>
              $"{DirectedLocation.GetArrowForDirection(cell.QuizWordDirection)} ",
            _ => $"{cell.Char} "
          });
        }

        bldr.AppendLine();
      }

      return bldr.ToString();
    }

    public string PrintDiagnostics()
    {
      StringBuilder bldr = new StringBuilder();

      bldr.AppendLine($"Board: {RowCount}x{ColumnCount}");
      bldr.Append(PrintBoard());
      bldr.AppendLine($"Words ({Words.Count}):");
      bldr.Append(PrintWords(true));
      bldr.AppendLine($"Intersections: {IntersectionCount}");
      bldr.Append(PrintIntersections());
      bldr.AppendLine(
        $"Occupied: {CharCellCount}/{RowCount * ColumnCount} ({PercentageOccupied:F2}%)");

      return bldr.ToString();
    }

    public string PrintIntersections()
    {
      StringBuilder bldr = new StringBuilder();

      for (int i = 0; i < RowCount; i++)
      {
        for (int j = 0; j < ColumnCount; j++)
        {
          if (Matrix[i, j].Intersections >= 2)
          {
            Cell cell = Matrix[i, j];
            string words = string.Join(", ", cell.Words.Select(word => word.Text));

            bldr.AppendLine(
              $"  ({i},{j}) '{cell.Char}': {cell.Intersections} words [{words}]");
          }
        }
      }

      if (bldr.Length == 0)
      {
        bldr.AppendLine("  none");
      }

      return bldr.ToString();
    }

    public string PrintWords(bool showSolution)
    {
      StringBuilder bldr = new StringBuilder();
      int fallbackNumber = 1;

      foreach (WordInfo word in Words)
      {
        int wordNumber = word.WordNumber > 0 ? word.WordNumber : fallbackNumber;
        string printableText = string.IsNullOrWhiteSpace(word.PrintableText)
          ? word.Text
          : word.PrintableText;

        bldr.Append($"  {wordNumber,2}. {printableText}");

        if (showSolution)
        {
          if (word.Placement == null)
          {
            bldr.Append(" [unplaced]");
          }
          else
          {
            bldr.Append(
              $" @ ({word.Placement.Row},{word.Placement.Column}) " +
              $"{DirectedLocation.GetArrowForDirection(word.Placement.Direction)} " +
              word.Placement.Direction);
          }
        }

        bldr.AppendLine();
        fallbackNumber++;
      }

      return bldr.ToString();
    }

    private void FillBoard()
    {
      List<char> messageChars = Message == null ? [] : Message.ToCharArray().ToList();
      Matrix = new Cell[RowCount, ColumnCount];

      for (int i = 0; i < RowCount; i++)
      for (int j = 0; j < ColumnCount; j++)
      {
        Matrix[i, j] = new Cell();
      }

      foreach (WordInfo word in Words)
      {
        if (word.Placement == null)
        {
          continue;
        }

        List<DirectedLocation> locations = word.GetAllLetterLocations();
        string wordText = word.Text;

        for (int j = 0; j < wordText.Length; j++)
        {
          DirectedLocation letterLocation = locations[j];
          int r = letterLocation.Row;
          int c = letterLocation.Column;

          Cell cell = Matrix[r, c];

          cell.Type = Cell.CellType.CharFromText;
          cell.Words.Add(word);

          if (j == 0 && QuizMode)
          {
            cell.Type = Cell.CellType.QuizWordPlaceholder;
            cell.QuizWordNumber = word.WordNumber;
            cell.QuizWordDirection = word.Placement.Direction;
          }
          else if (cell.Char == default)
          {
            cell.Char = wordText[j];
          }
        }
      }

      for (int i = 0; i < RowCount; i++)
      for (int j = 0; j < ColumnCount; j++)
      {
        if (messageChars.Count == 0)
        {
          break;
        }

        if (Matrix[i, j].Type != Cell.CellType.Empty)
        {
          continue;
        }

        Matrix[i, j].Type = Cell.CellType.CharFromMessage;
        Matrix[i, j].Char = messageChars.TakeFirst();
      }

      if (messageChars.Count > 0)
      {
        throw new Exception($"message is too long, {messageChars.Count} characters remain to be placed");
      }
    }

    #endregion

    #region Nested Types

    public class Cell
    {
      #region Enums

      public enum CellType
      {
        // Empty cell -> no character from any word is on it.
        Empty,

        // Char from found word(s).
        CharFromText,

        // Char from message.
        CharFromMessage,

        // Special placeholder placed in front of the word.
        // The cell will hold number and direction of the guessed word.
        QuizWordPlaceholder
      }

      #endregion

      #region Properties

      public char Char
      {
        get;
        set;
      }

      public int Intersections
      {
        get => Words.Count;
      }

      public List<WordInfo> Words
      {
        get;
      } = [];

      public DirectedLocation.LocationDirection QuizWordDirection
      {
        get;
        set;
      }

      public CellType Type
      {
        get;
        set;
      } = CellType.Empty;

      public int QuizWordNumber
      {
        get;
        set;
      }

      #endregion
    }

    #endregion
  }
}
