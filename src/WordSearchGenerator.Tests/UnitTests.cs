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
      RunGridWithWords(NumberOfRepetitions, 25, 11, 12);
    }

    private void RunGridWithWords(int numberOfRepetitions, int numberOfWords, int rows, int columns)
    {
      var iter = numberOfRepetitions;
      var stats = new Stats();
      var st = new Stopwatch();

      var words = new WordsLoader(@$"{TestDataFolder}\words-big.txt").Words.Take(numberOfWords).ToList();
      var charCount = words.Select(wrd => wrd.Text.Length).Sum();
      List<WordInfo> hardestWords = null;
      var hardestBacktrackings = 0;

      while (iter-- >= 0)
      {
        var wo = new WoSeCon(words.CloneList(), rows, columns);

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

      Console.WriteLine(board.Print());

      Console.WriteLine($"Total cell count: {rows * columns}");
      Console.WriteLine($"Words char count: {charCount}");
      Console.WriteLine($"Char cell count: {board.CharCellCount}");
      Console.WriteLine($"Backtracked: {hardestBacktrackings}");
      Console.WriteLine($"% occupied: {board.PercentageOccupied}");
      Console.WriteLine($"Max miliseconds: {stats.MaxMs}");
      Console.WriteLine($"Min miliseconds: {stats.MinMs}");
      Console.WriteLine($"Average miliseconds: {stats.AverageMs}");
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