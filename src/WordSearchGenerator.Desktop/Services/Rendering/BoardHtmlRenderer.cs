using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Wose.Common;
using Wose.Desktop.Localization;
using Wose.Desktop.Models;
using Wose.Desktop.Models.Rendering;

namespace Wose.Desktop.Services.Rendering
{
  public sealed class BoardHtmlRenderer : IBoardHtmlRenderer
  {
    #region Fields

    private readonly IBoardStyleCatalog _styleCatalog;

    #endregion

    #region Constructors

    public BoardHtmlRenderer(IBoardStyleCatalog styleCatalog)
    {
      _styleCatalog = styleCatalog ??
                      throw new ArgumentNullException(nameof(styleCatalog));
    }

    #endregion

    #region Interface Implementations

    public string Render(
      BoardRenderModel model,
      BoardPreviewMode previewMode,
      string styleId)
    {
      ArgumentNullException.ThrowIfNull(model);
      var styleCss = _styleCatalog.GetCss(styleId);

      if (model.Cells.Count != model.Rows * model.Columns)
      {
        throw new ArgumentException(
          AppStrings.Get("RenderCellCountInvalid"),
          nameof(model));
      }

      var isSolution = PuzzleDocumentPresentation.IsSolution(previewMode);
      var browserTitle = string.IsNullOrWhiteSpace(model.PuzzleHeading)
        ? isSolution
          ? AppStrings.Get("HtmlPuzzleSolution")
          : AppStrings.Get("HtmlWordSearchPuzzle")
        : isSolution
          ? AppStrings.Format("HtmlHeadingSolution", model.PuzzleHeading)
          : model.PuzzleHeading;
      var builder = new StringBuilder(24_000);

      AppendDocumentStart(
        builder,
        browserTitle,
        model.PuzzleHeading,
        isSolution,
        styleId,
        styleCss);
      AppendMatrix(builder, model, isSolution);

      if (PuzzleDocumentPresentation.ShouldIncludeTutorial(previewMode))
      {
        AppendTutorial(builder, model);
      }

      if (PuzzleDocumentPresentation.ShouldIncludeSecretMessage(model))
      {
        AppendSecretMessageSection(builder, model, previewMode);
      }

      if (isSolution)
      {
        AppendSolutionDetails(builder, model);
      }

      AppendEntries(builder, model, previewMode);
      builder.AppendLine("    </main>");
      builder.AppendLine("  </body>");
      builder.AppendLine("</html>");

      return builder.ToString();
    }

    #endregion

    #region Other Stuff

    private static void AppendDocumentStart(
      StringBuilder builder,
      string browserTitle,
      string puzzleHeading,
      bool isSolution,
      string styleId,
      string styleCss)
    {
      var modeClass = isSolution ? "solution" : "puzzle";
      var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

      builder.Append($$"""
                       <!DOCTYPE html>
                       <html lang="{{language}}">
                         <head>
                           <meta charset="utf-8">
                           <meta name="viewport" content="width=device-width, initial-scale=1">
                           <meta name="color-scheme" content="light">
                           <title>{{Encode(browserTitle)}}</title>
                           <style>
                       """);
      builder.AppendLine(styleCss);
      builder.Append($$"""
                           </style>
                         </head>
                         <body class="{{modeClass}}" data-style="{{Encode(styleId)}}">
                           <main class="sheet">
                       """);

      if (!string.IsNullOrWhiteSpace(puzzleHeading))
      {
        builder.Append("      <h1 class=\"document-title\">");
        builder.Append(Encode(puzzleHeading));
        builder.AppendLine("</h1>");
      }
    }

    private static void AppendEntries(
      StringBuilder builder,
      BoardRenderModel model,
      BoardPreviewMode previewMode)
    {
      var includeQuizAnswers =
        PuzzleDocumentPresentation.ShouldIncludeQuizAnswers(
          model,
          previewMode);

      builder.AppendLine("      <section class=\"entries\">");

      if (!string.IsNullOrWhiteSpace(model.EntryListHeading))
      {
        builder.Append("        <h2>");
        builder.Append(Encode(model.EntryListHeading));
        builder.AppendLine("</h2>");
      }

      if (model.Mode == PuzzleMode.Normal)
      {
        builder.AppendLine("        <ul class=\"word-list\">");

        foreach (var entry in
                 PuzzleDocumentPresentation.EnumerateEntries(model))
        {
          builder.Append("          <li>");
          builder.Append(Encode(entry.Answer));
          builder.AppendLine("</li>");
        }

        builder.AppendLine("        </ul>");
      }
      else
      {
        builder.AppendLine("        <ol class=\"question-list\">");

        foreach (var entry in model.Entries)
        {
          builder.Append("          <li value=\"");
          builder.Append(entry.Number.ToString(CultureInfo.InvariantCulture));
          builder.Append("\"><span class=\"question\">");
          builder.Append(Encode(entry.Question ?? string.Empty));
          builder.Append("</span>");

          if (includeQuizAnswers)
          {
            builder.Append("<span class=\"answer\">");
            builder.Append(Encode(AppStrings.Get("HtmlAnswer")));
            builder.Append(Encode(entry.Answer));
            builder.Append("</span>");
          }

          builder.AppendLine("</li>");
        }

        builder.AppendLine("        </ol>");
      }

      builder.AppendLine("      </section>");
    }

