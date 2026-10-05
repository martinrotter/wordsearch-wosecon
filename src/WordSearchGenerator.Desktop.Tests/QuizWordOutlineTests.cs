using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Wose.Common;
using Wose.Common.WoSeCon.Api;
using Wose.Desktop.Models;
using Wose.Desktop.Models.Rendering;
using Wose.Desktop.Services.Rendering;

namespace Wose.Desktop.Tests
{
  [TestClass]
  public sealed class QuizWordOutlineTests
  {
    private static readonly string[] Styles = ["editorial", "fluent-tiles", "newspaper"];

    [TestMethod]
    [DataRow(DirectedLocation.LocationDirection.LeftToRight)]
    [DataRow(DirectedLocation.LocationDirection.RightToLeft)]
    [DataRow(DirectedLocation.LocationDirection.TopBottom)]
    [DataRow(DirectedLocation.LocationDirection.BottomTop)]
    [DataRow(DirectedLocation.LocationDirection.LeftTopRightBottom)]
    [DataRow(DirectedLocation.LocationDirection.RightBottomLeftTop)]
    [DataRow(DirectedLocation.LocationDirection.LeftBottomRightTop)]
    [DataRow(DirectedLocation.LocationDirection.RightTopLeftBottom)]
    public void EveryDirectionHasOneClosedContourAroundOnlyItsAnswerCells(
      DirectedLocation.LocationDirection direction)
    {
      var word = CreateWord(1, "ABC", 4, 4, direction);
      var model = CreateModel(10, 10, [word]);

      foreach (var style in Styles)
      {
        var svg = ReadSvg(Render(model, style: style));
        var path = svg.Elements().Single();
        var data = path.Attribute("d")!.Value;
        var vertices = ReadVertices(data);
        Assert.AreEqual("0 0 1000 1000", svg.Attribute("viewBox")!.Value);
        Assert.AreEqual("true", svg.Attribute("aria-hidden")!.Value);
        Assert.AreEqual("1", path.Attribute("data-word-number")!.Value);
        Assert.AreEqual(1, data.Count(character => character == 'M'));
        Assert.EndsWith(" Z", data);

        // Check coverage independently: all answer centers are inside, and all
        // other cell centers, including the question cell, are outside.
        foreach (var cell in model.Cells)
        {
          Assert.AreEqual(cell.Kind == Board.Cell.CellType.CharFromText,
            Contains(vertices, cell.Column * 100 + 50, cell.Row * 100 + 50),
            $"{style}: unexpected coverage at row {cell.Row}, column {cell.Column}.");
        }

        var locations = word.GetAllPlacementLocations(true).Skip(1).ToArray();
        if (locations[0].Row == locations[^1].Row || locations[0].Column == locations[^1].Column)
        {
          Assert.AreEqual(4, vertices.Count, "Straight words need only four outer corners.");
        }
        else
        {
          for (var index = 1; index < locations.Length; index++)
          {
            var x = (Math.Min(locations[index - 1].Column, locations[index].Column) + 1) * 100;
            var y = (Math.Min(locations[index - 1].Row, locations[index].Row) + 1) * 100;
            Assert.IsTrue(Contains(vertices, x, y), $"{style}: disconnected diagonal neck.");
          }
        }
      }
    }

