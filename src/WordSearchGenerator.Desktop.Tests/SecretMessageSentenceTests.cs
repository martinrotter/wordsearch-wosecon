using System.Text.Json.Nodes;
using System.Net;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using Wose.Common;
using Wose.Common.WoSeCon.Api;
using Wose.Desktop.Models;
using Wose.Desktop.Models.Rendering;
using Wose.Desktop.Services.Exporting;
using Wose.Desktop.Services.Persistence;
using Wose.Desktop.Services.Rendering;

namespace Wose.Desktop.Tests
{
  [TestClass]
  public sealed class SecretMessageSentenceTests
  {
    private static readonly byte[] Png =
      Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [TestMethod]
    public void TemplateRequiresExactlyOneMarker()
    {
      Assert.IsTrue(SecretMessageTemplate.TrySplit(
        "Před <tajenka> potom",
        out var prefix,
        out var suffix));
      Assert.AreEqual("Před ", prefix);
      Assert.AreEqual(" potom", suffix);
      Assert.IsFalse(SecretMessageTemplate.TrySplit("Bez značky", out _, out _));
      Assert.IsFalse(SecretMessageTemplate.TrySplit(
        "<TAJENKA> a <TAJENKA>",
        out _,
        out _));
    }

    [TestMethod]
    public void HtmlRendersSentenceWithHiddenAndFilledSlots()
    {
      var model = CreateModel("Před <TAJENKA> potom");
      var renderer = new BoardHtmlRenderer(new EmbeddedBoardStyleCatalog());
      var puzzle = renderer.Render(
        model,
        BoardPreviewMode.Puzzle,
        EmbeddedBoardStyleCatalog.EditorialStyleId);
      var solution = renderer.Render(
        model,
        BoardPreviewMode.Solution,
        EmbeddedBoardStyleCatalog.EditorialStyleId);
      var readablePuzzle = WebUtility.HtmlDecode(puzzle);

      Assert.Contains("class=\"message-sentence\"", puzzle);
      Assert.Contains("class=\"message-context\">Před </span>", readablePuzzle);
      Assert.Contains("class=\"message-context\"> potom</span>", readablePuzzle);
      Assert.DoesNotContain(">X</span>", puzzle);
      Assert.Contains(">X</span>", solution);
    }

    [TestMethod]
    public async Task SentenceSurvivesProjectSaveAndLegacyLoad()
    {
      var catalog = new EmbeddedBoardStyleCatalog();
      var serializer = new PuzzleProjectSerializer(catalog);
      var definition = CreateDefinition("Před <TAJENKA> potom");
      var path = Path.Combine(Path.GetTempPath(), $"wose-sentence-{Guid.NewGuid():N}.wose");

      try
      {
        await serializer.SaveAsync(path, definition, null);
        var restored = await serializer.LoadAsync(path);
        Assert.AreEqual(definition.SecretMessageSentence,
          restored.Definition.SecretMessageSentence);

        var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!.AsObject();
        json.Remove("secretMessageSentence");
        await File.WriteAllTextAsync(path, json.ToJsonString());
        var legacy = await serializer.LoadAsync(path);
        Assert.AreEqual(SecretMessageTemplate.Marker,
          legacy.Definition.SecretMessageSentence);
      }
      finally
      {
        if (File.Exists(path))
        {
          File.Delete(path);
        }
      }
    }

    [TestMethod]
    public async Task WordExportPlacesSentenceAroundHiddenAndFilledSlots()
    {
      var model = CreateModel("Před <TAJENKA> potom");
      var exporter = new DocxPuzzleExporter();
      var path = Path.Combine(Path.GetTempPath(), $"wose-sentence-{Guid.NewGuid():N}.docx");

      try
      {
        await exporter.ExportAsync(path, model, BoardPreviewMode.Puzzle, Png);
        using (var puzzle = WordprocessingDocument.Open(path, false))
        {
          var errors = new OpenXmlValidator().Validate(puzzle).ToArray();
          Assert.HasCount(0, errors,
            string.Join(Environment.NewLine,
              errors.Select(error => error.Description)));
          var text = puzzle.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
          Assert.Contains("Před", text);
          Assert.Contains("potom", text);
          Assert.DoesNotContain("XYZ", text);
        }

        await exporter.ExportAsync(path, model, BoardPreviewMode.Solution, Png);
        using var solution = WordprocessingDocument.Open(path, false);
        var solutionErrors = new OpenXmlValidator().Validate(solution).ToArray();
        Assert.HasCount(0, solutionErrors,
          string.Join(Environment.NewLine,
            solutionErrors.Select(error => error.Description)));
        var solutionText = solution.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
        Assert.Contains("Před", solutionText);
        Assert.Contains("potom", solutionText);
        Assert.Contains("X", solutionText);
        Assert.Contains("Y", solutionText);
        Assert.Contains("Z", solutionText);
      }
      finally
      {
        if (File.Exists(path))
        {
          File.Delete(path);
        }
      }
    }

    private static PuzzleDefinition CreateDefinition(string sentence)
    {
      return new PuzzleDefinition(
        PuzzleMode.Normal,
        2,
        3,
        [new PuzzleEntry("ABC")],
        "XYZ",
        string.Empty,
        string.Empty,
        EmbeddedBoardStyleCatalog.EditorialStyleId,
        new GenerationOptions(1, 0),
        secretMessageSentence: sentence);
    }

    private static BoardRenderModel CreateModel(string sentence)
    {
      var definition = CreateDefinition(sentence);
      var word = definition.CreateWordInfos().Single();
      word.Placement = new DirectedLocation
      {
        Row = 0,
        Column = 0,
        Direction = DirectedLocation.LocationDirection.LeftToRight
      };
      var result = new GenerationResult(
        definition,
        new Board([word], definition),
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
  }
}
