using System.Diagnostics;
using System.Security.Cryptography;
using WordSearchGenerator.Common;
using WordSearchGenerator.Common.WoSeCon;
using WordSearchGenerator.Common.WoSeCon.Api;
using WordSearchGenerator.Desktop.Models;

namespace WordSearchGenerator.Desktop.Services
{
  public sealed class MonteCarloPuzzleGenerator : IPuzzleGenerator
  {
    #region Constants

    private const string NoSolutionMessage =
      "given words cannot fit into the grid";

    #endregion

    #region Interface Implementations

    public async Task<GenerationResult> GenerateAsync(
      PuzzleDefinition definition,
      IProgress<MonteCarloProgress>? progress,
      CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(definition);
      cancellationToken.ThrowIfCancellationRequested();

      var attemptCount = definition.Generation.ParallelAttempts;
      var seeds = CreateSeeds(attemptCount);
      using var linkedCancellation =
        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      var aggregator = new ProgressAggregator(
        attemptCount,
        definition.Entries.Count,
        progress);
      var remainingTasks = Enumerable
        .Range(0, attemptCount)
        .Select(index => Task.Run(
          () => RunAttempt(
            definition,
            index,
            seeds[index],
            aggregator,
            linkedCancellation.Token),
          CancellationToken.None))
        .ToList();

      aggregator.Report(force: true);
      AttemptOutcome? winner = null;

      while (remainingTasks.Count != 0)
      {
        var completedTask = await Task
          .WhenAny(remainingTasks)
          .ConfigureAwait(false);
        remainingTasks.Remove(completedTask);
        var outcome = await completedTask.ConfigureAwait(false);

        if (outcome.Error != null)
        {
          linkedCancellation.Cancel();

          if (remainingTasks.Count != 0)
          {
            await Task.WhenAll(remainingTasks).ConfigureAwait(false);
          }

          aggregator.Report(force: true);
          cancellationToken.ThrowIfCancellationRequested();

          throw new InvalidOperationException(
            $"Generation attempt {outcome.AttemptNumber} failed unexpectedly.",
            outcome.Error);
        }

        if (outcome.Board == null)
        {
          continue;
        }

        winner = outcome;
        linkedCancellation.Cancel();
        break;
      }

      if (remainingTasks.Count != 0)
      {
        await Task.WhenAll(remainingTasks).ConfigureAwait(false);
      }

      var finalProgress = aggregator.Report(force: true);
      cancellationToken.ThrowIfCancellationRequested();

      if (winner == null)
      {
        throw new MonteCarloGenerationException(
          attemptCount,
          finalProgress.PlacementFailureCount,
          finalProgress.MessageRejectedAttemptCount);
      }

      return new GenerationResult(
        definition,
        winner.Board!,
        finalProgress.Elapsed,
        finalProgress.TestedPositions,
        finalProgress.Backtrackings,
        attemptCount,
        winner.AttemptNumber,
        winner.Seed,
        winner.Elapsed,
        winner.TestedPositions,
        winner.Backtrackings,
        finalProgress.PlacementFailureCount,
        finalProgress.MessageRejectedAttemptCount,
        finalProgress.CancelledAttemptCount);
    }

    #endregion

    #region Other Stuff

    private static int[] CreateSeeds(int count)
    {
      var usedSeeds = new HashSet<int>();
      var seeds = new int[count];

      for (var index = 0; index < seeds.Length; index++)
      {
        int seed;

        do
        {
          seed = RandomNumberGenerator.GetInt32(1, int.MaxValue);
        } while (!usedSeeds.Add(seed));

        seeds[index] = seed;
      }

      return seeds;
    }

    private static IReadOnlyList<DirectedLocation> ShuffleLocations(
      IReadOnlyList<DirectedLocation> locations,
      int seed)
    {
      var shuffled = locations.ToList();
      var random = new Random(seed);

      for (var index = shuffled.Count - 1; index > 0; index--)
      {
        var swapIndex = random.Next(index + 1);
        (shuffled[index], shuffled[swapIndex]) =
          (shuffled[swapIndex], shuffled[index]);
      }

      return shuffled;
    }

