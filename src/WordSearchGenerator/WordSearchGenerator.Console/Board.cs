using System.Text;
using WordSearchGenerator.Common.WoSeCon.Data;

namespace WordSearchGenerator.Console
{
  public class Board
  {
    public List<WordInfo> Words
    {
      get;
    }

    public int RowCount
    {
      get;
    }

    public int ColumnCount
    {
      get;
    }

    public Board(List<WordInfo> words, int rowCount, int columnCount)
    {
      Words = words;
      RowCount = rowCount;
      ColumnCount = columnCount;

      GenerateBoard();
    }

    public char[,] Matrix
    {
      get;
      private set;
    }

    private void GenerateBoard()
    {
      Matrix = new char[RowCount, ColumnCount];

      //creates board, a BOARDLINESxBOARDCLMNS dynamic array of characters
      //input: a list of WordInfo objects. Each WordInfo keeps information for its content placement on the Board
      // It initializes board elements with '-' character if test is on, otherwise with a random character. 

      for (int i = 0; i < RowCount; i++) {
        for (int j = 0; j < ColumnCount; j++)
          Matrix[i, j] = '-';
          //else board[i][j] = 'A' + rand() % 26;
      }

      //Finds the placement of every word character and updates the board
      for (int i = 0; i < Words.Count; i++) {
        WordInfo cWord = Words[i];
        List<DirectedLocation> locations = cWord.GetAllLocations();

        if (!locations.Any())
        {
          throw new Exception("no locations");
        }
        string word = cWord.Text;

        for (int j = 0; j < word.Length; j++) {
          DirectedLocation dL = locations[j];
          int r = dL.Row;
          int c = dL.Column;

          Matrix[r, c] = word[j];
        }
      }
    }

    public string Print()
    {
      StringBuilder bldr = new StringBuilder();

      bldr.Append("   | ");
      for (int j = 0; j < ColumnCount; j++)
      {
        bldr.Append($" {j}");
      }

      bldr.AppendLine();
      bldr.Append("-----");

      for (int j = 0; j < ColumnCount; j++)
      {
        bldr.Append($"--");
      }

      bldr.AppendLine();

      for (int i = 0; i < RowCount; i++)
      {
        bldr.Append($"{i}  | ");

        for (int j = 0; j < ColumnCount; j++)
        {
          bldr.Append($" {Matrix[i, j]}");
        }

        bldr.AppendLine();
      }

      bldr.AppendLine();
      return bldr.ToString();
    }
  }
}
