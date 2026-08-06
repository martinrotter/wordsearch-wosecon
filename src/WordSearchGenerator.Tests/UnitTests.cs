using WordSearchGenerator.Common;
using WordSearchGenerator.Common.WoSeCon;
using WordSearchGenerator.Common.WoSeCon.Api;

namespace WordSearchGenerator.Tests
{
  [TestClass]
  public class UnitTests
  {
    #region Properties

    public TestContext TestContext
    {
      get;
      set;
    } = null!;

    #endregion

    #region Other Stuff

    [TestMethod]
    [DynamicData(nameof(FullGridCases))]
    public void ConstructsExpectedFullGrid(
      int size,
      string[] wordTexts,
      DirectedLocation[] expectedPlacements)
    {
      var generator = CreateGenerator(
        size,
        wordTexts,
        CreatePriorityOrderer(expectedPlacements));

      generator.Construct(null);

      WriteDiagnostics(generator, size, "Construction succeeded.");

      Assert.AreEqual(expectedPlacements.Length, generator.Words.Count);

      for (var i = 0; i < expectedPlacements.Length; i++)
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
      var generator = CreateGenerator(
        size,
        new[]
        {
          wordText
        },
        locations => locations);

      var exception = Assert.ThrowsExactly<Exception>(() => generator.Construct(null));

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

      var generator = CreateGenerator(
        3,
        new[]
        {
          "ABC", "DBE"
        },
        CreatePriorityOrderer(expectedPlacements));

      generator.Construct(null);

      var board = WriteDiagnostics(generator, 3, "Construction succeeded with a crossing.");

      Assert.AreEqual(expectedPlacements[0], generator.Words[0].Placement);
      Assert.AreEqual(expectedPlacements[1], generator.Words[1].Placement);
      Assert.AreEqual(1, board.IntersectionCount);
      Assert.AreEqual(5, board.CharCellCount);
      Assert.AreEqual(100.0 * 5 / 9, board.PercentageOccupied, 0.001);
    }

    [TestMethod]
    public void BacktracksWhenFirstPlacementBlocksRemainingWords()
    {
      var blockedPlacement =
        Location(0, 0, DirectedLocation.LocationDirection.LeftTopRightBottom);

      DirectedLocation[] expectedPlacements =
      {
        Location(0, 0, DirectedLocation.LocationDirection.LeftToRight),
        Location(1, 0, DirectedLocation.LocationDirection.LeftToRight),
        Location(2, 0, DirectedLocation.LocationDirection.LeftToRight)
      };

      DirectedLocation[] locationPriorities =
      {
        blockedPlacement, expectedPlacements[0], expectedPlacements[1], expectedPlacements[2]
      };

      var generator = CreateGenerator(
        3,
        new[]
        {
          "ABC", "DEF", "GHI"
        },
        CreatePriorityOrderer(locationPriorities));

      generator.Construct(null);

      WriteDiagnostics(generator, 3, "Construction succeeded after forced backtracking.");

      Assert.AreEqual(1, generator.Backtrackings);

      for (var i = 0; i < expectedPlacements.Length; i++)
      {
        Assert.AreEqual(expectedPlacements[i], generator.Words[i].Placement);
      }

      AssertFillsGrid(generator, 3);
    }

    [TestMethod]
    public void ConstructCanBeCalledRepeatedlyOnTheSameGenerator()
    {
      var blockedPlacement =
        Location(0, 0, DirectedLocation.LocationDirection.LeftTopRightBottom);

      DirectedLocation[] expectedPlacements =
      {
        Location(0, 0, DirectedLocation.LocationDirection.LeftToRight),
        Location(1, 0, DirectedLocation.LocationDirection.LeftToRight),
        Location(2, 0, DirectedLocation.LocationDirection.LeftToRight)
      };

      var generator = CreateGenerator(
        3,
        new[]
        {
          "ABC", "DEF", "GHI"
        },
        CreatePriorityOrderer(new[]
        {
          blockedPlacement,
          expectedPlacements[0],
          expectedPlacements[1],
          expectedPlacements[2]
        }));

      generator.Construct(null);

      var firstTestedPositions = generator.TestedPositions;
      Assert.AreEqual(1, generator.Backtrackings);

      generator.Construct(null);

      WriteDiagnostics(
        generator,
        3,
        "Repeated construction succeeded from a clean search state.");

      Assert.AreEqual(1, generator.Backtrackings);
      Assert.AreEqual(firstTestedPositions, generator.TestedPositions);

      for (var i = 0; i < expectedPlacements.Length; i++)
      {
        Assert.AreEqual(expectedPlacements[i], generator.Words[i].Placement);
      }

      AssertFillsGrid(generator, 3);
    }

