using Wose.Common;
using Wose.Common.WoSeCon.Api;
using Wose.Desktop.Models;
using Wose.Desktop.Services.Persistence;
using Wose.Desktop.Services.Rendering;

namespace Wose.Desktop.Tests
{
  [TestClass]
  public sealed class PuzzleProjectSerializerTests
  {
    #region Other Stuff

    [TestMethod]
    public async Task VersionedProjectIsRejected()
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
      var serializer = new PuzzleProjectSerializer(
        new EmbeddedBoardStyleCatalog());
      var path = Path.Combine(
        Path.GetTempPath(),
        $"wose-version-{Guid.NewGuid():N}.wose");

      try
      {
        await serializer.SaveAsync(path, definition, null);
        var json = await File.ReadAllTextAsync(path);

        Assert.IsFalse(json.Contains(
          "formatVersion",
          StringComparison.Ordinal));

        await File.WriteAllTextAsync(
          path,
          json.Insert(1, "\n  \"formatVersion\": 1,"));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => serializer.LoadAsync(path));
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
    public async Task CandidateStatisticsRoundTripWithGeneratedProject()
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
        new GenerationOptions(4, 17),
        true,
        30);
      var word = new WordInfo
      {
        Text = "ABC",
        WordNumber = 1,
        Placement = new DirectedLocation
        {
          Row = 0,
          Column = 0,
          Direction = DirectedLocation.LocationDirection.LeftToRight
        }
      };
      var result = new GenerationResult(
        definition,
        new Board([word], definition),
        TimeSpan.FromSeconds(1),
        100,
        10,
        4,
        1,
        123,
        TimeSpan.FromMilliseconds(100),
        25,
        2,
        1,
        1,
        1,
        10,
        7,
        2,
        1);
      var serializer = new PuzzleProjectSerializer(
        new EmbeddedBoardStyleCatalog());
      var path = Path.Combine(
        Path.GetTempPath(),
        $"wose-statistics-{Guid.NewGuid():N}.wose");

      try
      {
        await serializer.SaveAsync(path, definition, result);
        var restored = await serializer.LoadAsync(path);

        Assert.IsNotNull(restored.GeneratedResult);
        Assert.AreEqual(
          17,
          restored.Definition.Generation.MaximumAttemptTimeSeconds);
        Assert.AreEqual(
          EmbeddedBoardStyleCatalog.EditorialStyleId,
          restored.Definition.StyleId);
        Assert.IsTrue(restored.Definition.RequireExactMessageFit);
        Assert.AreEqual(30, restored.Definition.BlindPercentage);
        Assert.AreEqual(10, restored.GeneratedResult.CompletedCandidateCount);
        Assert.AreEqual(7, restored.GeneratedResult.MessageCapacityRejectionCount);
        Assert.AreEqual(2, restored.GeneratedResult.AmbiguousBoardRejectionCount);
        Assert.AreEqual(1, restored.GeneratedResult.PlacementFailedAttemptCount);
        Assert.AreEqual(1, restored.GeneratedResult.MessageCapacityRejectedAttemptCount);
        Assert.AreEqual(1, restored.GeneratedResult.AmbiguityRejectedAttemptCount);
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
    public async Task UnknownBoardStyleIsRejected()
    {
      var catalog = new EmbeddedBoardStyleCatalog();
      var definition = new PuzzleDefinition(
        PuzzleMode.Normal,
        1,
        3,
        [new PuzzleEntry("ABC")],
        string.Empty,
        string.Empty,
        string.Empty,
        catalog.DefaultStyleId,
        new GenerationOptions(1, 0));
      var serializer = new PuzzleProjectSerializer(catalog);
      var path = Path.Combine(
        Path.GetTempPath(),
        $"wose-style-{Guid.NewGuid():N}.wose");

      try
      {
        await serializer.SaveAsync(path, definition, null);
        var json = await File.ReadAllTextAsync(path);
        await File.WriteAllTextAsync(
          path,
          json.Replace(
            "\"styleId\": \"editorial\"",
            "\"styleId\": \"missing\"",
            StringComparison.Ordinal));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() =>
          serializer.LoadAsync(path));
      }
      finally
      {
        if (File.Exists(path))
        {
          File.Delete(path);
        }
      }
    }

    #endregion
  }
}
