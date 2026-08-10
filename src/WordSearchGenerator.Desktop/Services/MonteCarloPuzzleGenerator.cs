using System.Diagnostics;
using System.Security.Cryptography;
using Wose.Common;
using Wose.Common.WoSeCon;
using Wose.Common.WoSeCon.Api;
using Wose.Desktop.Localization;
using Wose.Desktop.Models;

namespace Wose.Desktop.Services
{
  public sealed class MonteCarloPuzzleGenerator : IPuzzleGenerator
  {
    #region Fields

    private readonly Func<TimeSpan, CancellationTokenSource>
      _attemptTimeoutFactory;
    private readonly Func<int, int[]> _seedFactory;

    #endregion

    #region Constructors

    public MonteCarloPuzzleGenerator(Func<int, int[]>? seedFactory = null)
      : this(
        seedFactory,
        timeout => new CancellationTokenSource(timeout))
    {
    }

    internal MonteCarloPuzzleGenerator(
      Func<int, int[]>? seedFactory,
      Func<TimeSpan, CancellationTokenSource> attemptTimeoutFactory)
    {
      _seedFactory = seedFactory ?? CreateSeeds;
      _attemptTimeoutFactory = attemptTimeoutFactory ??
                               throw new ArgumentNullException(
                                 nameof(attemptTimeoutFactory));
    }

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
      var seeds = _seedFactory(attemptCount);

      ValidateSeeds(seeds, attemptCount);

      using var linkedCancellation =
        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      var seedPool = new SeedPool(_seedFactory, seeds);
      var aggregator = new ProgressAggregator(
        attemptCount,
        definition.Entries.Count,
        progress);
      var remainingTasks = Enumerable
        .Range(0, attemptCount)
        .Select(index => Task.Run(
          () => RunWorker(
            definition,
            index,
            seeds[index],
            seedPool,
            aggregator,
            _attemptTimeoutFactory,
            linkedCancellation.Token),
          CancellationToken.None))
        .ToList();

      aggregator.Report(true);
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

          aggregator.Report(true);
          cancellationToken.ThrowIfCancellationRequested();

