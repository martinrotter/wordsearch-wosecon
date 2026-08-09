using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Models.Rendering;
using WordSearchGenerator.Desktop.Services;
using WordSearchGenerator.Desktop.Services.Rendering;
using WordSearchGenerator.Desktop.ViewModels;

namespace WordSearchGenerator.Desktop.Tests
{
  [TestClass]
  public sealed class UppercaseConversionTests
  {
    #region Other Stuff

    [TestMethod]
    public void NormalModeConvertsWordsAndPreservesFormatting()
    {
      var viewModel = CreateViewModel();
      viewModel.WordsText = "  red fox  \r\nblue whale\r\nEAGLE";

      Assert.IsTrue(viewModel.ConvertToUppercaseCommand.CanExecute(null));

      viewModel.ConvertToUppercaseCommand.Execute(null);

      Assert.AreEqual(
        "  RED FOX  \r\nBLUE WHALE\r\nEAGLE",
        viewModel.WordsText);
      Assert.IsFalse(viewModel.ConvertToUppercaseCommand.CanExecute(null));
    }

    [TestMethod]
    public void QuizModeConvertsOnlyAnswers()
    {
      var viewModel = CreateViewModel();
      const string question = "Which city is known as the Big Apple?";

      viewModel.Mode = PuzzleMode.Quiz;
      viewModel.QuizEntries.Clear();
      viewModel.QuizEntries.Add(new QuizEntryViewModel
      {
        Answer = "new york",
        Question = question
      });

      Assert.IsTrue(viewModel.ConvertToUppercaseCommand.CanExecute(null));

      viewModel.ConvertToUppercaseCommand.Execute(null);

      Assert.AreEqual("NEW YORK", viewModel.QuizEntries[0].Answer);
      Assert.AreEqual(question, viewModel.QuizEntries[0].Question);
      Assert.IsFalse(viewModel.ConvertToUppercaseCommand.CanExecute(null));
    }

    private static MainWindowViewModel CreateViewModel()
    {
      return new MainWindowViewModel(
        new UnusedPuzzleGenerator(),
        new UnusedBoardHtmlRenderer(),
        new EmbeddedBoardStyleCatalog());
    }

    #endregion

    #region Nested Types

    private sealed class UnusedPuzzleGenerator : IPuzzleGenerator
    {
      #region Interface Implementations

      public Task<GenerationResult> GenerateAsync(
        PuzzleDefinition definition,
        IProgress<MonteCarloProgress>? progress,
        CancellationToken cancellationToken)
      {
        throw new NotSupportedException();
      }

      #endregion
    }

    private sealed class UnusedBoardHtmlRenderer : IBoardHtmlRenderer
    {
      #region Interface Implementations

      public string Render(
        BoardRenderModel model,
        BoardPreviewMode previewMode,
        string styleId)
      {
        throw new NotSupportedException();
      }

      #endregion
    }

    #endregion
  }
}
