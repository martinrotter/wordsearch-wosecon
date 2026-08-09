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

    [TestMethod]
    public async Task TimedOutWorkerRestartsWithFreshSeed()
    {
      var definition = CreateDefinition(1, 3, string.Empty, 1, "ABC");
      var nextSeed = 0;
      var timeoutCount = 0;
      var generator = new MonteCarloPuzzleGenerator(
        _ => [Interlocked.Increment(ref nextSeed)],
        _ =>
        {
          var cancellation = new CancellationTokenSource();

          if (Interlocked.Increment(ref timeoutCount) == 1)
          {
            cancellation.Cancel();
          }

          return cancellation;
        });

      var result = await generator.GenerateAsync(
        definition,
        null,
        CancellationToken.None);

      Assert.AreEqual(2, result.WinningSeed);
      Assert.AreEqual(2, nextSeed);
      Assert.AreEqual(2, timeoutCount);
      Assert.AreEqual(0, result.CancelledAttemptCount);
    }

    [TestMethod]
    public async Task ZeroAttemptTimeDoesNotCreateTimeouts()
    {
      var definition = CreateDefinition(1, 3, string.Empty, "ABC");
      var generator = new MonteCarloPuzzleGenerator(
        _ => [1],
        _ => throw new AssertFailedException(
          "A zero maximum attempt time must disable timeouts."));

      var result = await generator.GenerateAsync(
        definition,
        null,
        CancellationToken.None);

      Assert.AreEqual(1, result.WinningSeed);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(GenerationOptions.MaximumAttemptTimeSecondsLimit + 1)]
    public void InvalidMaximumAttemptTimeIsRejected(int value)
    {
      Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        new GenerationOptions(1, value));
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
        new GenerationOptions(1, 0));
    }

    private static PuzzleDefinition CreateDefinition(
      int rows,
      int columns,
      string message,
      int maximumAttemptTimeSeconds,
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
        new GenerationOptions(1, maximumAttemptTimeSeconds));
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
