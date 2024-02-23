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

    public bool HtmlOutput
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
      get
      {
        int taken = 0;

        foreach (Cell cell in Matrix)
        {
          if (cell.Type == Cell.CellType.CharFromText)
          {
            taken++;
          }
        }

        return taken;
      }
    }

    public double PercentageOccupied
    {
      get => 100 * (double)CharCellCount / (RowCount * ColumnCount);
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

    private Random Rng
    {
      get;
    } = new Random((int)(DateTime.Now - DateTime.Today).TotalMilliseconds);

    public double BlindRate
    {
      get;
    }

    #endregion

    #region Constructors

    public Board(
      List<WordInfo> words,
      int rowCount,
      int columnCount,
      bool htmlOutput,
      double blindRate = default,
      string message = null)
    {
      Words = words;
      RowCount = rowCount;
      ColumnCount = columnCount;
      HtmlOutput = htmlOutput;
      Message = message;
      BlindRate = blindRate;

      GenerateBoard();
    }

    #endregion

    #region Other Stuff

    public string Print(bool showSolution)
    {
      return PrintBoard() + PrintWords(showSolution);
    }

    public string PrintBoard()
    {
      StringBuilder bldr = new StringBuilder();

      bldr.Append("   ❘ ");

      for (int j = 0; j < ColumnCount; j++)
      {
        bldr.Append(j.ToString("00"));
      }

      bldr.AppendLine();
      bldr.Append(new string('-', bldr.Length - 1));
      bldr.AppendLine();

      for (int i = 0; i < RowCount; i++)
      {
        bldr.Append($"{i:00} ❘ ");

        for (int j = 0; j < ColumnCount; j++)
        {
          Cell cell = Matrix[i, j];

          bldr.Append(cell.Type == Cell.CellType.Empty ? " -" : $" {cell.Char}");
        }

        bldr.AppendLine();
      }

      bldr.AppendLine();
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
            bldr.AppendLine($"Crossing: {i}x{j} -> {Matrix[i, j].Intersections}");
          }
        }
      }

      bldr.AppendLine();

      return bldr.ToString();
    }

    public string PrintWords(bool showSolution)
    {
      StringBuilder bldr = new StringBuilder();
      int longestWord = Words.Max(wrd => wrd.PrintableText.Length);

      foreach (WordInfo word in Words.OrderBy(wrd => wrd.PrintableText.ToLower()))
      {
        bldr.Append(word.ToString(longestWord, showSolution));
      }

      bldr.AppendLine();

      return bldr.ToString();
    }

    private void GenerateBoard()
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
        List<DirectedLocation> locations = word.GetAllLetterLocations();

        if (!locations.Any())
        {
          throw new Exception("no locations");
        }

        string wordText = word.Text;

        for (int j = 0; j < wordText.Length; j++)
        {
          DirectedLocation letterLocation = locations[j];
          int r = letterLocation.Row;
          int c = letterLocation.Column;

          Cell cell = Matrix[r, c];

          cell.Type = Cell.CellType.CharFromText;
          cell.Words.Add(word);

          if (cell.Char == default)
          {
            cell.Char = ShouldBeBlind() ? ' ' : wordText[j];
          }
        }
      }

      for (int i = 0; i < RowCount; i++)
      for (int j = 0; j < ColumnCount; j++)
      {
        if (Matrix[i, j].Type != Cell.CellType.Empty)
        {
          continue;
        }

        if (messageChars.Count == 0)
        {
          break;
        }

        Matrix[i, j].Type = Cell.CellType.CharFromMessage;
        Matrix[i, j].Char = messageChars.TakeFirst();
      }

      if (messageChars.Count > 0)
      {
        throw new Exception($"message is too long, {messageChars.Count} characters remain to be placed");
      }
    }

    private bool ShouldBeBlind()
    {
      return BlindRate > 0.0 && Rng.NextDouble() <= BlindRate;
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
        CharFromMessage
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
      } = new List<WordInfo>();

      public CellType Type
      {
        get;
        set;
      } = CellType.Empty;

      #endregion
    }

    #endregion
  }
}