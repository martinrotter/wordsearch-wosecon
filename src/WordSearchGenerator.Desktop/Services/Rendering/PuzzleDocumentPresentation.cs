using WordSearchGenerator.Desktop.Localization;
using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Models.Rendering;

namespace WordSearchGenerator.Desktop.Services.Rendering
{
  public static class PuzzleDocumentPresentation
  {
    #region Other Stuff

    public static IEnumerable<BoardRenderEntry> EnumerateEntries(
      BoardRenderModel model)
    {
      ArgumentNullException.ThrowIfNull(model);

      return model.Mode == PuzzleMode.Normal
        ? model.Entries.OrderBy(
          entry => entry.Answer,
          StringComparer.CurrentCulture)
        : model.Entries;
    }

    public static string GetSecretMessageInstructions(
      BoardRenderModel model,
      BoardPreviewMode previewMode)
    {
      ArgumentNullException.ThrowIfNull(model);

      return AppStrings.Get(IsSolution(previewMode)
        ? "HtmlSecretMessageSolutionInstructions"
        : model.Mode == PuzzleMode.Quiz
          ? "HtmlSecretMessageQuizInstructions"
          : "HtmlSecretMessageNormalInstructions");
    }

    public static string GetTutorialText(BoardRenderModel model)
    {
      ArgumentNullException.ThrowIfNull(model);

      var instructionsKey = model.Mode == PuzzleMode.Quiz
        ? model.SecretMessage.Length == 0
          ? "HtmlTutorialQuiz"
          : "HtmlTutorialQuizWithMessage"
        : model.SecretMessage.Length == 0
          ? "HtmlTutorialNormal"
          : "HtmlTutorialNormalWithMessage";

      return AppStrings.Get(instructionsKey);
    }

    public static bool IsSolution(BoardPreviewMode previewMode)
    {
      return previewMode == BoardPreviewMode.Solution;
    }

    public static bool ShouldIncludeQuizAnswers(
      BoardRenderModel model,
      BoardPreviewMode previewMode)
    {
      ArgumentNullException.ThrowIfNull(model);

      return model.Mode == PuzzleMode.Quiz && IsSolution(previewMode);
    }

    public static bool ShouldIncludeSecretMessage(BoardRenderModel model)
    {
      ArgumentNullException.ThrowIfNull(model);

      return model.SecretMessage.Length != 0;
    }

    public static bool ShouldIncludeTutorial(BoardPreviewMode previewMode)
    {
      return !IsSolution(previewMode);
    }

    #endregion
  }
}