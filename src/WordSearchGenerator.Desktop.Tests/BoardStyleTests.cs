using Wose.Common;
using Wose.Common.WoSeCon.Api;
using Wose.Desktop.Models;
using Wose.Desktop.Models.Rendering;
using Wose.Desktop.Services.Rendering;

namespace Wose.Desktop.Tests
{
  [TestClass]
  public sealed class BoardStyleTests
  {
    #region Other Stuff

    [TestMethod]
    public void AllBoardStylesAreDiscoveredFromEmbeddedCssFiles()
    {
      var catalog = new EmbeddedBoardStyleCatalog();

      CollectionAssert.AreEqual(
        new[]
        {
          EmbeddedBoardStyleCatalog.EditorialStyleId,
          "fluent-tiles",
          "newspaper"
        },
        catalog.StyleIds.ToArray());
      Assert.AreEqual(
        EmbeddedBoardStyleCatalog.EditorialStyleId,
        catalog.DefaultStyleId);

      foreach (var styleId in catalog.StyleIds)
      {
        var css = catalog.GetCss(styleId);

        Assert.Contains(".matrix", css);
        Assert.Contains("@media print", css);
      }
    }

    [TestMethod]
    public void RendererEmbedsSelectedStyleAndDynamicColumnCount()
    {
      var catalog = new EmbeddedBoardStyleCatalog();
      var renderer = new BoardHtmlRenderer(catalog);
      var model = CreateModel();

      foreach (var styleId in catalog.StyleIds)
      {
        var html = renderer.Render(
          model,
          BoardPreviewMode.Puzzle,
          styleId);

        Assert.Contains($"data-style=\"{styleId}\"", html);
        Assert.Contains("style=\"--columns: 3;\"", html);
        Assert.DoesNotContain("<link", html);
      }
    }

    [TestMethod]
    public void UnknownStyleCannotBeRendered()
    {
      var catalog = new EmbeddedBoardStyleCatalog();
      var renderer = new BoardHtmlRenderer(catalog);

      Assert.ThrowsExactly<ArgumentException>(() => renderer.Render(
        CreateModel(),
        BoardPreviewMode.Puzzle,
        "missing"));
    }

    private static BoardRenderModel CreateModel()
    {
      var definition = new PuzzleDefinition(
        PuzzleMode.Normal,
        1,
        3,
        [new PuzzleEntry("ABC")],
        string.Empty,
        string.Empty,
        string.Empty,
        EmbeddedBoardStyleCatalog.EditorialStyleId,
        new GenerationOptions(1, 0));
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

    #endregion
  }
}
