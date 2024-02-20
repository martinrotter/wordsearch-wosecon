using System.Diagnostics;
using WordSearchGenerator.Common;
using WordSearchGenerator.Common.WoSeCon;
using WordSearchGenerator.Common.WoSeCon.Data;

namespace WordSearchGenerator.Tests
{
  [TestClass]
  public class UnitTests
  {
    #region Properties

    private string TestDataFolder
    {
      get;
    } = @"..\..\..\..\..\test-data";

    private int NumberOfRepetitions
    {
      get;
    } = 20;

    #endregion

    #region Other Stuff

    [TestMethod]
    public void OnceFullListBigGrid()
    {
      RunGridWithWords(1, 130, 23, 27);
    }

    [TestMethod]
    public void FullListBigGrid()
    {
      RunGridWithWords(NumberOfRepetitions, 130, 22, 30);
    }

    [TestMethod]
    public void VerySmallListSmallGrid()
    {
      RunGridWithWords(NumberOfRepetitions, 10, 6, 8);
    }

    [TestMethod]
    public void SmallListBigGrid()
    {
      RunGridWithWords(NumberOfRepetitions, 14, 11, 11);
    }

    [TestMethod]
    public void BigListBigGrid()
    {
      RunGridWithWords(NumberOfRepetitions, 25, 9, 16);
    }

    [TestMethod]
    public void FillAllGrid()
    {
      RunGridWithWords(1, -1, 2, 2, "words-tiny.txt", false);
    }

    [TestMethod]
    public void FillAllGrid2()
    {
      RunGridWithWords(1, -1, 3, 3, "words-small.txt", false);
    }

    [TestMethod]
    public void Svesedlice()
    {
      RunGridWithWords(1, -1, 9, 16, "svesedlice.txt");
    }

    private void RunGridWithWords(int numberOfRepetitions, int numberOfWords, int rows, int columns, string fileName = null, bool twist = true)
    {
      var iter = numberOfRepetitions;
      var stats = new Stats();
      var st = new Stopwatch();
      var fullFilePath = Path.Combine(TestDataFolder, fileName ?? "words-big.txt");
      var loader = new WordsLoader(fullFilePath);
      var words = numberOfWords > 0 ? loader.Words.Take(numberOfWords).ToList() : loader.Words;
      var charCount = words.Select(wrd => wrd.Text.Length).Sum();
      List<WordInfo> hardestWords = null;
      var hardestBacktrackings = 0;

      while (--iter >= 0)
      {
        var wo = new WoSeCon(words.CloneList(), rows, columns, twist);

        st.Restart();
        var backtrackings = wo.Construct();

        var elapsed = st.ElapsedMilliseconds;

        stats.SetResult(elapsed);

        if (elapsed == stats.MaxMs)
        {
          hardestWords = wo.Words;
          hardestBacktrackings = backtrackings;
        }

        Debug.WriteLine($"Left: {iter + 1}");
      }

      var board = new Board(hardestWords, rows, columns);

      Console.WriteLine($"Total cell count: {rows * columns}");
      Console.WriteLine($"Words char count: {charCount}");
      Console.WriteLine($"Char cell count: {board.CharCellCount}");
      Console.WriteLine($"Backtracked: {hardestBacktrackings}");
      Console.WriteLine($"% occupied: {board.PercentageOccupied}");
      Console.WriteLine($"Max miliseconds: {stats.MaxMs}");
      Console.WriteLine($"Min miliseconds: {stats.MinMs}");
      Console.WriteLine($"Average miliseconds: {stats.AverageMs}");
      Console.WriteLine();
      Console.Write(board.Print());
      Console.Write(board.PrintWords(true));
    }

    #endregion

    #region Nested Types

    private class Stats
    {
      #region Properties

      public long MinMs => AllResults.Min();

      public long MaxMs => AllResults.Max();

      public long AverageMs => AllResults.Sum() / AllResults.Count;

      public List<long> AllResults
      {
        get;
      } = new List<long>();

      #endregion

      #region Other Stuff

      public void SetResult(long ms)
      {
        AllResults.Add(ms);
      }

      #endregion
    }

    #endregion
  }
}