    [TestMethod]
    public void ConstructsDenseElevenByElevenPuzzleWithBacktracking()
    {
      string[] matrix =
      {
        "AAAAAAAAAAA", "BBBBBBBBBBB", "CDEFGHIJKLM", "NOPQRSTUVWX", "YZ012345678", "9ABCDEFGHIJ",
        "KLMNOPQRSTU", "VWXYZ012345", "6789ABCDEFG", "HIJKLMNOPQR", "STUVWXYZ012"
      };

      List<(DirectedLocation Placement, int Length)> specifications = [];

      for (var row = 0; row < 11; row++)
      {
        specifications.Add((
          Location(row, 0, DirectedLocation.LocationDirection.LeftToRight),
          11));
      }

      for (var column = 0; column < 11; column++)
      {
        specifications.Add((
          Location(0, column, DirectedLocation.LocationDirection.TopBottom),
          11));
      }

      specifications.AddRange(new[]
      {
        (Location(0, 1, DirectedLocation.LocationDirection.LeftTopRightBottom), 10),
        (Location(1, 0, DirectedLocation.LocationDirection.LeftTopRightBottom), 10),
        (Location(0, 9, DirectedLocation.LocationDirection.RightTopLeftBottom), 10),
        (Location(1, 10, DirectedLocation.LocationDirection.RightTopLeftBottom), 10),
        (Location(0, 2, DirectedLocation.LocationDirection.LeftTopRightBottom), 9),
        (Location(2, 0, DirectedLocation.LocationDirection.LeftTopRightBottom), 9),
        (Location(0, 8, DirectedLocation.LocationDirection.RightTopLeftBottom), 9),
        (Location(2, 10, DirectedLocation.LocationDirection.RightTopLeftBottom), 9)
      });

      var wordTexts = specifications
        .Select(specification => ReadWord(
          matrix,
          specification.Placement,
          specification.Length))
        .ToArray();

      var expectedPlacements = specifications
        .Select(specification => specification.Placement)
        .ToArray();

      var blockedPlacement =
        Location(0, 0, DirectedLocation.LocationDirection.LeftTopRightBottom);

      var locationPriorities = new[]
        {
          blockedPlacement
        }
        .Concat(expectedPlacements)
        .ToArray();

      var generator = CreateGenerator(
        11,
        wordTexts,
        CreatePriorityOrderer(locationPriorities));

      generator.Construct(null);

      var board = WriteDiagnostics(
        generator,
        11,
        "Dense construction succeeded after forced backtracking.");

      Assert.AreEqual(30, generator.Words.Count);
      Assert.AreEqual(1, generator.Backtrackings);
      Assert.AreEqual(121, board.CharCellCount);
      Assert.AreEqual(121, board.IntersectionCount);
      Assert.AreEqual(100.0, board.PercentageOccupied, 0.001);

      for (var i = 0; i < expectedPlacements.Length; i++)
      {
        Assert.AreEqual(expectedPlacements[i], generator.Words[i].Placement);
      }
    }

    [TestMethod]
    public void ConstructsExpectedFullQuizModeGrid()
    {
      string[] wordTexts =
      {
        "ABC", "DEF", "GHI", "JKL"
      };
      DirectedLocation[] expectedPlacements =
      {
        Location(0, 0, DirectedLocation.LocationDirection.LeftToRight),
        Location(1, 0, DirectedLocation.LocationDirection.LeftToRight),
        Location(2, 0, DirectedLocation.LocationDirection.LeftToRight),
        Location(3, 0, DirectedLocation.LocationDirection.LeftToRight)
      };

      var generator = CreateGenerator(
        4,
        wordTexts,
        CreatePriorityOrderer(expectedPlacements),
        true);

      generator.Construct(null);

      var board = WriteDiagnostics(
        generator,
        4,
        "Quiz-mode construction succeeded.",
        true);

      Assert.AreEqual(16, board.CharCellCount);
      Assert.AreEqual(100.0, board.PercentageOccupied, 0.001);

      for (var row = 0; row < wordTexts.Length; row++)
      {
        Assert.AreEqual(expectedPlacements[row], generator.Words[row].Placement);
        Assert.AreEqual(
          $"{Constants.Misc.QuizModePlaceholder}{wordTexts[row]}",
          generator.Words[row].Text);
        Assert.AreEqual(
          Board.Cell.CellType.QuizWordPlaceholder,
          board.Matrix[row, 0].Type);
        Assert.AreEqual(row + 1, board.Matrix[row, 0].QuizWordNumber);
        Assert.AreEqual(
          DirectedLocation.LocationDirection.LeftToRight,
          board.Matrix[row, 0].QuizWordDirection);

        for (var column = 1; column < 4; column++)
        {
          Assert.AreEqual(wordTexts[row][column - 1], board.Matrix[row, column].Char);
        }
      }
    }

