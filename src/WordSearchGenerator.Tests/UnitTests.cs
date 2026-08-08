using WordSearchGenerator.Common;
using WordSearchGenerator.Common.WoSeCon;
using WordSearchGenerator.Common.WoSeCon.Api;

namespace WordSearchGenerator.Tests
{
  [TestClass]
  public class UnitTests
  {
    #region Static Fields

    private const int MemorialMonteCarloThreadCount = 16;

    #endregion

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

      generator.Construct();

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

      var exception = Assert.ThrowsExactly<Exception>(() => generator.Construct());

      WriteDiagnostics(generator, size, $"Expected failure: {exception.Message}");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void RejectsOneLetterWords(bool quizMode)
    {
      var exception = Assert.ThrowsExactly<ArgumentException>(() => CreateGenerator(
        1,
        new[]
        {
          "A"
        },
        locations => locations,
        quizMode));

      StringAssert.Contains(exception.Message, "at least two characters");
    }

    [TestMethod]
    public void EstimateDifficultyRecognizesEasyInput()
    {
      var estimate = WoSeCon.EstimateDifficulty(
        CreateWords(new[]
        {
          "ABC", "DEF", "GHI"
        }),
        10,
        10,
        false);

      Assert.AreEqual(WoSeCon.EstimatedConstructionTime.FastInSeconds, estimate);
    }

    [TestMethod]
    public void EstimateDifficultyRecognizesStructuralImpossibility()
    {
      var wordTooLong = WoSeCon.EstimateDifficulty(
        CreateWords(new[]
        {
          "ABCDEF"
        }),
        5,
        5,
        false);
      var insufficientCompatibleCrossings = WoSeCon.EstimateDifficulty(
        CreateWords(new[]
        {
          "ABC", "DEF", "GHI", "JKL"
        }),
        3,
        3,
        false);

      Assert.AreEqual(
        WoSeCon.EstimatedConstructionTime.LikelyImpossible,
        wordTooLong);
      Assert.AreEqual(
        WoSeCon.EstimatedConstructionTime.LikelyImpossible,
        insufficientCompatibleCrossings);
    }

    [TestMethod]
    public void EstimateDifficultyAccountsForParallelAttempts()
    {
      var words = CreateWords(Enumerable
        .Range(0, 12)
        .Select(_ => "ABCDEFGH"));

      var sequential = WoSeCon.EstimateDifficulty(
        words,
        10,
        10,
        false);
      var parallel = WoSeCon.EstimateDifficulty(
        words,
        10,
        10,
        false,
        16);

      Assert.IsTrue(parallel <= sequential);
    }

    [TestMethod]
    public void EstimateDifficultyAccountsForQuizQuestionCells()
    {
      var words = CreateWords(Enumerable
        .Range(0, 8)
        .Select(_ => "ABCDE"));

      var normal = WoSeCon.EstimateDifficulty(words, 7, 7, false);
      var quiz = WoSeCon.EstimateDifficulty(words, 7, 7, true);

      Assert.IsTrue(quiz >= normal);
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

      generator.Construct();

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

      generator.Construct();

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

      var priorityOrderer = CreatePriorityOrderer(new[]
      {
        blockedPlacement, expectedPlacements[0], expectedPlacements[1], expectedPlacements[2]
      });
      var ordererCalls = 0;
      Locator.LocationOrderer countingOrderer = locations =>
      {
        ordererCalls++;
        return priorityOrderer(locations);
      };

      var generator = CreateGenerator(
        3,
        new[]
        {
          "ABC", "DEF", "GHI"
        },
        countingOrderer);

      Assert.AreEqual(1, ordererCalls);

      generator.Construct();

      var firstTestedPositions = generator.TestedPositions;
      Assert.AreEqual(1, generator.Backtrackings);
      Assert.AreEqual(1, ordererCalls);

      generator.Construct();

      WriteDiagnostics(
        generator,
        3,
        "Repeated construction succeeded from a clean search state.");

      Assert.AreEqual(1, generator.Backtrackings);
      Assert.AreEqual(firstTestedPositions, generator.TestedPositions);
      Assert.AreEqual(2, ordererCalls);

      for (var i = 0; i < expectedPlacements.Length; i++)
      {
        Assert.AreEqual(expectedPlacements[i], generator.Words[i].Placement);
      }

      AssertFillsGrid(generator, 3);
    }