    private static void AppendMatrix(
      StringBuilder builder,
      BoardRenderModel model,
      bool isSolution)
    {
      builder.Append("      <div class=\"matrix\" style=\"--columns: ");
      builder.Append(model.Columns.ToString(CultureInfo.InvariantCulture));
      builder.Append(";\" role=\"grid\" aria-rowcount=\"");
      builder.Append(model.Rows.ToString(CultureInfo.InvariantCulture));
      builder.Append("\" aria-colcount=\"");
      builder.Append(model.Columns.ToString(CultureInfo.InvariantCulture));
      builder.AppendLine("\">");

      foreach (var cell in model.Cells)
      {
        var classes = GetCellClasses(cell, isSolution);
        var label = GetAccessibleLabel(cell, model.Mode, isSolution);

        builder.Append("        <div class=\"");
        builder.Append(classes);
        builder.Append("\" role=\"gridcell\" aria-label=\"");
        builder.Append(Encode(label));
        builder.Append("\"");

        if (isSolution)
        {
          builder.Append(" title=\"");
          builder.Append(Encode(label));
          builder.Append("\"");
        }

        builder.Append(">");
        AppendCellContent(builder, cell, model.Mode, isSolution);
        builder.AppendLine("</div>");
      }

      builder.AppendLine("      </div>");
    }

    private static void AppendCellContent(
      StringBuilder builder,
      BoardRenderCell cell,
      PuzzleMode mode,
      bool isSolution)
    {
      if (cell.Kind == Board.Cell.CellType.QuizQuestion)
      {
        builder.Append("<span class=\"quiz-number\">");
        builder.Append(
          cell.QuizQuestionNumber.ToString(CultureInfo.InvariantCulture));
        builder.Append("</span><span class=\"quiz-arrow\">");
        builder.Append(Encode(cell.DirectionArrow));
        builder.Append("</span>");
        return;
      }

      var hideAnswer = mode == PuzzleMode.Quiz &&
                       !isSolution &&
                       cell.Kind == Board.Cell.CellType.CharFromText;

      if (!hideAnswer && cell.Character != null)
      {
        builder.Append("<span class=\"cell-letter\">");
        builder.Append(cell.Character == ' '
          ? "&#160;"
          : Encode(cell.Character.Value.ToString()));
        builder.Append("</span>");
      }

      if (cell.MessageIndex != null)
      {
        builder.Append("<span class=\"message-index\" aria-hidden=\"true\">");
        builder.Append(cell.MessageIndex.Value.ToString(CultureInfo.InvariantCulture));
        builder.Append("</span>");
      }
    }

    private static void AppendSecretMessageSection(
      StringBuilder builder,
      BoardRenderModel model,
      BoardPreviewMode previewMode)
    {
      var isSolution = PuzzleDocumentPresentation.IsSolution(previewMode);
      var accessibleLabel = isSolution
        ? AppStrings.Format(
          "HtmlSecretMessageSolutionLabel",
          model.SecretMessage)
        : AppStrings.Format(
          "HtmlSecretMessagePlaceholderLabel",
          model.SecretMessage.Length);

      builder.AppendLine("      <section class=\"secret-message\">");
      builder.Append("        <h2>");
      builder.Append(Encode(AppStrings.Get("SecretMessage")));
      builder.AppendLine("</h2>");
      builder.Append("        <p class=\"secret-message-instructions\">");
      builder.Append(Encode(
        PuzzleDocumentPresentation.GetSecretMessageInstructions(
          model,
          previewMode)));
      builder.AppendLine("</p>");
      builder.Append("        <div class=\"message-slots\" aria-label=\"");
      builder.Append(Encode(accessibleLabel));
      builder.AppendLine("\">");

      foreach (var character in model.SecretMessage)
      {
        builder.Append("          <span class=\"message-slot\" aria-hidden=\"true\">");

        if (isSolution)
        {
          builder.Append(character == ' '
            ? "&#160;"
            : Encode(character.ToString()));
        }
        else
        {
          builder.Append("&#160;");
        }

        builder.AppendLine("</span>");
      }

      builder.AppendLine("        </div>");
      builder.AppendLine("      </section>");
    }

