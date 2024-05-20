using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using WordSearchGenerator.Common;
using WordSearchGenerator.Common.WoSeCon;
using WordSearchGenerator.Common.WoSeCon.Api;

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
      RunGridWithWords(1, 130, 19, 30);
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
      RunGridWithWords(NumberOfRepetitions, 14, 7, 12);
    }

    [TestMethod]
    public void BigListBigGrid()
    {
      RunGridWithWords(NumberOfRepetitions, 25, 12, 18);
    }

    [TestMethod]
    public void FillAllGridDiagonal2()
    {
      RunGridWithWords(1, -1, 3, 3, "words-small-diagonal.txt");
    }

    [TestMethod]
    public void FillAllGridDiagonal()
    {
      RunGridWithWords(1, -1, 2, 2, "words-tiny-diagonal.txt");
    }

    [TestMethod]
    public void FillAllGrid()
    {
      RunGridWithWords(1, -1, 2, 2, "words-tiny.txt");
    }

    [TestMethod]
    public void FillAllGrid2()
    {
      RunGridWithWords(1, -1, 3, 3, "words-small.txt");
    }

    [TestMethod]
    public void JanUlrich1()
    {
      RunGridWithWords(
        100, //NumberOfRepetitions * 10,
        -1,
        14,
        18,
        "e:\\Martin\\Syncthing\\Obec\\Kvízy\\memorial.txt",
        600);
    }

    [TestMethod]
    public void Svesedlice1()
    {
      RunGridWithWords(
        NumberOfRepetitions * 10,
        -1,
        13,
        19,
        "e:\\Martin\\Syncthing\\Obec\\Kvízy\\svesedlice.txt",
        2000);
    }

    private void RunGridWithWords(
      int numberOfRepetitions,
      int numberOfWords,
      int rows,
      int columns,
      string fileName = null,
      int limitWaitingMs = -1)
    {
      int iter = numberOfRepetitions;
      Stats stats = new Stats();
      Stopwatch st = new Stopwatch();
      string fullFilePath = Path.Combine(TestDataFolder, fileName ?? "words-big.txt");
      WordsLoader loader = new WordsLoader(File.ReadAllText(fullFilePath, Encoding.UTF8));
      List<WordInfo>? words = numberOfWords > 0 ? loader.Words.Take(numberOfWords).ToList() : loader.Words;
      int charCount = words.Select(wrd => wrd.Text.Length).Sum();
      List<WordInfo> hardestWords = null;
      int hardestBacktrackings = 0;
      long hardestTestedPositions = 0L;
      int hardestIntersections = 0;
      Board hardestBoard = null;

      while (--iter >= 0)
      {
        WoSeCon wo = new WoSeCon(words.CloneList(), rows, columns);

        st.Restart();
        CancellationTokenSource ts = new CancellationTokenSource();
        CancellationToken ct = ts.Token;

        bool finished = Task
          .Run(() => wo.Construct(ct), ct)
          .Wait(limitWaitingMs);
          
        if (!finished)
        {
          ts.Cancel(false);
          continue;
        }

        Board board = new Board(wo.Words, rows, columns, false);
        long elapsed = st.ElapsedMilliseconds;

        stats.SetResult(elapsed);

        if (board.IntersectionCount >= hardestIntersections)
        {
          hardestWords = wo.Words;
          hardestBoard = board;
          hardestBacktrackings = wo.Backtrackings;
          hardestTestedPositions = wo.TestesPositions;
          hardestIntersections = board.IntersectionCount;
        }

        Debug.WriteLine($"Left: {iter + 1}");
      }

      Console.WriteLine($"Total cell count: {rows * columns}");
      Console.WriteLine($"Words char count: {charCount}");
      Console.WriteLine($"Char cell count: {hardestBoard.CharCellCount}");
      Console.WriteLine($"Intersection count: {hardestBoard.IntersectionCount}");
      Console.WriteLine($"Positions tested: {hardestTestedPositions}");
      Console.WriteLine($"Backtracked: {hardestBacktrackings}");
      Console.WriteLine($"% occupied: {hardestBoard.PercentageOccupied}");
      Console.WriteLine($"Max miliseconds: {stats.MaxMs}");
      Console.WriteLine($"Min miliseconds: {stats.MinMs}");
      Console.WriteLine($"Average miliseconds: {stats.AverageMs}");
      Console.WriteLine();
      Console.Write(hardestBoard.Print(false, true));
      Console.Write(hardestBoard.PrintIntersections());
    }

    #endregion

    #region Nested Types

    private class Stats
    {
      #region Properties

      public long MinMs
      {
        get => AllResults.Min();
      }

      public long MaxMs
      {
        get => AllResults.Max();
      }

      public long AverageMs
      {
        get => AllResults.Sum() / AllResults.Count;
      }

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