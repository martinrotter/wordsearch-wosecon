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

    public Board(List<WordInfo> words, int rowCount, int columnCount, string message = null)
    {
      Words = words;
      RowCount = rowCount;
      ColumnCount = columnCount;
      Message = message;

      GenerateBoard();
    }

    #endregion

    #region Other Stuff

    public string Print()
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

    public string PrintWords(bool showSolution)
    {
      StringBuilder bldr = new StringBuilder();
      int longestWord = Words.Max(wrd => wrd.Text.Length);

      foreach (WordInfo word in Words.OrderBy(wrd => wrd.NormalizedText.ToLower()))
      {
        bldr.Append(word.ToString(longestWord, showSolution));
      }

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

      for (int i = 0; i < Words.Count; i++)
      {
        WordInfo cWord = Words[i];
        List<DirectedLocation> locations = cWord.GetAllLetterLocations();

        if (!locations.Any())
        {
          throw new Exception("no locations");
        }

        string word = cWord.Text;

        for (int j = 0; j < word.Length; j++)
        {
          DirectedLocation dL = locations[j];
          int r = dL.Row;
          int c = dL.Column;

          Matrix[r, c].Type = Cell.CellType.CharFromText;
          Matrix[r, c].Char = word[j];
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

    #endregion

    #region Nested Types

    public class Cell
    {
      #region Enums

      public enum CellType
      {
        // Empty cell.
        Empty,

        // Char from found word.
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