    private static void AppendTutorial(
      StringBuilder builder,
      BoardRenderModel model)
    {
      builder.AppendLine("      <section class=\"tutorial\">");
      builder.Append("        <h2>");
      builder.Append(Encode(AppStrings.Get("HtmlTutorialHeading")));
      builder.AppendLine("</h2>");
      builder.Append("        <p>");
      builder.Append(Encode(
        PuzzleDocumentPresentation.GetTutorialText(model)));
      builder.AppendLine("</p>");
      builder.AppendLine("      </section>");
    }

    private static void AppendSolutionDetails(
      StringBuilder builder,
      BoardRenderModel model)
    {
      builder.AppendLine("      <section class=\"details\">");
      builder.AppendLine("        <div class=\"legend\">");
      AppendLegendItem(builder, "word", AppStrings.Get("HtmlWordCell"));
      AppendLegendItem(
        builder,
        "message",
        AppStrings.Get(model.Mode == PuzzleMode.Quiz
          ? "HtmlQuizExtractionCell"
          : "HtmlMessageCell"));
      AppendLegendItem(
        builder,
        "intersection",
        AppStrings.Get("HtmlIntersection"));
      AppendLegendItem(builder, "black-box", AppStrings.Get("HtmlBlackBox"));
      builder.AppendLine("        </div>");
      builder.Append("        <p class=\"statistics\">");
      builder.Append(Encode(AppStrings.Format(
        model.Mode == PuzzleMode.Quiz
          ? "HtmlQuizStatistics"
          : "HtmlStatistics",
        model.PuzzleCellCount,
        model.MessageCellCount,
        model.BlackBoxCount,
        model.IntersectionCount)));
      builder.AppendLine("</p>");
      builder.AppendLine("      </section>");
    }

    private static void AppendLegendItem(
      StringBuilder builder,
      string cssClass,
      string label)
    {
      builder.Append("          <span class=\"legend-item\"><span class=\"swatch ");
      builder.Append(cssClass);
      builder.Append("\"></span>");
      builder.Append(Encode(label));
      builder.AppendLine("</span>");
    }

    private static string Encode(string value)
    {
      return HtmlEncoder.Default.Encode(value);
    }

    private static string GetAccessibleLabel(
      BoardRenderCell cell,
      PuzzleMode mode,
      bool isSolution)
    {
      var position = AppStrings.Format(
        "HtmlPosition",
        cell.Row + 1,
        cell.Column + 1);

      if (cell.Kind == Board.Cell.CellType.Empty)
      {
        return AppStrings.Format("HtmlBlackBoxLabel", position);
      }

      if (cell.Kind == Board.Cell.CellType.QuizQuestion)
      {
        return AppStrings.Format(
          "HtmlQuestionLabel",
          cell.QuizQuestionNumber,
          cell.DirectionArrow,
          position);
      }

      var character = cell.Character == ' '
        ? AppStrings.Get("HtmlSpace")
        : cell.Character?.ToString() ?? string.Empty;

      if (mode == PuzzleMode.Quiz && !isSolution)
      {
        return cell.MessageIndex == null
          ? AppStrings.Format("HtmlQuizAnswerCellLabel", position)
          : AppStrings.Format(
            "HtmlQuizExtractionCellLabel",
            cell.MessageIndex.Value,
            position);
      }

      if (!isSolution)
      {
        return AppStrings.Format("HtmlLetterLabel", character, position);
      }

      if (cell.Kind == Board.Cell.CellType.CharFromMessage)
      {
        return AppStrings.Format(
          "HtmlMessageCharacterLabel",
          character,
          position);
      }

      if (cell.MessageIndex != null)
      {
        return AppStrings.Format(
          "HtmlQuizExtractionSolutionLabel",
          cell.MessageIndex.Value,
          character,
          position);
      }

      var wordNumbers = string.Join(", ", cell.WordNumbers);
      var role = cell.IsIntersection
        ? AppStrings.Get("HtmlIntersection")
        : AppStrings.Get("HtmlWordCharacter");

      return AppStrings.Format(
        "HtmlWordLabel",
        role,
        character,
        wordNumbers,
        position);
    }

    private static string GetCellClasses(
      BoardRenderCell cell,
      bool isSolution)
    {
      var builder = new StringBuilder("cell");

      if (cell.Kind == Board.Cell.CellType.Empty)
      {
        builder.Append(" black-box");
      }
      else if (cell.Kind == Board.Cell.CellType.QuizQuestion)
      {
        builder.Append(" quiz-question");
      }
      else if (isSolution && cell.Kind == Board.Cell.CellType.CharFromMessage)
      {
        builder.Append(" message");
      }
      else if (isSolution && cell.Kind == Board.Cell.CellType.CharFromText)
      {
        builder.Append(" word");

        if (cell.IsIntersection)
        {
          builder.Append(" intersection");
        }
      }

      if (cell.MessageIndex != null)
      {
        builder.Append(" message-extraction");
      }

      return builder.ToString();
    }

    #endregion
  }
}