          throw new InvalidOperationException(AppStrings.Format(
              "GenerationAttemptUnexpected",
              outcome.AttemptNumber),
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

      var finalProgress = aggregator.Report(true);
      cancellationToken.ThrowIfCancellationRequested();

      if (winner == null)
      {
        throw new MonteCarloGenerationException(
          attemptCount,
          finalProgress.PlacementFailedAttemptCount,
          finalProgress.MessageCapacityRejectedAttemptCount,
          finalProgress.AmbiguityRejectedAttemptCount,
          finalProgress.CompletedCandidateCount,
          finalProgress.MessageCapacityRejectionCount,
          finalProgress.AmbiguousBoardRejectionCount);
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
        finalProgress.PlacementFailedAttemptCount,
        finalProgress.MessageCapacityRejectedAttemptCount,
        finalProgress.AmbiguityRejectedAttemptCount,
        finalProgress.CompletedCandidateCount,
        finalProgress.MessageCapacityRejectionCount,
        finalProgress.AmbiguousBoardRejectionCount,
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

    private static void ValidateSeeds(int[]? seeds, int expectedCount)
    {
      if (seeds == null ||
          seeds.Length != expectedCount ||
          seeds.Any(seed => seed <= 0) ||
          seeds.Distinct().Count() != seeds.Length)
      {
        throw new InvalidOperationException(
          "The seed factory must return the requested number of distinct positive seeds.");
      }
    }

    private static AttemptOutcome RunWorker(
      PuzzleDefinition definition,
      int attemptIndex,
      int initialSeed,
      SeedPool seedPool,
      ProgressAggregator aggregator,
      Func<TimeSpan, CancellationTokenSource> attemptTimeoutFactory,
      CancellationToken cancellationToken)
    {
      var seed = initialSeed;

      while (true)
      {
        var maximumAttemptTimeSeconds =
          definition.Generation.MaximumAttemptTimeSeconds;
        using var timeoutCancellation = maximumAttemptTimeSeconds > 0
          ? attemptTimeoutFactory(TimeSpan.FromSeconds(
              maximumAttemptTimeSeconds)) ??
            throw new InvalidOperationException(
              "The attempt timeout factory returned null.")
          : null;
        using var attemptCancellation = timeoutCancellation == null
          ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
          : CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCancellation.Token);

        var outcome = RunAttempt(
          definition,
          attemptIndex,
          seed,
          aggregator,
          attemptCancellation.Token);

        if (!outcome.WasCancelled)
        {
          return outcome;
        }

        if (cancellationToken.IsCancellationRequested ||
            maximumAttemptTimeSeconds == 0)
        {
          aggregator.Complete(
            attemptIndex,
            AttemptCompletion.Cancelled,
            outcome.TestedPositions,
            outcome.Backtrackings);
          return outcome;
        }

        aggregator.Restart(
          attemptIndex,
          outcome.TestedPositions,
          outcome.Backtrackings);

        try
        {
          seed = seedPool.Next();
        }
        catch (Exception exception)
        {
          aggregator.Complete(
            attemptIndex,
            AttemptCompletion.Faulted,
            0,
            0);
          return AttemptOutcome.Faulted(
            attemptIndex + 1,
            seed,
            TimeSpan.Zero,
            0,
            0,
            exception);
        }
      }
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
      var completedLayoutMessageRejected = false;
      var completedLayoutAmbiguityRejected = false;

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        generator = new WoSeCon(
          definition.CreateWordInfos(),
          definition.Rows,
          definition.Columns,
          definition.QuizMode,
          locations => ShuffleLocations(locations, seed));
        var attemptProgress =
          new DelegateProgress<ConstructionProgress>(value => aggregator.Update(attemptIndex, value));
        Func<IReadOnlyList<WordInfo>, bool>? completionValidator = null;

        if (!definition.QuizMode)
        {
          completionValidator = placedWords =>
          {
            Board board;

            try
            {
              board = new Board(
                placedWords.ToList(),
                definition,
                definition.SecretMessage);
            }
            catch (MessageCannotBePlacedException)
            {
              completedLayoutMessageRejected = true;
              aggregator.RecordMessageCapacityRejection();
              return false;
            }

            if (board.HasUniqueWordOccurrences())
            {
              aggregator.RecordAcceptedCandidate();
              return true;
            }

            completedLayoutAmbiguityRejected = true;
            aggregator.RecordAmbiguousBoardRejection();
            return false;
          };
        }
        else
        {
          completionValidator = placedWords =>
          {
            try
            {
              _ = new Board(
                placedWords.ToList(),
                definition,
                definition.SecretMessage);
              aggregator.RecordAcceptedCandidate();
              return true;
            }
            catch (MessageCannotBePlacedException)
            {
              completedLayoutMessageRejected = true;
              aggregator.RecordMessageCapacityRejection();
              return false;
            }
          };
        }

        generator.Construct(
          attemptProgress,
          cancellationToken,
          completionValidator);
        cancellationToken.ThrowIfCancellationRequested();

        var board = new Board(
          generator.Words,
          definition,
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
        return AttemptOutcome.Cancelled(
          attemptIndex + 1,
          seed,
          stopwatch.Elapsed,
          generator?.TestedPositions ?? 0,
          generator?.Backtrackings ?? 0);
      }
      catch (ConstructionExhaustedException)
      {
        stopwatch.Stop();
        var completion = completedLayoutAmbiguityRejected
          ? AttemptCompletion.AmbiguityRejected
          : completedLayoutMessageRejected
            ? AttemptCompletion.MessageRejected
            : AttemptCompletion.PlacementFailed;
        aggregator.Complete(
          attemptIndex,
          completion,
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

    #endregion

    #region Nested Types

    internal enum AttemptCompletion
    {
      Running,
      Succeeded,
      PlacementFailed,
      MessageRejected,
      AmbiguityRejected,
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
      bool WasCancelled,
      Exception? Error)
    {
      #region Other Stuff

      public static AttemptOutcome Cancelled(
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
          true,
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
          false,
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
          false,
          error);
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
          false,
          null);
      }

      #endregion
    }

    private sealed class SeedPool
    {
      #region Fields

      private readonly Func<int, int[]> _seedFactory;
      private readonly object _gate = new();
      private readonly HashSet<int> _usedSeeds;

      #endregion

      #region Constructors

      public SeedPool(
        Func<int, int[]> seedFactory,
        IEnumerable<int> initialSeeds)
      {
        _seedFactory = seedFactory;
        _usedSeeds = initialSeeds.ToHashSet();
      }

      #endregion

      #region Other Stuff

      public int Next()
      {
        lock (_gate)
        {
          for (var attempt = 0; attempt < 1024; attempt++)
          {
            var seeds = _seedFactory(1);
            ValidateSeeds(seeds, 1);

            if (_usedSeeds.Add(seeds[0]))
            {
              return seeds[0];
            }
          }
        }

        throw new InvalidOperationException(
          "The seed factory repeatedly returned seeds that were already used.");
      }

      #endregion
    }

    private sealed class AttemptState
    {
      #region Properties

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

      public int FurthestPlacedWordCount
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

      #endregion
    }

    private sealed class DelegateProgress<T>(Action<T> report) : IProgress<T>
    {
      #region Interface Implementations

      public void Report(T value)
      {
        report(value);
      }

      #endregion
    }

    internal sealed class ProgressAggregator
    {
      #region Static Fields

      private static readonly long ReportIntervalTicks =
        Stopwatch.Frequency / 10;

      #endregion

      #region Fields

      private readonly object _gate = new();
      private readonly IProgress<MonteCarloProgress>? _progress;
      private readonly AttemptState[] _states;
      private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
      private readonly int _totalWordCount;
      private long _ambiguousBoardRejectionCount;
      private long _completedCandidateCount;
      private long _messageCapacityRejectionCount;
      private long _nextReportAt;
      private long _restartedAttemptBacktrackings;
      private long _restartedAttemptTestedPositions;

      #endregion

      #region Constructors

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

      #endregion

      #region Other Stuff

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

      public void RecordAcceptedCandidate()
      {
        lock (_gate)
        {
          _completedCandidateCount++;
        }

        Report();
      }

      public void RecordAmbiguousBoardRejection()
      {
        lock (_gate)
        {
          _completedCandidateCount++;
          _ambiguousBoardRejectionCount++;
        }

        Report();
      }

      public void RecordMessageCapacityRejection()
      {
        lock (_gate)
        {
          _completedCandidateCount++;
          _messageCapacityRejectionCount++;
        }

        Report();
      }

      public void Restart(
        int attemptIndex,
        long testedPositions,
        int backtrackings)
      {
        lock (_gate)
        {
          var state = _states[attemptIndex];
          _restartedAttemptTestedPositions += Math.Max(
            state.TestedPositions,
            testedPositions);
          _restartedAttemptBacktrackings += Math.Max(
            state.Backtrackings,
            backtrackings);
          state.Completion = AttemptCompletion.Running;
          state.Progress = null;
          state.TestedPositions = 0;
          state.Backtrackings = 0;
        }

        Report();
      }

      public void Update(
        int attemptIndex,
        ConstructionProgress progress)
      {
        lock (_gate)
        {
          var state = _states[attemptIndex];
          state.Progress = progress;
          state.FurthestPlacedWordCount = Math.Max(
            state.FurthestPlacedWordCount,
            progress.FurthestPlacedWordCount);
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
        var activeAttemptCount = _states.Count(state => state.Completion == AttemptCompletion.Running);
        var finishedAttemptCount = _states.Length - activeAttemptCount;
        var placementFailedAttemptCount =
          _states.Count(state => state.Completion == AttemptCompletion.PlacementFailed);
        var messageCapacityRejectedAttemptCount =
          _states.Count(state => state.Completion == AttemptCompletion.MessageRejected);
        var ambiguityRejectedAttemptCount =
          _states.Count(state => state.Completion == AttemptCompletion.AmbiguityRejected);
        var cancelledAttemptCount = _states.Count(state => state.Completion == AttemptCompletion.Cancelled);
        var placedWordCount = _states
          .Where(state => state.Completion == AttemptCompletion.Running)
          .Select(state => state.Progress?.PlacedWordCount ?? 0)
          .DefaultIfEmpty()
          .Max();
        var furthestPlacedWordCount = _states
          .Select(state => state.FurthestPlacedWordCount)
          .DefaultIfEmpty()
          .Max();
        var testedPositions = _restartedAttemptTestedPositions +
                              _states.Sum(state => state.TestedPositions);
        var backtrackings = _restartedAttemptBacktrackings +
                            _states.Sum(state => (long)state.Backtrackings);

        return new MonteCarloProgress(
          activeAttemptCount,
          finishedAttemptCount,
          placementFailedAttemptCount,
          messageCapacityRejectedAttemptCount,
          ambiguityRejectedAttemptCount,
          _completedCandidateCount,
          _messageCapacityRejectionCount,
          _ambiguousBoardRejectionCount,
          cancelledAttemptCount,
          _states.Length,
          placedWordCount,
          furthestPlacedWordCount,
          _totalWordCount,
          testedPositions,
          backtrackings,
          _stopwatch.Elapsed);
      }

      #endregion
    }

    #endregion
  }
}
