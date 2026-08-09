using WordSearchGenerator.Common;
using WordSearchGenerator.Common.WoSeCon.Api;
using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Models.Rendering;
using WordSearchGenerator.Desktop.Services.Rendering;

namespace WordSearchGenerator.Desktop.Tests
{
  [TestClass]
  public sealed class PuzzleDocumentPresentationTests
  {
    #region Other Stuff

    [TestMethod]
    public void NormalEntriesAreSortedForDocumentOutput()
    {
      var model = CreateModel(
        PuzzleMode.Normal,
        string.Empty,
        new PuzzleEntry("pear"),
        new PuzzleEntry("apple"),
        new PuzzleEntry("banana"));

      var entries = PuzzleDocumentPresentation
        .EnumerateEntries(model)
        .Select(entry => entry.Answer)
        .ToArray();

      CollectionAssert.AreEqual(
        new[]
        {
          "apple", "banana", "pear"
        },
        entries);
    }

    [TestMethod]
    public void QuizAnswersAreIncludedOnlyInSolutionOutput()
    {
      var model = CreateModel(
        PuzzleMode.Quiz,
        string.Empty,
        new PuzzleEntry("answer", "Question?"));

      Assert.IsFalse(
        PuzzleDocumentPresentation.ShouldIncludeQuizAnswers(
          model,
          BoardPreviewMode.Puzzle));
      Assert.IsTrue(
        PuzzleDocumentPresentation.ShouldIncludeQuizAnswers(
          model,
          BoardPreviewMode.Solution));
    }

    [TestMethod]
    public void TutorialAndSecretSectionFollowDocumentState()
    {
      var model = CreateModel(
        PuzzleMode.Normal,
        "HI",
        new PuzzleEntry("cat"));

      Assert.IsTrue(
        PuzzleDocumentPresentation.ShouldIncludeTutorial(
          BoardPreviewMode.Puzzle));
      Assert.IsFalse(
        PuzzleDocumentPresentation.ShouldIncludeTutorial(
          BoardPreviewMode.Solution));
      Assert.IsTrue(
        PuzzleDocumentPresentation.ShouldIncludeSecretMessage(model));
      Assert.IsFalse(string.IsNullOrWhiteSpace(
        PuzzleDocumentPresentation.GetTutorialText(model)));
      Assert.IsFalse(string.IsNullOrWhiteSpace(
        PuzzleDocumentPresentation.GetSecretMessageInstructions(
          model,
          BoardPreviewMode.Puzzle)));
    }

    private static BoardRenderModel CreateModel(
      PuzzleMode mode,
      string secretMessage,
      params PuzzleEntry[] entries)
    {
      var rows = entries.Length + (secretMessage.Length == 0 ? 0 : 1);
      var columns = entries.Max(entry => entry.Answer.Length) +
                    (mode == PuzzleMode.Quiz ? 1 : 0);
      var definition = new PuzzleDefinition(
        mode,
        rows,
        columns,
        entries,
        secretMessage,
        string.Empty,
        string.Empty,
        new GenerationOptions(1));
      var words = definition.CreateWordInfos();

      for (var index = 0; index < words.Count; index++)
      {
        words[index].Placement = new DirectedLocation
        {
          Row = index,
          Column = 0,
          Direction = DirectedLocation.LocationDirection.LeftToRight
        };
      }

      var board = new Board(
        words,
        rows,
        columns,
        mode == PuzzleMode.Quiz,
        secretMessage);
      var result = new GenerationResult(
        definition,
        board,
        TimeSpan.Zero,
        0,
        0,
        1,
        1,
        1,
        TimeSpan.Zero,
        0,
        0,
        0,
        0,
        0,
        1,
        0,
        0,
        0);

      return BoardRenderModel.Create(result);
    }

    #endregion
  }
}