    [TestMethod]
    public void RejectsQuizPlaceholderIntersection()
    {
      var generator = CreateGenerator(
        3,
        new[]
        {
          "AB", "CD"
        },
        locations => locations,
        true);

      Assert.IsTrue(generator.IsValidPlacement(
        generator.Words[0],
        Location(0, 0, DirectedLocation.LocationDirection.LeftToRight)));

      Assert.IsFalse(generator.IsValidPlacement(
        generator.Words[1],
        Location(0, 0, DirectedLocation.LocationDirection.TopBottom)));

      var board = WriteDiagnostics(
        generator,
        3,
        "Quiz placeholder intersection correctly rejected.",
        true);

      Assert.IsNull(generator.Words[1].Placement);
      Assert.AreEqual(3, board.CharCellCount);
      Assert.AreEqual(0, board.IntersectionCount);
    }

    public static IEnumerable<object[]> FullGridCases()
    {
      yield return new object[]
      {
        2, new[]
        {
          "AB", "CD"
        },
        new[]
        {
          Location(0, 0, DirectedLocation.LocationDirection.LeftTopRightBottom),
          Location(0, 1, DirectedLocation.LocationDirection.RightTopLeftBottom)
        }
      };

      yield return new object[]
      {
        3, new[]
        {
          "ABC", "DE", "FG", "H", "I"
        },
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
        4, new[]
        {
          "ABCD", "EFG", "HIJ", "KL", "MN", "O", "P"
        },
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
      RandomLocator.LocationOrderer orderer,
      bool quizMode = false)
    {
      var words = wordTexts
        .Select((text, index) => new WordInfo
        {
          Text = text,
          PrintableText = text,
          WordNumber = index + 1
        })
        .ToList();

      return new WoSeCon(words, size, size, quizMode, orderer);
    }

    private static RandomLocator.LocationOrderer CreatePriorityOrderer(
      IReadOnlyList<DirectedLocation> priorityLocations)
    {
      var priorities = priorityLocations
        .Select((location, index) => new
        {
          location,
          index
        })
        .ToDictionary(item => item.location, item => item.index);

      return locations => locations
        .Select((location, index) => new
        {
          location,
          index
        })
        .OrderBy(item => priorities.TryGetValue(item.location, out var priority)
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

    private static string ReadWord(
      IReadOnlyList<string> matrix,
      DirectedLocation placement,
      int length)
    {
      var (rowStep, columnStep) = GetSteps(placement.Direction);

      return new string(Enumerable
        .Range(0, length)
        .Select(index => matrix
            [placement.Row + rowStep * index]
          [placement.Column + columnStep * index])
        .ToArray());
    }

    private Board WriteDiagnostics(
      WoSeCon generator,
      int size,
      string outcome,
      bool quizMode = false)
    {
      var board = new Board(
        generator.Words,
        size,
        size,
        quizMode);

      TestContext.WriteLine(outcome);
      TestContext.WriteLine(board.PrintDiagnostics());
      TestContext.WriteLine($"Backtrackings: {generator.Backtrackings}");
      TestContext.WriteLine($"Tested positions: {generator.TestedPositions}");

      return board;
    }

    private static void AssertFillsGrid(WoSeCon generator, int size)
    {
      Dictionary<(int Row, int Column), char> occupiedCells = new();

      foreach (var word in generator.Words)
      {
        Assert.IsNotNull(word.Placement);

        var (rowStep, columnStep) = GetSteps(word.Placement.Direction);

        for (var index = 0; index < word.Text.Length; index++)
        {
          var row = word.Placement.Row + rowStep * index;
          var column = word.Placement.Column + columnStep * index;

          Assert.IsTrue(row >= 0 && row < size);
          Assert.IsTrue(column >= 0 && column < size);

          if (occupiedCells.TryGetValue((row, column), out var existingCharacter))
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

    #endregion
  }
}
