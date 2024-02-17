using System.Text;
using WordSearchGenerator.Common.WoSeCon.Data;

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
      var bldr = new StringBuilder();

      bldr.Append("   \u239c ");

      for (var j = 0; j < ColumnCount; j++)
      {
        bldr.Append(j.ToString("00"));
      }

      bldr.AppendLine();
      bldr.Append(new string('―', bldr.Length - 2));
      bldr.AppendLine();

      for (var i = 0; i < RowCount; i++)
      {
        bldr.Append($"{i:00} \u2758 ");

        for (var j = 0; j < ColumnCount; j++)
        {
          var cell = Matrix[i, j];

          bldr.Append(cell.Type == Cell.CellType.Empty ? " -" : $" {cell.Char}");
        }

        bldr.AppendLine();
      }

      bldr.AppendLine();
      return bldr.ToString();
    }

    public string PrintWords(bool showSolution)
    {
      var bldr = new StringBuilder();
      var longestWord = Words.Max(wrd => wrd.Text.Length);

      foreach (var word in Words.OrderBy(wrd => wrd.NormalizedText.ToLower()))
      {
        bldr.Append(word.ToString(longestWord, showSolution));
      }

      return bldr.ToString();
    }

    private void GenerateBoard()
    {
      var messageChars = Message == null ? [] : Message.ToCharArray().ToList();
      Matrix = new Cell[RowCount, ColumnCount];

      for (var i = 0; i < RowCount; i++)
      for (var j = 0; j < ColumnCount; j++)
      {
        Matrix[i, j] = new Cell();
      }

      for (var i = 0; i < Words.Count; i++)
      {
        var cWord = Words[i];
        var locations = cWord.GetAllLocations();

        if (!locations.Any())
        {
          throw new Exception("no locations");
        }

        var word = cWord.Text;

        for (var j = 0; j < word.Length; j++)
        {
          var dL = locations[j];
          var r = dL.Row;
          var c = dL.Column;

          Matrix[r, c].Type = Cell.CellType.CharFromText;
          Matrix[r, c].Char = word[j];
        }
      }

      for (var i = 0; i < RowCount; i++)
      for (var j = 0; j < ColumnCount; j++)
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

      #region Nested Types

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
    }

    #endregion
  }
}