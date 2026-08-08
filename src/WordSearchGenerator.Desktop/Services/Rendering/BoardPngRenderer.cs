using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WordSearchGenerator.Desktop.Localization;
using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Models.Rendering;

namespace WordSearchGenerator.Desktop.Services.Rendering
{
  public sealed class BoardPngRenderer
  {
    #region Static Fields

    private static readonly Brush BlackBoxBrush = CreateBrush("#16191D");
    private static readonly Brush InkBrush = CreateBrush("#17202A");
    private static readonly Brush IntersectionBrush = CreateBrush("#E9D5FF");
    private static readonly Brush LineBrush = CreateBrush("#9AA7B2");
    private static readonly Brush MessageBadgeBrush = CreateBrush("#FBBF24");
    private static readonly Brush MessageBadgeBorderBrush = CreateBrush("#9A6700");
    private static readonly Brush MessageBrush = CreateBrush("#FEF3C7");
    private static readonly Brush QuizBrush = CreateBrush("#DCFCE7");
    private static readonly Brush WhiteBrush = Brushes.White;
    private static readonly Brush WordBrush = CreateBrush("#DBEAFE");

    #endregion

    #region Other Stuff

    public byte[] Render(
      BoardRenderModel model,
      BoardPreviewMode previewMode,
      int targetLongSide = 2400)
    {
      ArgumentNullException.ThrowIfNull(model);
      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetLongSide);

      if (model.Rows <= 0 || model.Columns <= 0)
      {
        throw new ArgumentException(
          AppStrings.Get("BoardDimensionsPositive"),
          nameof(model));
      }

      if (model.Cells.Count != model.Rows * model.Columns)
      {
        throw new ArgumentException(
          AppStrings.Get("RenderCellCountInvalid"),
          nameof(model));
      }

      var cellSize = Math.Max(
        1,
        targetLongSide / Math.Max(model.Rows, model.Columns));
      var outerBorder = Math.Max(2, cellSize / 40);
      var pixelWidth = checked(model.Columns * cellSize + outerBorder * 2);
      var pixelHeight = checked(model.Rows * cellSize + outerBorder * 2);
      var visual = new DrawingVisual();

      using (var drawing = visual.RenderOpen())
      {
        var cellPen = new Pen(LineBrush, Math.Max(1, cellSize / 120.0));

        for (var index = 0; index < model.Cells.Count; index++)
        {
          var cell = model.Cells[index];
          var bounds = new Rect(
            outerBorder + cell.Column * cellSize,
            outerBorder + cell.Row * cellSize,
            cellSize,
            cellSize);

          drawing.DrawRectangle(
            GetCellBrush(cell, previewMode),
            cellPen,
            bounds);
          DrawCellContent(
            drawing,
            cell,
            bounds,
            cellSize,
            model.Mode,
            previewMode);
        }

        drawing.DrawRectangle(
          null,
          new Pen(InkBrush, outerBorder),
          new Rect(
            outerBorder / 2.0,
            outerBorder / 2.0,
            pixelWidth - outerBorder,
            pixelHeight - outerBorder));
      }

      var bitmap = new RenderTargetBitmap(
        pixelWidth,
        pixelHeight,
        96,
        96,
        PixelFormats.Pbgra32);
      bitmap.Render(visual);

      var encoder = new PngBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(bitmap));

      using var stream = new MemoryStream();
      encoder.Save(stream);
      return stream.ToArray();
    }

    private static Brush CreateBrush(string color)
    {
      var brush = new SolidColorBrush(
        (Color)ColorConverter.ConvertFromString(color));
      brush.Freeze();
      return brush;
    }

    private static void DrawCellContent(
      DrawingContext drawing,
      BoardRenderCell cell,
      Rect bounds,
      double cellSize,
      PuzzleMode mode,
      BoardPreviewMode previewMode)
    {
      if (cell.Kind == BoardRenderCellKind.QuizQuestion)
      {
        var numberBounds = new Rect(
          bounds.X,
          bounds.Y + cellSize * 0.08,
          bounds.Width,
          bounds.Height * 0.42);
        var arrowBounds = new Rect(
          bounds.X,
          bounds.Y + cellSize * 0.47,
          bounds.Width,
          bounds.Height * 0.43);

        DrawCenteredText(
          drawing,
          cell.QuizQuestionNumber.ToString(CultureInfo.CurrentCulture),
          numberBounds,
          cellSize * 0.27,
          FontWeights.Bold);
        DrawCenteredText(
          drawing,
          cell.DirectionArrow,
          arrowBounds,
          cellSize * 0.31,
          FontWeights.Bold);
        return;
      }

      var hideAnswer = mode == PuzzleMode.Quiz &&
                       previewMode == BoardPreviewMode.Puzzle &&
                       cell.Kind == BoardRenderCellKind.Word;

      if (!hideAnswer && cell.Character is not null and not ' ')
      {
        DrawCenteredText(
          drawing,
          cell.Character.Value.ToString(),
          bounds,
          cellSize * 0.62,
          FontWeights.SemiBold);
      }

      if (cell.MessageIndex != null)
      {
        DrawMessageIndex(
          drawing,
          cell.MessageIndex.Value,
          bounds,
          cellSize);
      }
    }

    private static void DrawMessageIndex(
      DrawingContext drawing,
      int messageIndex,
      Rect bounds,
      double cellSize)
    {
      var badgeSize = cellSize * 0.34;
      var badgeBounds = new Rect(
        bounds.Right - badgeSize - cellSize * 0.04,
        bounds.Top + cellSize * 0.04,
        badgeSize,
        badgeSize);

      drawing.DrawEllipse(
        MessageBadgeBrush,
        new Pen(MessageBadgeBorderBrush, Math.Max(1, cellSize / 100.0)),
        new Point(badgeBounds.X + badgeBounds.Width / 2,
          badgeBounds.Y + badgeBounds.Height / 2),
        badgeBounds.Width / 2,
        badgeBounds.Height / 2);
      DrawCenteredText(
        drawing,
        messageIndex.ToString(CultureInfo.CurrentCulture),
        badgeBounds,
        cellSize * 0.18,
        FontWeights.Bold);
    }

    private static void DrawCenteredText(
      DrawingContext drawing,
      string text,
      Rect bounds,
      double fontSize,
      FontWeight fontWeight)
    {
      var formattedText = new FormattedText(
        text,
        CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight,
        new Typeface(
          new FontFamily("Segoe UI"),
          FontStyles.Normal,
          fontWeight,
          FontStretches.Normal),
        fontSize,
        InkBrush,
        1.0);
      var point = new Point(
        bounds.X +
        Math.Max(0, bounds.Width - formattedText.WidthIncludingTrailingWhitespace) /
        2,
        bounds.Y + Math.Max(0, bounds.Height - formattedText.Height) / 2);

      drawing.DrawText(formattedText, point);
    }

    private static Brush GetCellBrush(
      BoardRenderCell cell,
      BoardPreviewMode previewMode)
    {
      if (cell.Kind == BoardRenderCellKind.Empty)
      {
        return BlackBoxBrush;
      }

      if (cell.Kind == BoardRenderCellKind.QuizQuestion)
      {
        return QuizBrush;
      }

      if (cell.MessageIndex != null)
      {
        return MessageBrush;
      }

      if (previewMode == BoardPreviewMode.Puzzle)
      {
        return WhiteBrush;
      }

      if (cell.Kind == BoardRenderCellKind.Message)
      {
        return MessageBrush;
      }

      return cell.IsIntersection ? IntersectionBrush : WordBrush;
    }

    #endregion
  }
}