    [TestMethod]
    public void ConstructReportsInitialAndCompletedProgress()
    {
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
        CreatePriorityOrderer(expectedPlacements));
      List<ConstructionProgress> updates = [];

      generator.Construct(new InlineProgress<ConstructionProgress>(updates.Add));

      Assert.IsTrue(updates.Count >= 2);

      var initial = updates[0];
      var completed = updates[^1];

      Assert.AreEqual(0, initial.PlacedWordCount);
      Assert.AreEqual(0, initial.FurthestPlacedWordCount);
      Assert.AreEqual(3, initial.TotalWordCount);
      Assert.AreEqual(3, completed.PlacedWordCount);
      Assert.AreEqual(3, completed.FurthestPlacedWordCount);
      Assert.AreEqual(3, completed.TotalWordCount);
      Assert.AreEqual(generator.TestedPositions, completed.TestedPositions);
      Assert.AreEqual(generator.Backtrackings, completed.Backtrackings);
      Assert.IsTrue(completed.Elapsed >= initial.Elapsed);
    }

    [TestMethod]
    public void ConstructCancellationResetsStateAndAllowsRetry()
    {
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
        CreatePriorityOrderer(expectedPlacements));
      using var cancellation = new CancellationTokenSource();
      var progress = new InlineProgress<ConstructionProgress>(_ => cancellation.Cancel());

      Assert.ThrowsExactly<OperationCanceledException>(() => generator.Construct(
        progress,
        cancellation.Token));

      Assert.IsTrue(generator.Words.All(word => word.Placement == null));
      Assert.AreEqual(0L, generator.TestedPositions);
      Assert.AreEqual(0, generator.Backtrackings);
      Assert.AreEqual(WoSeCon.OperationMode.Forward, generator.Mode);

      generator.Construct();

