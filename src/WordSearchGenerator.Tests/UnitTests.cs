using WordSearchGenerator.Common;
using WordSearchGenerator.Common.WoSeCon;
using WordSearchGenerator.Common.WoSeCon.Api;

namespace WordSearchGenerator.Tests
{
  [TestClass]
  public class UnitTests
  {
    public TestContext TestContext
    {
      get;
      set;
    } = null!;

    [TestMethod]
    [DynamicData(nameof(FullGridCases))]
    public void ConstructsExpectedFullGrid(
      int size,
      string[] wordTexts,
      DirectedLocation[] expectedPlacements)
    {
      WoSeCon generator = CreateGenerator(
        size,
        wordTexts,
        CreatePriorityOrderer(expectedPlacements));

      generator.Construct(null);

      WriteDiagnostics(generator, size, "Construction succeeded.");

      Assert.AreEqual(expectedPlacements.Length, generator.Words.Count);

      for (int i = 0; i < expectedPlacements.Length; i++)
      {
        Assert.AreEqual(expectedPlacements[i], generator.Words[i].Placement);
      }

      AssertFillsGrid(generator, size);
    }

    [TestMethod]
    [DataRow(2, "ABC")]
    [DataRow(3, "ABCD")]
    [DataRow(4, "ABCDE")]
    public void RejectsWordLongerThanMatrix(int size, string wordText)
    {
      WoSeCon generator = CreateGenerator(
        size,
        new[] {wordText},
        locations => locations);

      Exception exception = Assert.ThrowsExactly<Exception>(() => generator.Construct(null));

      WriteDiagnostics(generator, size, $"Expected failure: {exception.Message}");
    }

    [TestMethod]
    public void ConstructsExpectedCrossing()
    {
      DirectedLocation[] expectedPlacements =
      {
        Location(1, 0, DirectedLocation.LocationDirection.LeftToRight),
        Location(0, 1, DirectedLocation.LocationDirection.TopBottom)
      };

      WoSeCon generator = CreateGenerator(
        3,
        new[] {"ABC", "DBE"},
        CreatePriorityOrderer(expectedPlacements));

      generator.Construct(null);

      Board board = WriteDiagnostics(generator, 3, "Construction succeeded with a crossing.");

      Assert.AreEqual(expectedPlacements[0], generator.Words[0].Placement);
      Assert.AreEqual(expectedPlacements[1], generator.Words[1].Placement);
      Assert.AreEqual(1, board.IntersectionCount);
      Assert.AreEqual(5, board.CharCellCount);
      Assert.AreEqual(100.0 * 5 / 9, board.PercentageOccupied, 0.001);
    }

    public static IEnumerable<object[]> FullGridCases()
    {
      yield return new object[]
      {
        2,
        new[] {"AB", "CD"},
        new[]
        {
          Location(0, 0, DirectedLocation.LocationDirection.LeftTopRightBottom),
          Location(0, 1, DirectedLocation.LocationDirection.RightTopLeftBottom)
        }
      };

      yield return new object[]
      {
        3,
        new[] {"ABC", "DE", "FG", "H", "I"},
        new[]
        {
          Location(0, 0, DirectedLocation.LocationDirection.LeftTopRightBottom),
          Location(0, 1, DirectedLocation.LocationDirection.LeftTopRightBottom),
          Location(1, 0, DirectedLocation.LocationDirection.LeftTopRightBottom),
          Location(0, 2, DirectedLocation.LocationDirection.RightToLeft),
          Location(2, 0, DirectedLocation.LocationDirection.LeftToRight)
        }
      };

      yield return new object[]
      {
        4,
        new[] {"ABCD", "EFG", "HIJ", "KL", "MN", "O", "P"},
        new[]
        {
          Location(0, 0, DirectedLocation.LocationDirection.LeftTopRightBottom),
          Location(0, 1, DirectedLocation.LocationDirection.LeftTopRightBottom),
          Location(1, 0, DirectedLocation.LocationDirection.LeftTopRightBottom),
          Location(0, 2, DirectedLocation.LocationDirection.LeftTopRightBottom),
          Location(2, 0, DirectedLocation.LocationDirection.LeftTopRightBottom),
          Location(0, 3, DirectedLocation.LocationDirection.RightToLeft),
          Location(3, 0, DirectedLocation.LocationDirection.LeftToRight)
        }
      };
    }

