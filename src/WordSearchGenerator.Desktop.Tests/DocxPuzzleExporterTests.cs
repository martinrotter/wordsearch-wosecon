using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using Wose.Common;
using Wose.Common.WoSeCon.Api;
using Wose.Desktop.Models;
using Wose.Desktop.Models.Rendering;
using Wose.Desktop.Services.Exporting;
using Wose.Desktop.Services.Rendering;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace Wose.Desktop.Tests
{
  [TestClass]
  public sealed class DocxPuzzleExporterTests
  {
    #region Static Fields

    private static readonly byte[] Png = Convert.FromBase64String(
      "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    #endregion

    #region Other Stuff

    [TestMethod]
    public async Task NormalPuzzleCreatesValidDocumentWithExpectedContent()
    {
      var exporter = new DocxPuzzleExporter();
      var model = CreateModel(
        PuzzleMode.Normal,
        "Village puzzle",
        "HI",
        new PuzzleEntry("pear"),
        new PuzzleEntry("apple"),
        new PuzzleEntry("banana"));
      var path = CreateTemporaryPath();

      try
      {
        await exporter.ExportAsync(
          path,
          model,
          BoardPreviewMode.Puzzle,
          Png);

        using var document = WordprocessingDocument.Open(path, false);
        var mainPart = document.MainDocumentPart ??
                       throw new AssertFailedException(
                         "The document has no main part.");
        var validationErrors = new OpenXmlValidator()
          .Validate(document)
          .ToArray();

        Assert.HasCount(1, mainPart.ImageParts);
        Assert.IsNotNull(mainPart.DocumentSettingsPart?.Settings?
          .GetFirstChild<W.DoNotAutoCompressPictures>());
        Assert.IsNotNull(mainPart.StyleDefinitionsPart?.Styles);
        Assert.IsNotNull(mainPart.NumberingDefinitionsPart?.Numbering);
        Assert.HasCount(
          0,
          validationErrors,
          string.Join(
            Environment.NewLine,
            validationErrors.Select(error =>
              $"{error.Description} ({error.Path?.XPath})")));

        var text = mainPart.Document?.Body?.InnerText ?? string.Empty;
        Assert.Contains("Village puzzle", text);
        Assert.Contains("apple", text);
        Assert.Contains("banana", text);
        Assert.Contains("pear", text);
        var wordCells = mainPart.Document?.Body?
          .Elements<W.Table>()
          .Last()
          .Descendants<W.TableCell>()
          .Select(cell => cell.InnerText)
          .Where(value => value.Length != 0)
          .ToArray();
        CollectionAssert.AreEqual(
          new[]
          {
            "apple", "banana", "pear"
          },
          wordCells);
      }
      finally
      {
        DeleteIfPresent(path);
      }
    }

    [TestMethod]
    public async Task QuizPuzzleContainsQuestionsButNotAnswers()
    {
      var exporter = new DocxPuzzleExporter();
      const string answer = "hiddenanswer";
      const string question = "What should remain editable?";
      var model = CreateModel(
        PuzzleMode.Quiz,
        string.Empty,
        string.Empty,
        new PuzzleEntry(answer, question));
      var path = CreateTemporaryPath();

      try
      {
        await exporter.ExportAsync(
          path,
          model,
          BoardPreviewMode.Puzzle,
          Png);

        using var document = WordprocessingDocument.Open(path, false);
        var mainPart = document.MainDocumentPart ??
                       throw new AssertFailedException(
                         "The document has no main part.");
        var text = mainPart.Document?.Body?.InnerText ?? string.Empty;

        Assert.Contains(question, text);
        Assert.DoesNotContain(answer, text);
        Assert.IsTrue(mainPart.Document?.Body?
          .Descendants<W.NumberingProperties>()
          .Any());
      }
      finally
      {
        DeleteIfPresent(path);
      }
    }

    [TestMethod]
    public async Task QuizSolutionContainsAnswers()
    {
      var exporter = new DocxPuzzleExporter();
      const string answer = "visibleanswer";
      var model = CreateModel(
        PuzzleMode.Quiz,
        string.Empty,
        string.Empty,
        new PuzzleEntry(answer, "What is the answer?"));
      var path = CreateTemporaryPath();

      try
      {
        await exporter.ExportAsync(
          path,
          model,
          BoardPreviewMode.Solution,
          Png);

        using var document = WordprocessingDocument.Open(path, false);
        var text = document.MainDocumentPart?.Document?.Body?.InnerText ??
                   string.Empty;

        Assert.Contains(answer, text);
      }
      finally
      {
        DeleteIfPresent(path);
      }
    }

    private static BoardRenderModel CreateModel(
      PuzzleMode mode,
      string heading,
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
        heading,
        mode == PuzzleMode.Normal ? "Words" : "Questions",
        EmbeddedBoardStyleCatalog.EditorialStyleId,
        new GenerationOptions(1, 0));
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
        definition,
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

    private static string CreateTemporaryPath()
    {
      return Path.Combine(
        Path.GetTempPath(),
        $"wosecon-docx-{Guid.NewGuid():N}.docx");
    }

    private static void DeleteIfPresent(string path)
    {
      if (File.Exists(path))
      {
        File.Delete(path);
      }
    }

    #endregion
  }
}
