using Wose.Common;
using Wose.Desktop.Models;
using Wose.Desktop.Models.Rendering;
using Wose.Desktop.Services;
using Wose.Desktop.Services.Persistence;
using Wose.Desktop.Services.Rendering;
using Wose.Desktop.ViewModels;

namespace Wose.Desktop.Tests
{
  [TestClass]
  public sealed class QuizPasteTests
  {
    [TestMethod]
    public void TabSeparatedRowsReplaceInitialBlankRow()
    {
      var viewModel = CreateViewModel();
      viewModel.Mode = PuzzleMode.Quiz;
      var entries = PuzzleInputFileParser.ParseQuizEntries(
        "answer ansver\tquestion\r\nanswer\tquestion\r\naaaa\tquestion");

      var selected = viewModel.InsertPastedQuizEntries(entries, 0);

      Assert.AreEqual(3, viewModel.QuizEntries.Count);
      Assert.AreSame(viewModel.QuizEntries[0], selected);
      CollectionAssert.AreEqual(
        new[] { "answer ansver", "answer", "aaaa" },
        viewModel.QuizEntries.Select(entry => entry.Answer).ToArray());
      Assert.IsTrue(viewModel.IsDirty);
    }

    [TestMethod]
    public void PastedRowsInsertBeforeSelectedEntry()
    {
      var viewModel = CreateViewModel();
      viewModel.Mode = PuzzleMode.Quiz;
      viewModel.QuizEntries.Clear();
      viewModel.QuizEntries.Add(new QuizEntryViewModel
      {
        Answer = "FIRST",
        Question = "First question"
      });
      viewModel.QuizEntries.Add(new QuizEntryViewModel
      {
        Answer = "LAST",
        Question = "Last question"
      });

      viewModel.InsertPastedQuizEntries(
        PuzzleInputFileParser.ParseQuizEntries("MIDDLE\tMiddle question"),
        1);

      CollectionAssert.AreEqual(
        new[] { "FIRST", "MIDDLE", "LAST" },
        viewModel.QuizEntries.Select(entry => entry.Answer).ToArray());
    }

    [TestMethod]
    public void QuizParserAcceptsTabsAndRunsOfSpacesWithTrailingWhitespace()
    {
      var entries = PuzzleInputFileParser.ParseQuizEntries(
        "APPLE\t \t  Which fruit?\r\nNEW YORK   \t  Which city?\r\nMARS  Which planet?");

      CollectionAssert.AreEqual(
        new[] { "APPLE", "NEW YORK", "MARS" },
        entries.Select(entry => entry.Answer).ToArray());
      CollectionAssert.AreEqual(
        new[] { "Which fruit?", "Which city?", "Which planet?" },
        entries.Select(entry => entry.Question).ToArray());
    }

    [TestMethod]
    public void QuizParserDoesNotTreatSingleSpaceAsSeparator()
    {
      Assert.ThrowsExactly<System.IO.InvalidDataException>(() =>
        PuzzleInputFileParser.ParseQuizEntries("NEW YORK"));
    }

    private static MainWindowViewModel CreateViewModel()
    {
      return new MainWindowViewModel(
        new UnusedPuzzleGenerator(),
        new UnusedBoardHtmlRenderer(),
        new EmbeddedBoardStyleCatalog());
    }

    private sealed class UnusedPuzzleGenerator : IPuzzleGenerator
    {
      public Task<GenerationResult> GenerateAsync(
        PuzzleDefinition definition,
        IProgress<MonteCarloProgress>? progress,
        CancellationToken cancellationToken)
      {
        throw new NotSupportedException();
      }
    }

    private sealed class UnusedBoardHtmlRenderer : IBoardHtmlRenderer
    {
      public string Render(
        BoardRenderModel model,
        BoardPreviewMode previewMode,
        string styleId)
      {
        throw new NotSupportedException();
      }
    }
  }
}