      Assert.IsTrue(generator.Words.All(word => word.Placement != null));
      AssertFillsGrid(generator, 3);
    }

    [TestMethod]
    public async Task ConstructRejectsConcurrentCallsOnSameInstance()
    {
      var generator = CreateGenerator(
        3,
        new[]
        {
          "ABC", "DEF", "GHI"
        },
        locations => locations);
      using var progressEntered = new ManualResetEventSlim();
      using var releaseProgress = new ManualResetEventSlim();
      var progressReportCount = 0;
      var progress = new InlineProgress<ConstructionProgress>(_ =>
      {
        if (Interlocked.Increment(ref progressReportCount) == 1)
        {
          progressEntered.Set();
          releaseProgress.Wait();
        }
      });
      var firstConstruction = Task.Run(() => generator.Construct(progress));

      try
      {
        Assert.IsTrue(progressEntered.Wait(TimeSpan.FromSeconds(5)));

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => generator.Construct());

        StringAssert.Contains(exception.Message, "already running");
      }
      finally
      {
        releaseProgress.Set();
        await firstConstruction;
      }
    }

    [TestMethod]
    public void GeneratorsOwnIndependentWordCopies()
    {
      var sourceWords = new List<WordInfo>
      {
        new WordInfo
        {
          Text = "ABC",
          WordNumber = 1
        },
        new WordInfo
        {
          Text = "DEF",
          WordNumber = 2
        },
        new WordInfo
        {
          Text = "GHI",
          WordNumber = 3
        }
      };
      DirectedLocation[] expectedPlacements =
      {
        Location(0, 0, DirectedLocation.LocationDirection.LeftToRight),
        Location(1, 0, DirectedLocation.LocationDirection.LeftToRight),
        Location(2, 0, DirectedLocation.LocationDirection.LeftToRight)
      };
      var orderer = CreatePriorityOrderer(expectedPlacements);
      var firstGenerator = new WoSeCon(sourceWords, 3, 3, false, orderer);

      firstGenerator.Construct();

      var firstPlacements = firstGenerator.Words
        .Select(word => (DirectedLocation)word.Placement.Clone())
        .ToArray();
      var secondGenerator = new WoSeCon(sourceWords, 3, 3, false, orderer);

      for (var i = 0; i < sourceWords.Count; i++)
      {
        Assert.AreNotSame(sourceWords[i], firstGenerator.Words[i]);
        Assert.AreNotSame(sourceWords[i], secondGenerator.Words[i]);
        Assert.IsNull(sourceWords[i].Placement);
        Assert.AreEqual(firstPlacements[i], firstGenerator.Words[i].Placement);
      }

      secondGenerator.Construct();

      for (var i = 0; i < sourceWords.Count; i++)
      {
        Assert.AreEqual(firstPlacements[i], firstGenerator.Words[i].Placement);
        Assert.AreEqual(expectedPlacements[i], secondGenerator.Words[i].Placement);
      }
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

      generator.Construct();

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
    [TestCategory("LongRunning")]
    public async Task ConstructsMemorialWords()
    {
      var wordTexts = File
        .ReadAllLines(Path.Combine(
          AppContext.BaseDirectory,
          "test-data",
          "memorial.txt"))
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .ToArray();
      var estimate = WoSeCon.EstimateDifficulty(
        CreateWords(wordTexts),
        15,
        17,
        false,
        MemorialMonteCarloThreadCount);
      using var cancellation = new CancellationTokenSource();
      var seeds = Enumerable
        .Range(0, MemorialMonteCarloThreadCount)
        .Select(_ => Random.Shared.Next())
        .ToArray();
      var pendingAttempts = seeds
        .Select(seed => Task.Factory.StartNew(
          () => TryConstructMemorialWords(wordTexts, seed, cancellation.Token),
          CancellationToken.None,
          TaskCreationOptions.LongRunning,
          TaskScheduler.Default))
        .ToList();
      MemorialConstructionAttempt? winner = null;
      List<Exception> errors = [];

      TestContext.WriteLine(
        $"Starting {MemorialMonteCarloThreadCount} Monte Carlo construction threads.");
      TestContext.WriteLine($"Estimated construction time: {estimate}");
      TestContext.WriteLine($"Seeds: {string.Join(", ", seeds)}");

      try
      {
        while (pendingAttempts.Count > 0 && winner == null)
        {
          var completedTask = await Task.WhenAny(pendingAttempts);
          pendingAttempts.Remove(completedTask);

          var result = await completedTask;

          if (result.Generator != null)
          {
            winner = result;
          }
          else if (result.Error != null)
          {
            errors.Add(result.Error);
          }
        }
      }
      finally
      {
        cancellation.Cancel();
        await Task.WhenAll(pendingAttempts);
      }

      if (winner?.Generator == null)
      {
        Assert.Fail(
          $"None of the {MemorialMonteCarloThreadCount} construction attempts succeeded. " +
          $"Errors: {string.Join(" | ", errors.Select(error => error.Message))}");
      }

      var generator = winner.Generator;

      var board = WriteDiagnostics(
        generator,
        15,
        17,
        $"Memorial word construction succeeded with seed {winner.Seed}.");

      Assert.AreEqual(40, generator.Words.Count);
      Assert.AreNotEqual(WoSeCon.EstimatedConstructionTime.LikelyImpossible, estimate);
      Assert.IsTrue(generator.Words.All(word => word.Placement != null));
      Assert.AreEqual(15, board.RowCount);
      Assert.AreEqual(17, board.ColumnCount);
      CollectionAssert.AreEquivalent(
        wordTexts,
        generator.Words.Select(word => word.Text).ToArray());
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

      generator.Construct();

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
        Assert.AreEqual(wordTexts[row], generator.Words[row].Text);
        Assert.AreEqual(
          Board.Cell.CellType.QuizQuestion,
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
    public void QuizMessageUsesDistinctAnswerCellsIncludingSpaces()
    {
      var words = new List<WordInfo>
      {
        new WordInfo
        {
          Placement = Location(
            0,
            0,
            DirectedLocation.LocationDirection.LeftToRight),
          QuizQuestion = "Example question",
          Text = "A A",
          WordNumber = 1
        }
      };

      var board = new Board(words, 2, 4, true, "A A");

      Assert.AreEqual(Board.Cell.CellType.QuizQuestion, board.Matrix[0, 0].Type);

      for (var column = 1; column < 4; column++)
      {
        var cell = board.Matrix[0, column];

        Assert.AreEqual(Board.Cell.CellType.CharFromText, cell.Type);
        Assert.AreEqual(column, cell.MessageIndex);
        Assert.AreEqual(words[0].Text[column - 1], cell.Char);
      }

      Assert.AreEqual(' ', board.Matrix[0, 2].Char);
      Assert.IsTrue(board.Matrix
        .OfType<Board.Cell>()
        .Where(cell => cell.MessageIndex != null)
        .All(cell => cell.Type == Board.Cell.CellType.CharFromText));
      Assert.IsTrue(board.Matrix
        .OfType<Board.Cell>()
        .Skip(4)
        .All(cell => cell.Type == Board.Cell.CellType.Empty));
    }

    [TestMethod]
    public void QuizMessageIsSpreadAcrossMatchingAnswerCells()
    {
      var words = new List<WordInfo>
      {
        new WordInfo
        {
          Placement = Location(
            0,
            0,
            DirectedLocation.LocationDirection.LeftToRight),
          QuizQuestion = "First question",
          Text = "AAA",
          WordNumber = 1
        },
        new WordInfo
        {
          Placement = Location(
            3,
            0,
            DirectedLocation.LocationDirection.LeftToRight),
          QuizQuestion = "Second question",
          Text = "AAA",
          WordNumber = 2
        }
      };

      var board = new Board(words, 4, 4, true, "AA");

      Assert.AreEqual(1, board.Matrix[0, 1].MessageIndex);
      Assert.AreEqual(2, board.Matrix[3, 3].MessageIndex);
      Assert.AreEqual(
        2,
        board.Matrix
          .OfType<Board.Cell>()
          .Count(cell => cell.MessageIndex != null));
    }

    [TestMethod]
    public void QuizMessageRejectsReusingTheSameAnswerCell()
    {
      var words = new List<WordInfo>
      {
        new WordInfo
        {
          Placement = Location(
            0,
            0,
            DirectedLocation.LocationDirection.LeftToRight),
          QuizQuestion = "Example question",
          Text = "AB",
          WordNumber = 1
        }
      };

      Assert.ThrowsExactly<MessageCannotBePlacedException>(() =>
        new Board(words, 1, 3, true, "AA"));
    }

    [TestMethod]
    public void NormalMessageStillUsesOtherwiseEmptyCells()
    {
      var words = new List<WordInfo>
      {
        new WordInfo
        {
          Placement = Location(
            0,
            0,
            DirectedLocation.LocationDirection.LeftToRight),
          Text = "AB",
          WordNumber = 1
        }
      };

      var board = new Board(words, 2, 2, false, " C");

      Assert.AreEqual(Board.Cell.CellType.CharFromMessage, board.Matrix[1, 0].Type);
      Assert.AreEqual(' ', board.Matrix[1, 0].Char);
      Assert.IsNull(board.Matrix[1, 0].MessageIndex);
      Assert.AreEqual(Board.Cell.CellType.CharFromMessage, board.Matrix[1, 1].Type);
      Assert.AreEqual('C', board.Matrix[1, 1].Char);
      Assert.IsNull(board.Matrix[1, 1].MessageIndex);
    }

    [TestMethod]
    public void NormalMessageIsSpreadUniformlyAcrossEmptyCells()
    {
      var words = new List<WordInfo>
      {
        new WordInfo
        {
          Placement = Location(
            1,
            1,
            DirectedLocation.LocationDirection.LeftToRight),
          Text = "AB",
          WordNumber = 1
        }
      };

      var board = new Board(words, 3, 4, false, "CDEF");
      (int Row, int Column, char Character)[] expectedMessageCells =
      {
        (0, 0, 'C'),
        (0, 3, 'D'),
        (2, 0, 'E'),
        (2, 3, 'F')
      };

      foreach (var expected in expectedMessageCells)
      {
        var cell = board.Matrix[expected.Row, expected.Column];

        Assert.AreEqual(Board.Cell.CellType.CharFromMessage, cell.Type);
        Assert.AreEqual(expected.Character, cell.Char);
      }

      Assert.AreEqual(
        expectedMessageCells.Length,
        board.Matrix
          .OfType<Board.Cell>()
          .Count(cell => cell.Type == Board.Cell.CellType.CharFromMessage));
    }

    [TestMethod]
    public void QuizMessageConstraintBacktracksFromMergedMessageLetters()
    {
      DirectedLocation[] crossingPlacements =
      {
        Location(1, 0, DirectedLocation.LocationDirection.LeftToRight),
        Location(0, 1, DirectedLocation.LocationDirection.TopBottom)
      };
      var generator = CreateGenerator(
        3,
        new[]
        {
          "AA", "AA"
        },
        CreatePriorityOrderer(crossingPlacements),
        true);
      var rejectedLayoutCount = 0;

      generator.Construct(
        completionValidator: words =>
        {
          try
          {
            _ = new Board(words.ToList(), 3, 3, true, "AAAA");
            return true;
          }
          catch (MessageCannotBePlacedException)
          {
            rejectedLayoutCount++;
            return false;
          }
        });

      var board = new Board(generator.Words, 3, 3, true, "AAAA");

      Assert.IsTrue(rejectedLayoutCount >= 1);
      Assert.IsTrue(generator.Backtrackings >= 1);
      Assert.AreEqual(
        4,
        board.Matrix
          .OfType<Board.Cell>()
          .Count(cell => cell.MessageIndex != null));
    }

    [TestMethod]
    public void QuizQuestionCellOffsetsAnswerInEveryDirection()
    {
      (DirectedLocation.LocationDirection Direction, int Row, int Column)[] cases =
      {
        (DirectedLocation.LocationDirection.LeftToRight, 1, 0),
        (DirectedLocation.LocationDirection.RightToLeft, 1, 2),
        (DirectedLocation.LocationDirection.TopBottom, 0, 1),
        (DirectedLocation.LocationDirection.BottomTop, 2, 1),
        (DirectedLocation.LocationDirection.LeftTopRightBottom, 0, 0),
        (DirectedLocation.LocationDirection.RightBottomLeftTop, 2, 2),
        (DirectedLocation.LocationDirection.LeftBottomRightTop, 2, 0),
        (DirectedLocation.LocationDirection.RightTopLeftBottom, 0, 2)
      };

      foreach (var testCase in cases)
      {
        var expectedPlacement = Location(
          testCase.Row,
          testCase.Column,
          testCase.Direction);
        var generator = CreateGenerator(
          3,
          new[]
          {
            "AB"
          },
          CreatePriorityOrderer(new[]
          {
            expectedPlacement
          }),
          true);

        generator.Construct();

        var board = WriteDiagnostics(
          generator,
          3,
          $"Quiz question cell correctly offsets {testCase.Direction}.",
          true);
        var word = generator.Words[0];
        var locations = word.GetAllPlacementLocations(true);

        Assert.AreEqual("AB", word.Text);
        Assert.AreEqual(expectedPlacement, word.Placement);
        Assert.AreEqual(3, locations.Count);
        Assert.AreEqual(
          Board.Cell.CellType.QuizQuestion,
          board.Matrix[locations[0].Row, locations[0].Column].Type);
        Assert.AreEqual('A', board.Matrix[locations[1].Row, locations[1].Column].Char);
        Assert.AreEqual('B', board.Matrix[locations[2].Row, locations[2].Column].Char);
      }
    }

    [TestMethod]
    public void QuizAnswersCanCrossAfterTheirQuestionCells()
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
          "AB", "AB"
        },
        CreatePriorityOrderer(expectedPlacements),
        true);

      generator.Construct();

      var board = WriteDiagnostics(
        generator,
        3,
        "Quiz answers crossed after their exclusive question cells.",
        true);

      Assert.AreEqual(expectedPlacements[0], generator.Words[0].Placement);
      Assert.AreEqual(expectedPlacements[1], generator.Words[1].Placement);
      Assert.AreEqual(Board.Cell.CellType.QuizQuestion, board.Matrix[1, 0].Type);
      Assert.AreEqual(Board.Cell.CellType.QuizQuestion, board.Matrix[0, 1].Type);
      Assert.AreEqual('A', board.Matrix[1, 1].Char);
      Assert.AreEqual(2, board.Matrix[1, 1].Intersections);
      Assert.AreEqual(1, board.IntersectionCount);
    }

    [TestMethod]
    public void RejectsQuizQuestionCellIntersection()
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
        "Quiz question-cell intersection correctly rejected.",
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
          "ABC", "DE", "FG", "HD", "IG"
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
          "ABCD", "EFG", "HIJ", "KL", "MN", "OK", "PN"
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
      Locator.LocationOrderer orderer,
      bool quizMode = false)
    {
      return CreateGenerator(size, size, wordTexts, orderer, quizMode);
    }

    private static WoSeCon CreateGenerator(
      int rowCount,
      int columnCount,
      IEnumerable<string> wordTexts,
      Locator.LocationOrderer orderer,
      bool quizMode = false)
    {
      return new WoSeCon(
        CreateWords(wordTexts),
        rowCount,
        columnCount,
        quizMode,
        orderer);
    }

    private static List<WordInfo> CreateWords(IEnumerable<string> wordTexts)
    {
      return wordTexts
        .Select((text, index) => new WordInfo
        {
          Text = text,
          WordNumber = index + 1
        })
        .ToList();
    }

    private static Locator.LocationOrderer CreatePriorityOrderer(
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

    private static Locator.LocationOrderer CreateShuffledOrderer(int seed)
    {
      return locations =>
      {
        var shuffledLocations = locations.ToList();
        var random = new Random(seed);

        for (var i = shuffledLocations.Count - 1; i > 0; i--)
        {
          var otherIndex = random.Next(i + 1);

          (shuffledLocations[i], shuffledLocations[otherIndex]) =
            (shuffledLocations[otherIndex], shuffledLocations[i]);
        }

        return shuffledLocations;
      };
    }

    private static MemorialConstructionAttempt TryConstructMemorialWords(
      IEnumerable<string> wordTexts,
      int seed,
      CancellationToken cancellationToken)
    {
      try
      {
        var generator = CreateGenerator(
          15,
          17,
          wordTexts,
          CreateShuffledOrderer(seed));

        generator.Construct(cancellationToken: cancellationToken);

        return new MemorialConstructionAttempt(seed, generator, null);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        return new MemorialConstructionAttempt(seed, null, null);
      }
      catch (Exception exception)
      {
        return new MemorialConstructionAttempt(seed, null, exception);
      }
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
      return WriteDiagnostics(generator, size, size, outcome, quizMode);
    }

    private Board WriteDiagnostics(
      WoSeCon generator,
      int rowCount,
      int columnCount,
      string outcome,
      bool quizMode = false)
    {
      var board = new Board(
        generator.Words,
        rowCount,
        columnCount,
        quizMode);

      TestContext.WriteLine(outcome);
      TestContext.WriteLine(board.PrintDiagnostics());
      TestContext.WriteLine($"Backtrackings: {generator.Backtrackings}");
      TestContext.WriteLine($"Tested positions: {generator.TestedPositions}");

      return board;
    }

    private static void AssertFillsGrid(WoSeCon generator, int size)
    {
      Dictionary<(int Row, int Column), char> occupiedCells = new Dictionary<(int Row, int Column), char>();

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

    #region Nested Types

    private sealed record MemorialConstructionAttempt(
      int Seed,
      WoSeCon? Generator,
      Exception? Error);

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
      #region Interface Implementations

      public void Report(T value)
      {
        report(value);
      }

      #endregion
    }

    #endregion
  }
}
