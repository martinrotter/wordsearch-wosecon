using Wose.Common;
using Wose.Common.WoSeCon.Api;
using Wose.Desktop.Models;
using Wose.Desktop.Models.Rendering;
using Wose.Desktop.Services.Rendering;

namespace Wose.Desktop.Tests
{
  [TestClass]
  public sealed class BlindModeTests
  {
    [TestMethod]
    public void BlindCellsAreDistributedAcrossWordAndMessageCellsOnly()
    {
      var model = CreateModel(30);
      var blindCells = model.Cells.Where(cell => cell.IsBlind).ToArray();

      Assert.AreEqual(3, model.BlindCellCount);
      Assert.AreEqual(5, model.BlackBoxCount);
      Assert.AreEqual(
        1,
        blindCells.Count(cell =>
          cell.Kind == Board.Cell.CellType.CharFromText));
      Assert.AreEqual(
        2,
        blindCells.Count(cell =>
          cell.Kind == Board.Cell.CellType.CharFromMessage));
      Assert.IsFalse(blindCells.Any(cell =>
        cell.Kind == Board.Cell.CellType.Empty));

      CollectionAssert.AreEqual(
        new[]
        {
          (0, 3),
          (1, 2),
          (2, 4)
        },
        blindCells.Select(cell => (cell.Row, cell.Column)).ToArray());
    }

    [TestMethod]
    public void BlindSelectionIsDeterministicAndZeroDisablesIt()
    {
      var first = CreateModel(30);
      var second = CreateModel(30);
      var disabled = CreateModel(0);

      CollectionAssert.AreEqual(
        first.Cells.Select(cell => cell.IsBlind).ToArray(),
        second.Cells.Select(cell => cell.IsBlind).ToArray());
      Assert.AreEqual(0, disabled.BlindCellCount);
    }

    [TestMethod]
    public void PuzzleHidesBlindCharactersAndSolutionRevealsThemWithEyeMarker()
    {
      var renderer = new BoardHtmlRenderer(new EmbeddedBoardStyleCatalog());
      var model = CreateModel(30);
      var puzzle = renderer.Render(
        model,
        BoardPreviewMode.Puzzle,
        EmbeddedBoardStyleCatalog.EditorialStyleId);
      var solution = renderer.Render(
        model,
        BoardPreviewMode.Solution,
        EmbeddedBoardStyleCatalog.EditorialStyleId);

      Assert.DoesNotContain(
        "<span class=\"cell-letter\">D</span>",
        puzzle);
      Assert.DoesNotContain("class=\"blind-marker\"", puzzle);
      Assert.Contains("class=\"cell blind\"", puzzle);
      Assert.Contains(
        "<span class=\"cell-letter\">D</span>",
        solution);
      Assert.Contains("class=\"blind-marker\"", solution);
      Assert.Contains("class=\"swatch blind\"", solution);
    }

    [TestMethod]
    public void QuizDefinitionRejectsBlindMode()
    {
      Assert.ThrowsExactly<ArgumentException>(() => new PuzzleDefinition(
        PuzzleMode.Quiz,
        1,
        4,
        [new PuzzleEntry("ABC", "Question?")],
        string.Empty,
        string.Empty,
        string.Empty,
        EmbeddedBoardStyleCatalog.EditorialStyleId,
        new GenerationOptions(1, 0),
        false,
        1));
    }

    private static BoardRenderModel CreateModel(int blindPercentage)
    {
      var definition = new PuzzleDefinition(
        PuzzleMode.Normal,
        3,
        5,
        [new PuzzleEntry("ABCDE")],
        "FGHIJ",
        string.Empty,
        string.Empty,
        EmbeddedBoardStyleCatalog.EditorialStyleId,
        new GenerationOptions(1, 0),
        false,
        blindPercentage);
      var word = definition.CreateWordInfos().Single();
      word.Placement = new DirectedLocation
      {
        Row = 0,
        Column = 0,
        Direction = DirectedLocation.LocationDirection.LeftToRight
      };
      var result = new GenerationResult(
        definition,
        new Board(
          [word],
          definition,
          definition.SecretMessage,
          definition.RequireExactMessageFit),
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
