using System.Text;
using WordSearchGenerator.Common;
using WordSearchGenerator.Common.WoSeCon.Data;

namespace WordSearchGenerator.Console
{
  public class Board
  {
    #region Vlastnosti

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

    #region Konstruktory

    public Board(List<WordInfo> words, int rowCount, int columnCount, string message = null)
    {
      Words = words;
      RowCount = rowCount;
      ColumnCount = columnCount;
      Message = message;

      GenerateBoard();
    }

    #endregion

    #region Metody

    public string Print()
    {
      StringBuilder bldr = new StringBuilder();

      bldr.Append("   | ");

      for (int j = 0; j < ColumnCount; j++)
      {
        bldr.Append(j.ToString("00"));
      }

      bldr.AppendLine();
      bldr.Append(new string('-', bldr.Length - 2));
      bldr.AppendLine();

      for (int i = 0; i < RowCount; i++)
      {
        bldr.Append($"{i:00} | ");

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

    public void PrintToConsole()
    {
      System.Console.Write("   | ");

      for (int j = 0; j < ColumnCount; j++)
      {
        System.Console.Write(j.ToString("00"));
      }

      System.Console.WriteLine();
      System.Console.Write(new string('-', ColumnCount * 2 + 5));
      System.Console.WriteLine();

      for (int i = 0; i < RowCount; i++)
      {
        System.Console.Write($"{i:00} | ");

        for (int j = 0; j < ColumnCount; j++)
        {
          Cell cell = Matrix[i, j];

          switch (cell.Type)
          {
            case Cell.CellType.Empty:
              System.Console.Write(" -");
              break;

            case Cell.CellType.CharFromMessage:
              ConsoleUtils.WithBgColor(() => { System.Console.Write($" {cell.Char}"); }, ConsoleColor.Red);
              break;

            default:
              System.Console.Write($" {Matrix[i, j].Char}");
              break;
          }
        }

        System.Console.WriteLine();
      }

      System.Console.WriteLine();
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

    public void PrintWordsToConsole(bool showSolution)
    {
      int longestWord = Words.Max(wrd => wrd.Text.Length);

      foreach (WordInfo word in Words.OrderBy(wrd => wrd.NormalizedText.ToLower()))
      {
        System.Console.Write(word.ToString(longestWord, showSolution));
      }
    }

    private void GenerateBoard()
    {
      List<char> messageChars = Message == null ? [] : Message.ToCharArray().ToList();
      Matrix = new Cell[RowCount, ColumnCount];
      char emptyChar = '-';

      for (int i = 0; i < RowCount; i++)
      {
        for (int j = 0; j < ColumnCount; j++)
        {
          Matrix[i, j] = new Cell();
        }
      }

      for (int i = 0; i < Words.Count; i++)
      {
        WordInfo cWord = Words[i];
        List<DirectedLocation> locations = cWord.GetAllLocations();

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
      {
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
      }

      if (messageChars.Count > 0)
      {
        throw new Exception($"message is too long, {messageChars.Count} characters remain to be placed");
      }
    }

    #endregion

    #region Vnořené typy

    public class Cell
    {
      #region Enumy

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

      #region Vlastnosti

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