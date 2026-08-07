using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
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
          "The render model must contain exactly one cell per matrix position.",
          nameof(model));
      }

      var isSolution = previewMode == BoardPreviewMode.Solution;
      var browserTitle = string.IsNullOrWhiteSpace(model.PuzzleHeading)
        ? isSolution ? "Puzzle solution" : "Word-search puzzle"
        : isSolution
          ? $"{model.PuzzleHeading} - Solution"
          : model.PuzzleHeading;
      var builder = new StringBuilder(24_000);

      AppendDocumentStart(
        builder,
        model.Columns,
        browserTitle,
        model.PuzzleHeading,
        isSolution);
      AppendMatrix(builder, model, isSolution);

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

      builder.Append($$"""
        <!DOCTYPE html>
        <html lang="en">
          <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta name="color-scheme" content="light">
            <title>{{Encode(browserTitle)}}</title>
            <style>
              :root {
                --columns: {{columnCount.ToString(CultureInfo.InvariantCulture)}};
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
                width: min(100%, 900px);
                margin: 0 auto;
                overflow: hidden;
                border: 2px solid #26313a;
                background: #26313a;
                break-inside: avoid;
              }

              .cell {
                display: flex;
                align-items: center;
                justify-content: center;
                aspect-ratio: 1 / 1;
                min-width: 0;
                overflow: hidden;
                border: 0.5px solid var(--line);
                background: #ffffff;
                font-size: clamp(8px, 3.2vmin, 30px);
                font-weight: 650;
                line-height: 1;
                user-select: none;
              }

              .cell.black-box {
                border-color: var(--black-box);
                background: var(--black-box);
              }

              .cell.quiz-question {
                flex-direction: column;
                gap: 1px;
                background: var(--quiz);
                font-size: clamp(7px, 1.8vmin, 18px);
                font-weight: 700;
              }

              .quiz-arrow { font-size: 1.15em; }

              .solution .cell.word { background: var(--word); }
              .solution .cell.message { background: var(--message); }
              .solution .cell.intersection { background: var(--intersection); }

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
            builder.Append("<span class=\"answer\">Answer: ");
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
        var label = GetAccessibleLabel(cell, isSolution);

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
        AppendCellContent(builder, cell);
        builder.AppendLine("</div>");
      }

      builder.AppendLine("      </div>");
    }

    private static void AppendCellContent(
      StringBuilder builder,
      BoardRenderCell cell)
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

      if (cell.Character == null)
      {
        return;
      }

      builder.Append(cell.Character == ' '
        ? "&#160;"
        : Encode(cell.Character.Value.ToString()));
    }

    private static void AppendSolutionDetails(
      StringBuilder builder,
      BoardRenderModel model)
    {
      builder.AppendLine("      <section class=\"details\">");
      builder.AppendLine("        <div class=\"legend\">");
      AppendLegendItem(builder, "word", "Word cell");
      AppendLegendItem(builder, "message", "Secret-message cell");
      AppendLegendItem(builder, "intersection", "Word intersection");
      AppendLegendItem(builder, "black-box", "Black box");
      builder.AppendLine("        </div>");
      builder.Append("        <p class=\"statistics\">");
      builder.Append($"{model.PuzzleCellCount:N0} puzzle cells, ");
      builder.Append($"{model.MessageCellCount:N0} message cells, ");
      builder.Append($"{model.BlackBoxCount:N0} black boxes and ");
      builder.Append($"{model.IntersectionCount:N0} intersections.");
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
      bool isSolution)
    {
      var position = $"row {cell.Row + 1}, column {cell.Column + 1}";

      if (cell.Kind == BoardRenderCellKind.Empty)
      {
        return $"Black box, {position}";
      }

      if (cell.Kind == BoardRenderCellKind.QuizQuestion)
      {
        return $"Question {cell.QuizQuestionNumber}, direction " +
               $"{cell.DirectionArrow}, {position}";
      }

      var character = cell.Character == ' '
        ? "space"
        : cell.Character?.ToString() ?? string.Empty;

      if (!isSolution)
      {
        return $"Letter {character}, {position}";
      }

      if (cell.Kind == BoardRenderCellKind.Message)
      {
        return $"Secret-message character {character}, {position}";
      }

      var wordNumbers = string.Join(", ", cell.WordNumbers);
      var role = cell.IsIntersection ? "Word intersection" : "Word character";

      return $"{role} {character}, words {wordNumbers}, {position}";
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

      return builder.ToString();
    }

    #endregion
  }
}
