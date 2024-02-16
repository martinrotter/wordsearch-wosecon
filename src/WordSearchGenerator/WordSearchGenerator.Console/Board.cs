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

    public string Message
    {
      get;
    }

    public char[,] Matrix
    {
      get;
      private set;
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
          bldr.Append($" {Matrix[i, j]}");
        }

        bldr.AppendLine();
      }

      bldr.AppendLine();
      return bldr.ToString();
    }

    public string PrintWords()
    {
      StringBuilder bldr = new StringBuilder();
      int longestWord = Words.Max(wrd => wrd.Text.Length);

      foreach (WordInfo word in Words.OrderBy(wrd => wrd.NormalizedText.ToLower()))
      {
        bldr.AppendLine(word.ToString(longestWord));
      }

      return bldr.ToString();
    }

    private void GenerateBoard()
    {
      List<char> messageChars = Message == null ? [] : Message.ToCharArray().ToList();
      Matrix = new char[RowCount, ColumnCount];
      char emptyChar = '-';

      for (int i = 0; i < RowCount; i++)
      {
        for (int j = 0; j < ColumnCount; j++)
        {
          Matrix[i, j] = emptyChar;
        }

        //else board[i][j] = 'A' + rand() % 26;
      }

      //Finds the placement of every word character and updates the board
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

          Matrix[r, c] = word[j];
        }
      }

      for (int i = 0; i < RowCount; i++)
      {
        for (int j = 0; j < ColumnCount; j++)
        {
          if (Matrix[i, j] != emptyChar)
          {
            continue;
          }

          if (messageChars.Count == 0)
          {
            break;
          }

          Matrix[i, j] = char.ToLower(messageChars.TakeFirst());
        }
      }

      if (messageChars.Count > 0)
      {
        throw new Exception($"message is too long, {messageChars.Count} characters remain to be placed");
      }
    }

    #endregion
  }
}