    [TestMethod]
    [DataRow(BoardPreviewMode.Puzzle)]
    [DataRow(BoardPreviewMode.Solution)]
    public void FourCrossingWordsKeepSeparateContoursAndExtractionNumbers(BoardPreviewMode previewMode)
    {
      var model = CreateModel(8, 9,
      [
        CreateWord(1, "ABC", 4, 2, DirectedLocation.LocationDirection.LeftToRight),
        CreateWord(2, "DBE", 2, 4, DirectedLocation.LocationDirection.TopBottom),
        CreateWord(3, "FBG", 2, 2, DirectedLocation.LocationDirection.LeftTopRightBottom),
        CreateWord(4, "HBJ", 2, 6, DirectedLocation.LocationDirection.RightTopLeftBottom)
      ], secretMessage: "B");

      foreach (var style in Styles)
      {
        var html = Render(model, previewMode, style);
        var paths = ReadSvg(html).Elements().ToArray();
        Assert.AreEqual(4, paths.Length);
        Assert.AreEqual(4, paths.Select(path => path.Attribute("stroke")!.Value).Distinct().Count());
        Assert.AreEqual(4, paths.Select(path => path.Attribute("d")!.Value).Distinct().Count());
        Assert.AreEqual(1, model.Cells.Count(cell => cell.MessageIndex != null));
        Assert.AreEqual(4, model.Cells.Single(cell => cell.Row == 4 && cell.Column == 4).WordNumbers.Count);
        Assert.Contains("class=\"message-index\" aria-hidden=\"true\">1</span>", html);
        Assert.AreEqual(72, Regex.Matches(html, "role=\"gridcell\"").Count);

        foreach (var path in paths)
        {
          Assert.IsTrue(Contains(ReadVertices(path.Attribute("d")!.Value), 450, 450));
          var number = path.Attribute("data-word-number")!.Value;
          var color = path.Attribute("stroke")!.Value;
          Assert.Contains($"<li value=\"{number}\" style=\"--quiz-word-color: {color};\">", html);
          Assert.AreEqual(2, Regex.Matches(html, Regex.Escape($"style=\"--quiz-word-color: {color};\"")).Count);
        }

        if (previewMode == BoardPreviewMode.Puzzle)
        {
          Assert.DoesNotContain("class=\"cell-letter\"", html);
          Assert.DoesNotContain("class=\"answer\"", html);
        }
        else
        {
          Assert.Contains("class=\"cell-letter\">B</span>", html);
          Assert.Contains("class=\"answer\"", html);
        }
      }
    }

    [TestMethod]
    public void AllStylesEnableOutlinesOnlyForQuizOutput()
    {
      var renderer = new BoardHtmlRenderer(new EmbeddedBoardStyleCatalog());
      var words = new List<WordInfo>
      {
        CreateWord(1, "ABC", 0, 0, DirectedLocation.LocationDirection.LeftToRight)
      };

      foreach (var mode in new[] { PuzzleMode.Normal, PuzzleMode.Quiz })
      foreach (var style in Styles)
      {
        var html = renderer.Render(CreateModel(1, 4, words, mode: mode), BoardPreviewMode.Puzzle, style);
        var outlined = mode == PuzzleMode.Quiz;
        Assert.AreEqual(outlined, html.Contains("<svg class=\"quiz-word-outlines\""));
        Assert.AreEqual(outlined, html.Contains("class=\"matrix quiz-outlined\""));
        Assert.AreEqual(outlined, html.Contains("style=\"--quiz-word-color:"));
        Assert.AreEqual(4, Regex.Matches(html, "role=\"gridcell\"").Count);
      }
    }

    [TestMethod]
    public void ColorsStayStableBeyondTheInitialPaletteAndAcrossCultures()
    {
      var words = Enumerable.Range(1, 14)
        .Select(number => CreateWord(number, $"A{(char)('A' + number)}", number - 1, 0,
          DirectedLocation.LocationDirection.LeftToRight)).ToList();
      var model = CreateModel(14, 3, words);
      var originalCulture = CultureInfo.CurrentCulture;
      var originalUiCulture = CultureInfo.CurrentUICulture;

      try
      {
        foreach (var style in Styles)
        {
          CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
          CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
          var english = Render(model, style: style);
          CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("cs-CZ");
          CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("cs-CZ");
          var czech = Render(model, style: style);
          var englishPaths = ReadSvg(english).Elements().ToArray();
          var czechPaths = ReadSvg(czech).Elements().ToArray();
          Assert.AreEqual(14, englishPaths.Select(path => path.Attribute("stroke")!.Value).Distinct().Count());
          CollectionAssert.AreEqual(englishPaths.Select(path => path.ToString()).ToArray(),
            czechPaths.Select(path => path.ToString()).ToArray());
          Assert.Contains("Follow the matching colored outline", english);
          Assert.Contains("Sledujte obrys", czech);
        }
      }
      finally
      {
        CultureInfo.CurrentCulture = originalCulture;
        CultureInfo.CurrentUICulture = originalUiCulture;
      }
    }