    private static WoSeCon CreateGenerator(
      int size,
      IEnumerable<string> wordTexts,
      RandomLocator.LocationOrderer orderer)
    {
      List<WordInfo> words = wordTexts
        .Select((text, index) => new WordInfo
        {
          Text = text,
          PrintableText = text,
          WordNumber = index + 1
        })
        .ToList();

      return new WoSeCon(words, size, size, false, orderer);
    }

    private static RandomLocator.LocationOrderer CreatePriorityOrderer(
      IReadOnlyList<DirectedLocation> priorityLocations)
    {
      Dictionary<DirectedLocation, int> priorities = priorityLocations
        .Select((location, index) => new {location, index})
        .ToDictionary(item => item.location, item => item.index);

      return locations => locations
        .Select((location, index) => new {location, index})
        .OrderBy(item => priorities.TryGetValue(item.location, out int priority)
          ? priority
          : int.MaxValue)
        .ThenBy(item => item.index)
        .Select(item => item.location)
        .ToList();
    }

    private static DirectedLocation Location(
      int row,
      int column,
      DirectedLocation.LocationDirection direction)
    {
      return new DirectedLocation
      {
        Row = row,
        Column = column,
        Direction = direction
      };
    }

    private Board WriteDiagnostics(WoSeCon generator, int size, string outcome)
    {
      Board board = new Board(
        generator.Words,
        size,
        size,
        false);

      TestContext.WriteLine(outcome);
      TestContext.WriteLine(board.PrintDiagnostics());
      TestContext.WriteLine($"Backtrackings: {generator.Backtrackings}");
      TestContext.WriteLine($"Tested positions: {generator.TestesPositions}");

      return board;
    }

    private static void AssertFillsGrid(WoSeCon generator, int size)
    {
      Dictionary<(int Row, int Column), char> occupiedCells = new();

      foreach (WordInfo word in generator.Words)
      {
        Assert.IsNotNull(word.Placement);

        (int rowStep, int columnStep) = GetSteps(word.Placement.Direction);

        for (int index = 0; index < word.Text.Length; index++)
        {
          int row = word.Placement.Row + rowStep * index;
          int column = word.Placement.Column + columnStep * index;

          Assert.IsTrue(row >= 0 && row < size);
          Assert.IsTrue(column >= 0 && column < size);

          if (occupiedCells.TryGetValue((row, column), out char existingCharacter))
          {
            Assert.AreEqual(existingCharacter, word.Text[index]);
          }
          else
          {
            occupiedCells[(row, column)] = word.Text[index];
          }
        }
      }

      Assert.AreEqual(size * size, occupiedCells.Count);
    }

    private static (int Row, int Column) GetSteps(
      DirectedLocation.LocationDirection direction)
    {
      return direction switch
      {
        DirectedLocation.LocationDirection.LeftToRight => (0, 1),
        DirectedLocation.LocationDirection.RightToLeft => (0, -1),
        DirectedLocation.LocationDirection.TopBottom => (1, 0),
        DirectedLocation.LocationDirection.BottomTop => (-1, 0),
        DirectedLocation.LocationDirection.LeftTopRightBottom => (1, 1),
        DirectedLocation.LocationDirection.RightBottomLeftTop => (-1, -1),
        DirectedLocation.LocationDirection.LeftBottomRightTop => (-1, 1),
        DirectedLocation.LocationDirection.RightTopLeftBottom => (1, -1),
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
      };
    }
  }
}
