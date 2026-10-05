using System.Globalization;
using System.Text;
using Wose.Common;
using Wose.Desktop.Models.Rendering;

namespace Wose.Desktop.Services.Rendering
{
  internal sealed record QuizWordOutline(int Number, string Color, string PathData)
  {
    // The overlay and the gapless cell grid use the same 100-unit cell pitch.
    public const int CellSize = 100;

    private static readonly string[] Palette =
    [
      "#2563eb", "#c65b12", "#008b83", "#8b4dcc", "#bd3a66",
      "#64732b", "#086eaa", "#ae6d00", "#864a30", "#64748b"
    ];

    public static IReadOnlyList<QuizWordOutline> Create(BoardRenderModel model, int insetOffset = 0)
    {
      return model.Cells
        .Where(cell => cell.Kind == Board.Cell.CellType.CharFromText)
        .SelectMany(cell => cell.WordNumbers.Select(number => (number, cell)))
        .GroupBy(item => item.number)
        .OrderBy(group => group.Key)
        .Select(group => new QuizWordOutline(
          group.Key,
          GetColor(group.Key),
          CreatePath(group.Select(item => item.cell)
            .OrderBy(cell => cell.Row)
            .ThenBy(cell => cell.Column)
            .ToArray(), insetOffset)))
        .ToArray();
    }

    public static string GetColor(int number)
    {
      if (number > 0 && number <= Palette.Length)
      {
        return Palette[number - 1];
      }

      // Continue with distinct hues rather than cycling through the palette.
      var hue = (220 + (number - 1) * 137.508) % 360;
      return FormattableString.Invariant($"hsl({hue:0.###}, 65%, 38%)");
    }

    private static string CreatePath(IReadOnlyList<BoardRenderCell> cells, int insetOffset)
    {
      var first = cells[0];
      var last = cells[^1];
      var horizontal = first.Row == last.Row;
      var vertical = first.Column == last.Column;

      // Different insets keep the four line orientations visible at crossings.
      var inset = (horizontal ? 8 : vertical ? 11 : first.Column < last.Column ? 14 : 17) + insetOffset;

      if (horizontal || vertical)
      {
        var left = Math.Min(first.Column, last.Column) * CellSize + inset;
        var top = first.Row * CellSize + inset;
        var right = (Math.Max(first.Column, last.Column) + 1) * CellSize - inset;
        var bottom = (last.Row + 1) * CellSize - inset;
        return FormattableString.Invariant(
          $"M {left} {top} H {right} V {bottom} H {left} Z");
      }

      // Trace one closed stepped perimeter. Narrow necks connect diagonal cells
      // at their corners without enclosing the intervening unused cells.
      var points = new List<(int X, int Y)> { (inset, inset) };
      var far = CellSize - inset;
      const int neck = 8;

      for (var index = 0; index < cells.Count; index++)
      {
        var offset = index * CellSize;
        points.Add((offset + far, offset + inset));

        if (index < cells.Count - 1)
        {
          points.Add((offset + far, offset + far - neck));
          points.Add((offset + CellSize + inset + neck, offset + CellSize + inset));
        }
        else
        {
          points.Add((offset + far, offset + far));
          points.Add((offset + inset, offset + far));
        }
      }

      for (var index = cells.Count - 1; index > 0; index--)
      {
        var offset = index * CellSize;
        points.Add((offset + inset, offset + inset + neck));
        points.Add((offset - CellSize + far - neck, offset - CellSize + far));
        points.Add((offset - CellSize + inset, offset - CellSize + far));
      }

      var path = new StringBuilder();
      var goesRight = first.Column < last.Column;

      foreach (var (x, y) in points)
      {
        var columnPosition = first.Column * CellSize + (goesRight ? x : CellSize - x);
        var rowPosition = first.Row * CellSize + y;
        path.Append(path.Length == 0 ? "M " : " L ");
        path.Append(columnPosition.ToString(CultureInfo.InvariantCulture));
        path.Append(' ');
        path.Append(rowPosition.ToString(CultureInfo.InvariantCulture));
      }

      return path.Append(" Z").ToString();
    }
  }
}