    private static AttemptOutcome RunAttempt(
      PuzzleDefinition definition,
      int attemptIndex,
      int seed,
      ProgressAggregator aggregator,
      CancellationToken cancellationToken)
    {
      var stopwatch = Stopwatch.StartNew();
      WoSeCon? generator = null;

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        generator = new WoSeCon(
          definition.CreateWordInfos(),
          definition.Rows,
          definition.Columns,
          definition.QuizMode,
          locations => ShuffleLocations(locations, seed));
        var attemptProgress = new DelegateProgress<ConstructionProgress>(
          value => aggregator.Update(attemptIndex, value));

        generator.Construct(attemptProgress, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var boardWithoutMessage = new Board(
          generator.Words,
          definition.Rows,
          definition.Columns,
          definition.QuizMode);
        var availableCellCount = boardWithoutMessage.Matrix
          .OfType<Board.Cell>()
          .Count(cell => cell.Type == Board.Cell.CellType.Empty);

        if (definition.SecretMessage.Length > availableCellCount)
        {
          stopwatch.Stop();
          aggregator.Complete(
            attemptIndex,
            AttemptCompletion.MessageRejected,
            generator.TestedPositions,
            generator.Backtrackings);

          return AttemptOutcome.Rejected(
            attemptIndex + 1,
            seed,
            stopwatch.Elapsed,
            generator.TestedPositions,
            generator.Backtrackings);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var board = new Board(
          generator.Words,
          definition.Rows,
          definition.Columns,
          definition.QuizMode,
          definition.SecretMessage);

        stopwatch.Stop();
        aggregator.Complete(
          attemptIndex,
          AttemptCompletion.Succeeded,
          generator.TestedPositions,
          generator.Backtrackings);

        return AttemptOutcome.Succeeded(
          attemptIndex + 1,
          seed,
          board,
          stopwatch.Elapsed,
          generator.TestedPositions,
          generator.Backtrackings);
      }
      catch (OperationCanceledException)
      {
        stopwatch.Stop();
        aggregator.Complete(
          attemptIndex,
          AttemptCompletion.Cancelled,
          generator?.TestedPositions ?? 0,
          generator?.Backtrackings ?? 0);

        return AttemptOutcome.Cancelled(
          attemptIndex + 1,
          seed,
          stopwatch.Elapsed);
      }
      catch (Exception exception)
        when (exception.Message == NoSolutionMessage)
      {
        stopwatch.Stop();
        aggregator.Complete(
          attemptIndex,
          AttemptCompletion.PlacementFailed,
          generator?.TestedPositions ?? 0,
          generator?.Backtrackings ?? 0);

        return AttemptOutcome.Failed(
          attemptIndex + 1,
          seed,
          stopwatch.Elapsed,
          generator?.TestedPositions ?? 0,
          generator?.Backtrackings ?? 0);
      }
      catch (Exception exception)
      {
        stopwatch.Stop();
        aggregator.Complete(
          attemptIndex,
          AttemptCompletion.Faulted,
          generator?.TestedPositions ?? 0,
          generator?.Backtrackings ?? 0);

        return AttemptOutcome.Faulted(
          attemptIndex + 1,
          seed,
          stopwatch.Elapsed,
          generator?.TestedPositions ?? 0,
          generator?.Backtrackings ?? 0,
          exception);
      }
    }

    private enum AttemptCompletion
    {
      Running,
      Succeeded,
      PlacementFailed,
      MessageRejected,
      Cancelled,
      Faulted
    }

    private sealed record AttemptOutcome(
      int AttemptNumber,
      int Seed,
      Board? Board,
      TimeSpan Elapsed,
      long TestedPositions,
      int Backtrackings,
      Exception? Error)
    {
      public static AttemptOutcome Cancelled(
        int attemptNumber,
        int seed,
        TimeSpan elapsed)
      {
        return new AttemptOutcome(
          attemptNumber,
          seed,
          null,
          elapsed,
          0,
          0,
          null);
      }

      public static AttemptOutcome Failed(
        int attemptNumber,
        int seed,
        TimeSpan elapsed,
        long testedPositions,
        int backtrackings)
      {
        return new AttemptOutcome(
          attemptNumber,
          seed,
          null,
          elapsed,
          testedPositions,
          backtrackings,
          null);
      }

      public static AttemptOutcome Faulted(
        int attemptNumber,
        int seed,
        TimeSpan elapsed,
        long testedPositions,
        int backtrackings,
        Exception error)
      {
        return new AttemptOutcome(
          attemptNumber,
          seed,
          null,
          elapsed,
          testedPositions,
          backtrackings,
          error);
      }

      public static AttemptOutcome Rejected(
        int attemptNumber,
        int seed,
        TimeSpan elapsed,
        long testedPositions,
        int backtrackings)
      {
        return Failed(
          attemptNumber,
          seed,
          elapsed,
          testedPositions,
          backtrackings);
      }

      public static AttemptOutcome Succeeded(
        int attemptNumber,
        int seed,
        Board board,
        TimeSpan elapsed,
        long testedPositions,
        int backtrackings)
      {
        return new AttemptOutcome(
          attemptNumber,
          seed,
          board,
          elapsed,
          testedPositions,
          backtrackings,
          null);
      }
    }

    private sealed class AttemptState
    {
      public AttemptCompletion Completion
      {
        get;
        set;
      }

      public ConstructionProgress? Progress
      {
        get;
        set;
      }

      public long TestedPositions
      {
        get;
        set;
      }

      public int Backtrackings
      {
        get;
        set;
      }
    }

    private sealed class DelegateProgress<T>(Action<T> report) : IProgress<T>
    {
      public void Report(T value)
      {
        report(value);
      }
    }

    private sealed class ProgressAggregator
    {
      private static readonly long ReportIntervalTicks =
        Stopwatch.Frequency / 10;

      private readonly object _gate = new();
      private readonly IProgress<MonteCarloProgress>? _progress;
      private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
      private readonly AttemptState[] _states;
      private readonly int _totalWordCount;
      private long _nextReportAt;

      public ProgressAggregator(
        int attemptCount,
        int totalWordCount,
        IProgress<MonteCarloProgress>? progress)
      {
        _states = Enumerable
          .Range(0, attemptCount)
          .Select(_ => new AttemptState())
          .ToArray();
        _totalWordCount = totalWordCount;
        _progress = progress;
      }

      public void Complete(
        int attemptIndex,
        AttemptCompletion completion,
        long testedPositions,
        int backtrackings)
      {
        lock (_gate)
        {
          var state = _states[attemptIndex];
          state.Completion = completion;
          state.TestedPositions = Math.Max(
            state.TestedPositions,
            testedPositions);
          state.Backtrackings = Math.Max(
            state.Backtrackings,
            backtrackings);
        }

        Report();
      }

      public MonteCarloProgress Report(bool force = false)
      {
        MonteCarloProgress snapshot;
        var shouldReport = false;

        lock (_gate)
        {
          var now = Stopwatch.GetTimestamp();

          if (force || now >= _nextReportAt)
          {
            _nextReportAt = now + ReportIntervalTicks;
            shouldReport = true;
          }

          snapshot = CreateSnapshot();
        }

        if (shouldReport)
        {
          _progress?.Report(snapshot);
        }

        return snapshot;
      }

      public void Update(
        int attemptIndex,
        ConstructionProgress progress)
      {
        lock (_gate)
        {
          var state = _states[attemptIndex];
          state.Progress = progress;
          state.TestedPositions = Math.Max(
            state.TestedPositions,
            progress.TestedPositions);
          state.Backtrackings = Math.Max(
            state.Backtrackings,
            progress.Backtrackings);
        }

        Report();
      }

      private MonteCarloProgress CreateSnapshot()
      {
        var activeAttemptCount = _states.Count(
          state => state.Completion == AttemptCompletion.Running);
        var finishedAttemptCount = _states.Length - activeAttemptCount;
        var placementFailureCount = _states.Count(
          state => state.Completion == AttemptCompletion.PlacementFailed);
        var messageRejectedAttemptCount = _states.Count(
          state => state.Completion == AttemptCompletion.MessageRejected);
        var cancelledAttemptCount = _states.Count(
          state => state.Completion == AttemptCompletion.Cancelled);
        var placedWordCount = _states
          .Where(state => state.Completion == AttemptCompletion.Running)
          .Select(state => state.Progress?.PlacedWordCount ?? 0)
          .DefaultIfEmpty()
          .Max();
        var furthestPlacedWordCount = _states
          .Select(state => state.Progress?.FurthestPlacedWordCount ?? 0)
          .DefaultIfEmpty()
          .Max();
        var testedPositions = _states.Sum(state => state.TestedPositions);
        var backtrackings = _states.Sum(
          state => (long)state.Backtrackings);

        return new MonteCarloProgress(
          activeAttemptCount,
          finishedAttemptCount,
          placementFailureCount,
          messageRejectedAttemptCount,
          cancelledAttemptCount,
          _states.Length,
          placedWordCount,
          furthestPlacedWordCount,
          _totalWordCount,
          testedPositions,
          backtrackings,
          _stopwatch.Elapsed);
      }
    }

    #endregion
  }
}