    [TestMethod]
    [DataRow(1, 4, DirectedLocation.LocationDirection.LeftToRight)]
    [DataRow(4, 1, DirectedLocation.LocationDirection.TopBottom)]
    public void NarrowGridsKeepContoursInsideTheFrame(
      int rows, int columns, DirectedLocation.LocationDirection direction)
    {
      var model = CreateModel(rows, columns, [CreateWord(1, "ABC", 0, 0, direction)]);
      foreach (var style in Styles)
      {
        var svg = ReadSvg(Render(model, style: style));
        Assert.AreEqual($"0 0 {columns * 100} {rows * 100}", svg.Attribute("viewBox")!.Value);
        foreach (var vertex in ReadVertices(svg.Elements().Single().Attribute("d")!.Value))
        {
          Assert.IsTrue(vertex.X > 0 && vertex.X < columns * 100);
          Assert.IsTrue(vertex.Y > 0 && vertex.Y < rows * 100);
        }
      }
    }

    private static string Render(
      BoardRenderModel model, BoardPreviewMode mode = BoardPreviewMode.Puzzle, string style = "editorial")
    {
      return new BoardHtmlRenderer(new EmbeddedBoardStyleCatalog()).Render(model, mode, style);
    }

    private static XElement ReadSvg(string html)
    {
      return XElement.Parse(Regex.Match(html, "<svg\\b.*?</svg>", RegexOptions.Singleline).Value);
    }

    private static WordInfo CreateWord(
      int number, string answer, int row, int column, DirectedLocation.LocationDirection direction)
    {
      return new WordInfo
      {
        WordNumber = number, Text = answer,
        Placement = new DirectedLocation { Row = row, Column = column, Direction = direction }
      };
    }

    private static BoardRenderModel CreateModel(
      int rows, int columns, List<WordInfo> words, string secretMessage = "", PuzzleMode mode = PuzzleMode.Quiz)
    {
      var definition = new PuzzleDefinition(mode, rows, columns,
        words.Select(word => new PuzzleEntry(word.Text, "Question?")),
        secretMessage, "Quiz", "Questions", "editorial", new GenerationOptions(1, 0));
      return BoardRenderModel.Create(new GenerationResult(
        definition, new Board(words, definition, secretMessage), TimeSpan.Zero,
        0, 0, 1, 1, 1, TimeSpan.Zero, 0, 0, 0, 0, 0, 1, 0, 0, 0));
    }

    private static List<(int X, int Y)> ReadVertices(string data)
    {
      var tokens = Regex.Matches(data, "[MLHVZ]|-?\\d+").Select(match => match.Value).ToArray();
      var points = new List<(int X, int Y)>();
      var x = 0;
      var y = 0;
      for (var index = 0; index < tokens.Length;)
      {
        var command = tokens[index++];
        if (command == "Z") break;
        if (command is "M" or "L" or "H") x = int.Parse(tokens[index++], CultureInfo.InvariantCulture);
        if (command is "M" or "L" or "V") y = int.Parse(tokens[index++], CultureInfo.InvariantCulture);
        points.Add((x, y));
      }
      return points;
    }

    private static bool Contains(IReadOnlyList<(int X, int Y)> vertices, double x, double y)
    {
      var inside = false;
      for (var index = 0; index < vertices.Count; index++)
      {
        var a = vertices[index];
        var b = vertices[(index + 1) % vertices.Count];
        if ((a.Y > y) != (b.Y > y) && x < (b.X - a.X) * (y - a.Y) / (b.Y - a.Y) + a.X)
        {
          inside = !inside;
        }
      }
      return inside;
    }
  }
}
