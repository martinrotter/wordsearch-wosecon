using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using WordSearchGenerator.Desktop.Localization;
using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Models.Rendering;

namespace WordSearchGenerator.Desktop.Services.Rendering
{
  public sealed class BoardHtmlRenderer : IBoardHtmlRenderer
  {
    #region Interface Implementations

    public string Render(
      BoardRenderModel model,
      BoardPreviewMode previewMode)
    {
      ArgumentNullException.ThrowIfNull(model);

      if (model.Cells.Count != model.Rows * model.Columns)
      {
        throw new ArgumentException(
          AppStrings.Get("RenderCellCountInvalid"),
          nameof(model));
      }

      var isSolution = previewMode == BoardPreviewMode.Solution;
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
        model.Columns,
        browserTitle,
        model.PuzzleHeading,
        isSolution);
      AppendMatrix(builder, model, isSolution);

      if (!isSolution)
      {
        AppendTutorial(builder, model);
      }

      if (model.SecretMessage.Length != 0)
      {
        AppendSecretMessageSection(builder, model, isSolution);
      }

      if (isSolution)
      {
        AppendSolutionDetails(builder, model);
      }

      AppendEntries(builder, model, isSolution);
      builder.AppendLine("    </main>");
      builder.AppendLine("  </body>");
      builder.AppendLine("</html>");

      return builder.ToString();
    }

    #endregion

    #region Other Stuff

    private static void AppendDocumentStart(
      StringBuilder builder,
      int columnCount,
      string browserTitle,
      string puzzleHeading,
      bool isSolution)
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
                             :root {
                               --columns: {{columnCount.ToString(CultureInfo.InvariantCulture)}};
                               --cell-max-size: 58px;
                               --ink: #17202a;
                               --muted: #5f6b76;
                               --paper: #ffffff;
                               --canvas: #f3f5f7;
                               --line: #9aa7b2;
                               --word: #dbeafe;
                               --message: #fef3c7;
                               --intersection: #e9d5ff;
                               --quiz: #dcfce7;
                               --black-box: #16191d;
                             }

                             * { box-sizing: border-box; }

                             html, body { min-height: 100%; }

                             body {
                               margin: 0;
                               color: var(--ink);
                               background: var(--canvas);
                               font-family: "Segoe UI", Arial, sans-serif;
                             }

                             .sheet {
                               width: min(100%, 1100px);
                               min-height: 100vh;
                               margin: 0 auto;
                               padding: clamp(16px, 3vw, 38px);
                               background: var(--paper);
                               box-shadow: 0 8px 30px rgba(23, 32, 42, 0.12);
                             }

                             .document-title {
                               margin: 0 0 18px;
                               font-size: clamp(22px, 3vw, 34px);
                               font-weight: 650;
                               text-align: center;
                             }

                             .matrix {
                               display: grid;
                               grid-template-columns: repeat(var(--columns), minmax(0, 1fr));
                               width: min(100%, 900px, calc(var(--columns) * var(--cell-max-size)));
                               margin: 0 auto;
                               overflow: hidden;
                               border: 2px solid #26313a;
                               border-radius: 6px;
                               background: #26313a;
                               break-inside: avoid;
                             }

                             .cell {
                               position: relative;
                               display: flex;
                               align-items: center;
                               justify-content: center;
                               container-type: inline-size;
                               aspect-ratio: 1 / 1;
                               min-width: 0;
                               overflow: hidden;
                               border: 0.5px solid var(--line);
                               background: #ffffff;
                               font-weight: 650;
                               line-height: 1;
                               user-select: none;
                             }

                             .cell-letter {
                               font-size: clamp(8px, 52cqi, 30px);
                             }

                             .cell.black-box {
                               border-color: var(--black-box);
                               background: var(--black-box);
                             }

                             .cell.quiz-question {
                               flex-direction: column;
                               gap: 1px;
                               background: var(--quiz);
                               font-weight: 700;
                             }

                             .quiz-number {
                               font-size: clamp(6px, 30cqi, 18px);
                             }

                             .quiz-arrow {
                               font-size: clamp(7px, 40cqi, 24px);
                             }

                             .solution .cell.word { background: var(--word); }
                             .solution .cell.message { background: var(--message); }
                             .solution .cell.intersection { background: var(--intersection); }
                             .puzzle .cell.message-extraction,
                             .solution .cell.message-extraction { background: var(--message); }

                             .message-index {
                               position: absolute;
                               top: 4%;
                               right: 4%;
                               display: flex;
                               align-items: center;
                               justify-content: center;
                               min-width: 38cqi;
                               min-height: 32cqi;
                               padding: 1cqi 4cqi;
                               border: 1px solid #9a6700;
                               border-radius: 4px;
                               background: #fbbf24;
                               color: #3f2d00;
                               font-size: clamp(7px, 24cqi, 15px);
                               font-weight: 800;
                               line-height: 1;
                             }

                             .tutorial {
                               width: min(100%, 900px);
                               margin: 20px auto 0;
                               padding: 14px 16px;
                               border: 1px solid #d5dce2;
                               border-radius: 8px;
                               background: #f8fafc;
                               break-inside: avoid;
                             }

                             .tutorial h2 {
                               margin: 0 0 5px;
                               font-size: 18px;
                             }

                             .tutorial p {
                               margin: 0;
                               color: var(--muted);
                               font-size: 14px;
                               line-height: 1.45;
                             }

                             .tutorial + .secret-message {
                               margin-top: 14px;
                             }

