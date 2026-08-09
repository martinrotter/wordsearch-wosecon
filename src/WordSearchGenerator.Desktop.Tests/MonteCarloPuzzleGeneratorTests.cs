using System.Collections.Concurrent;
using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Services;

namespace WordSearchGenerator.Desktop.Tests
{
  [TestClass]
  public sealed class MonteCarloPuzzleGeneratorTests
  {
    #region Other Stuff

    [TestMethod]
    public async Task CapacityRejectedCandidatesArePreservedWhenAttemptExhausts()
    {
      var definition = CreateDefinition(2, 2, "ABC", "AB");
      var generator = new MonteCarloPuzzleGenerator(_ => [1]);
      var updates = new ConcurrentQueue<MonteCarloProgress>();

      var exception = await Assert.ThrowsExactlyAsync<MonteCarloGenerationException>(() =>
        generator.GenerateAsync(
          definition,
          new InlineProgress<MonteCarloProgress>(updates.Enqueue),
          CancellationToken.None));

      Assert.IsGreaterThan(0, exception.MessageCapacityRejectionCount);
      Assert.AreEqual(
        exception.MessageCapacityRejectionCount,
        exception.CompletedCandidateCount);
      Assert.AreEqual(0, exception.AmbiguousBoardRejectionCount);
      Assert.IsTrue(updates.Any(update =>
        update.MessageCapacityRejectionCount ==
        exception.MessageCapacityRejectionCount));
    }

    [TestMethod]
    public async Task AmbiguousCandidatesUseTheSameCompletedLayoutUnit()
    {
      var definition = CreateDefinition(2, 3, "CAT", "CAT");
      var generator = new MonteCarloPuzzleGenerator(_ => [1]);

      var exception = await Assert.ThrowsExactlyAsync<MonteCarloGenerationException>(() =>
        generator.GenerateAsync(
          definition,
          null,
          CancellationToken.None));

      Assert.IsGreaterThan(0, exception.AmbiguousBoardRejectionCount);
      Assert.AreEqual(
        exception.AmbiguousBoardRejectionCount,
        exception.CompletedCandidateCount);
      Assert.AreEqual(0, exception.MessageCapacityRejectionCount);
    }

    private static PuzzleDefinition CreateDefinition(
      int rows,
      int columns,
      string message,
      params string[] words)
    {
      return new PuzzleDefinition(
        PuzzleMode.Normal,
        rows,
        columns,
        words.Select(word => new PuzzleEntry(word)),
        message,
        string.Empty,
        string.Empty,
        new GenerationOptions(1));
    }

    #endregion

    #region Nested Types

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
      #region Interface Implementations

      public void Report(T value)
      {
        report(value);
      }

      #endregion
    }

    #endregion
  }
}