                             .secret-message {
                               width: min(100%, 900px);
                               margin: 24px auto 0;
                               padding: 16px;
                               border: 1px solid #d5dce2;
                               border-radius: 8px;
                               background: #f8fafc;
                               break-inside: avoid;
                             }

                             .secret-message h2 {
                               margin: 0 0 5px;
                               font-size: 18px;
                             }

                             .secret-message-instructions {
                               margin: 0 0 14px;
                               color: var(--muted);
                               font-size: 13px;
                             }

                             .message-slots {
                               display: flex;
                               flex-wrap: wrap;
                               justify-content: center;
                               gap: 9px 6px;
                             }

                             .message-slot {
                               display: inline-flex;
                               align-items: flex-end;
                               justify-content: center;
                               width: 30px;
                               height: 34px;
                               padding-bottom: 3px;
                               border-bottom: 2px solid var(--ink);
                               font-size: 21px;
                               font-weight: 650;
                               line-height: 1;
                             }

                             .details,
                             .entries {
                               width: min(100%, 900px);
                               margin: 24px auto 0;
                             }

                             .details {
                               padding: 14px 16px;
                               border: 1px solid #d5dce2;
                               border-radius: 8px;
                               background: #f8fafc;
                             }

                             .legend {
                               display: flex;
                               flex-wrap: wrap;
                               gap: 10px 18px;
                               margin-bottom: 10px;
                               color: var(--muted);
                               font-size: 13px;
                             }

                             .legend-item {
                               display: inline-flex;
                               align-items: center;
                               gap: 7px;
                             }

                             .swatch {
                               width: 16px;
                               height: 16px;
                               border: 1px solid var(--line);
                               border-radius: 3px;
                             }

                             .swatch.word { background: var(--word); }
                             .swatch.message { background: var(--message); }
                             .swatch.intersection { background: var(--intersection); }
                             .swatch.black-box { background: var(--black-box); }

                             .statistics {
                               margin: 0;
                               color: var(--muted);
                               font-size: 13px;
                             }

                             .entries h2 {
                               margin: 0 0 12px;
                               font-size: 18px;
                             }

                             .word-list {
                               columns: 3 150px;
                               column-gap: 28px;
                               margin: 0;
                               padding: 0;
                               list-style: none;
                             }

                             .word-list li {
                               padding: 4px 0;
                               break-inside: avoid;
                             }

                             .question-list {
                               margin: 0;
                               padding-left: 28px;
                             }

                             .question-list li {
                               margin-bottom: 9px;
                               padding-left: 4px;
                               break-inside: avoid;
                             }

                             .answer {
                               display: block;
                               margin-top: 2px;
                               color: #1d4ed8;
                               font-weight: 650;
                             }

                             @media (max-width: 620px) {
                               .sheet { padding: 12px; }
                               .document-title { margin-bottom: 12px; }
                               .word-list { columns: 2 120px; }
                             }

                             @media print {
                               @page { margin: 12mm; }

                               body { background: #ffffff; }

                               .sheet {
                                 width: 100%;
                                 min-height: auto;
                                 padding: 0;
                                 box-shadow: none;
                               }

                               .matrix { width: min(100%, 185mm); }
                               .details, .entries { width: min(100%, 185mm); }
                               .cell { print-color-adjust: exact; -webkit-print-color-adjust: exact; }
                             }
                           </style>
                         </head>
                         <body class="{{modeClass}}">
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
      bool isSolution)
    {
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

        foreach (var entry in model.Entries.OrderBy(
                   entry => entry.Answer,
                   StringComparer.CurrentCulture))
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

          if (isSolution)
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
      builder.Append("      <div class=\"matrix\" role=\"grid\" aria-rowcount=\"");
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
      if (cell.Kind == BoardRenderCellKind.QuizQuestion)
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
                       cell.Kind == BoardRenderCellKind.Word;

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
      bool isSolution)
    {
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
      builder.Append(Encode(AppStrings.Get(isSolution
        ? "HtmlSecretMessageSolutionInstructions"
        : model.Mode == PuzzleMode.Quiz
          ? "HtmlSecretMessageQuizInstructions"
          : "HtmlSecretMessageNormalInstructions")));
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
      var instructionsKey = model.Mode == PuzzleMode.Quiz
        ? model.SecretMessage.Length == 0
          ? "HtmlTutorialQuiz"
          : "HtmlTutorialQuizWithMessage"
        : model.SecretMessage.Length == 0
          ? "HtmlTutorialNormal"
          : "HtmlTutorialNormalWithMessage";

      builder.AppendLine("      <section class=\"tutorial\">");
      builder.Append("        <h2>");
      builder.Append(Encode(AppStrings.Get("HtmlTutorialHeading")));
      builder.AppendLine("</h2>");
      builder.Append("        <p>");
      builder.Append(Encode(AppStrings.Get(instructionsKey)));
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

      if (cell.Kind == BoardRenderCellKind.Empty)
      {
        return AppStrings.Format("HtmlBlackBoxLabel", position);
      }

      if (cell.Kind == BoardRenderCellKind.QuizQuestion)
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

      if (cell.Kind == BoardRenderCellKind.Message)
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

      if (cell.Kind == BoardRenderCellKind.Empty)
      {
        builder.Append(" black-box");
      }
      else if (cell.Kind == BoardRenderCellKind.QuizQuestion)
      {
        builder.Append(" quiz-question");
      }
      else if (isSolution && cell.Kind == BoardRenderCellKind.Message)
      {
        builder.Append(" message");
      }
      else if (isSolution && cell.Kind == BoardRenderCellKind.Word)
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
