using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WordSearchGenerator.Common.WoSeCon;
using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Services;

namespace WordSearchGenerator.Benchmarks
{
  internal static class Program
  {
    #region Static Fields

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
      Converters =
      {
        new JsonStringEnumConverter()
      },
      PropertyNameCaseInsensitive = true,
      WriteIndented = true
    };

    #endregion

    #region Other Stuff

    private static async Task<int> Main(string[] arguments)
    {
      Console.OutputEncoding = Encoding.UTF8;

      if (arguments.Length is < 1 or > 2)
      {
        Console.Error.WriteLine(
          "Usage: WordSearchGenerator.Benchmarks <manifest.json> [output-directory]");
        return 2;
      }

      var manifestPath = Path.GetFullPath(arguments[0]);
      var outputDirectory = Path.GetFullPath(
        arguments.Length == 2
          ? arguments[1]
          : Path.Combine("artifacts", "benchmarks"));
      var manifest = await LoadManifestAsync(manifestPath);

      ValidateManifest(manifest);
      Directory.CreateDirectory(outputDirectory);

      var runId = $"{manifest.OutputName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
      var jsonPath = Path.Combine(outputDirectory, $"{runId}.json");
      var csvPath = Path.Combine(outputDirectory, $"{runId}.csv");
      var results = new List<BenchmarkResult>();

      Console.WriteLine($"Manifest: {manifestPath}");
      Console.WriteLine($"Cases: {manifest.Cases.Count}");
      Console.WriteLine($"Output: {outputDirectory}");

      foreach (var benchmarkCase in manifest.Cases)
      {
        for (var repetition = 1;
             repetition <= benchmarkCase.Repetitions;
             repetition++)
        {
          Console.WriteLine(
            $"[{benchmarkCase.Id}] repetition {repetition}/{benchmarkCase.Repetitions}");
          var result = await RunAsync(benchmarkCase, repetition);

          results.Add(result);
          await WriteResultsAsync(results, jsonPath, csvPath);

          Console.WriteLine(
            $"  {result.Status}, {result.WallTimeMilliseconds:N0} ms, " +
            $"estimate {result.Estimate}, tested {result.TestedPositions:N0}, " +
            $"backtracks {result.Backtrackings:N0}");
        }
      }

      Console.WriteLine($"JSON: {jsonPath}");
      Console.WriteLine($"CSV:  {csvPath}");
      return 0;
    }

    private static int[] CreateSeeds(
      BenchmarkCase benchmarkCase,
      int repetition,
      int count)
    {
      var firstSeed = checked(
        benchmarkCase.BaseSeed +
        (repetition - 1) * 10_000);

      if (firstSeed <= 0 || (long)firstSeed + count > int.MaxValue)
      {
        throw new InvalidDataException(
          $"Case '{benchmarkCase.Id}' produces seeds outside the positive Int32 range.");
      }

      return Enumerable
        .Range(0, count)
        .Select(index => firstSeed + index)
        .ToArray();
    }

    private static string EscapeCsv(object? value)
    {
      var text = Convert.ToString(value, CultureInfo.InvariantCulture) ??
                 string.Empty;

      return text.IndexOfAny([',', '"', '\r', '\n']) >= 0
        ? $"\"{text.Replace("\"", "\"\"")}\""
        : text;
    }

    private static async Task<BenchmarkManifest> LoadManifestAsync(
      string path)
    {
      await using var stream = File.OpenRead(path);

      return await JsonSerializer.DeserializeAsync<BenchmarkManifest>(
               stream,
               JsonOptions) ??
             throw new InvalidDataException("The benchmark manifest is empty.");
    }

    private static async Task<BenchmarkResult> RunAsync(
      BenchmarkCase benchmarkCase,
      int repetition)
    {
      var entries = benchmarkCase.Words
        .Select((word, index) => new PuzzleEntry(
          word,
          benchmarkCase.Mode == PuzzleMode.Quiz
            ? $"Benchmark question {index + 1}"
            : null))
        .ToArray();
      var definition = new PuzzleDefinition(
        benchmarkCase.Mode,
        benchmarkCase.Rows,
        benchmarkCase.Columns,
        entries,
        benchmarkCase.SecretMessage,
        string.Empty,
        string.Empty,
        "editorial",
        new GenerationOptions(benchmarkCase.Parallelism, 0));
      var estimate = WoSeCon.EstimateDifficulty(
        definition.CreateWordInfos(),
        definition.Rows,
        definition.Columns,
        definition.QuizMode,
        definition.Generation.ParallelAttempts,
        definition.QuizMode ? 0 : definition.SecretMessage.Length);
      var seeds = CreateSeeds(
        benchmarkCase,
        repetition,
        benchmarkCase.Parallelism);
      var latestProgress = new ProgressCapture();
      var generator = new MonteCarloPuzzleGenerator(_ => seeds.ToArray());
      using var timeout = new CancellationTokenSource(
        TimeSpan.FromSeconds(benchmarkCase.TimeoutSeconds));
      var stopwatch = Stopwatch.StartNew();

      try
      {
        var generation = await generator.GenerateAsync(
          definition,
          latestProgress,
          timeout.Token);
        stopwatch.Stop();

        return BenchmarkResult.Create(
          benchmarkCase,
          repetition,
          seeds,
          estimate,
          "Succeeded",
          stopwatch.Elapsed,
          generation.Elapsed,
          generation.TestedPositions,
          generation.Backtrackings,
          generation.AmbiguousBoardRejectionCount,
          generation.PlacementFailedAttemptCount,
          generation.MessageCapacityRejectedAttemptCount,
          generation.CancelledAttemptCount,
          generation.WinningAttemptNumber,
          generation.WinningSeed,
          generation.PuzzleOccupancyPercentage,
          null);
      }
      catch (OperationCanceledException) when (timeout.IsCancellationRequested)
      {
        stopwatch.Stop();
        var progress = latestProgress.Latest;

        return BenchmarkResult.Create(
          benchmarkCase,
          repetition,
          seeds,
          estimate,
          "TimedOut",
          stopwatch.Elapsed,
          progress?.Elapsed,
          progress?.TestedPositions ?? 0,
          progress?.Backtrackings ?? 0,
          progress?.AmbiguousBoardRejectionCount ?? 0,
          progress?.PlacementFailedAttemptCount ?? 0,
          progress?.MessageCapacityRejectedAttemptCount ?? 0,
          progress?.CancelledAttemptCount ?? benchmarkCase.Parallelism,
          null,
          null,
          null,
          "Per-run timeout reached.");
      }
      catch (MonteCarloGenerationException exception)
      {
        stopwatch.Stop();
        var progress = latestProgress.Latest;

        return BenchmarkResult.Create(
          benchmarkCase,
          repetition,
          seeds,
          estimate,
          "NoSolution",
          stopwatch.Elapsed,
          progress?.Elapsed,
          progress?.TestedPositions ?? 0,
          progress?.Backtrackings ?? 0,
          exception.AmbiguousBoardRejectionCount,
          exception.PlacementFailedAttemptCount,
          exception.MessageCapacityRejectedAttemptCount,
          progress?.CancelledAttemptCount ?? 0,
          null,
          null,
          null,
          exception.Message);
      }
      catch (Exception exception)
      {
        stopwatch.Stop();
        var progress = latestProgress.Latest;

        return BenchmarkResult.Create(
          benchmarkCase,
          repetition,
          seeds,
          estimate,
          "Error",
          stopwatch.Elapsed,
          progress?.Elapsed,
          progress?.TestedPositions ?? 0,
          progress?.Backtrackings ?? 0,
          progress?.AmbiguousBoardRejectionCount ?? 0,
          progress?.PlacementFailedAttemptCount ?? 0,
          progress?.MessageCapacityRejectedAttemptCount ?? 0,
          progress?.CancelledAttemptCount ?? 0,
          null,
          null,
          null,
          exception.ToString());
      }
    }

    private static void ValidateManifest(BenchmarkManifest manifest)
    {
      if (manifest.SchemaVersion != 1)
      {
        throw new InvalidDataException(
          $"Unsupported manifest schema {manifest.SchemaVersion}.");
      }

      if (string.IsNullOrWhiteSpace(manifest.OutputName) ||
          manifest.Cases.Count == 0)
      {
        throw new InvalidDataException(
          "The manifest needs an output name and at least one case.");
      }

      var identifiers = new HashSet<string>(StringComparer.Ordinal);

      foreach (var benchmarkCase in manifest.Cases)
      {
        if (string.IsNullOrWhiteSpace(benchmarkCase.Id) ||
            !identifiers.Add(benchmarkCase.Id))
        {
          throw new InvalidDataException(
            "Every benchmark case needs a unique non-empty ID.");
        }

        if (benchmarkCase.Rows <= 0 ||
            benchmarkCase.Columns <= 0 ||
            benchmarkCase.Parallelism <= 0 ||
            benchmarkCase.Repetitions <= 0 ||
            benchmarkCase.TimeoutSeconds is <= 0 or > 180 ||
            benchmarkCase.BaseSeed <= 0 ||
            benchmarkCase.Words.Count == 0)
        {
          throw new InvalidDataException(
            $"Case '{benchmarkCase.Id}' has invalid dimensions, execution settings, or words.");
        }

        var minimumLength = benchmarkCase.Mode == PuzzleMode.Normal
          ? PuzzleInputParser.MinimumWordLength
          : 2;

        if (benchmarkCase.Words.Any(word =>
              string.IsNullOrWhiteSpace(word) ||
              word.Length < minimumLength))
        {
          throw new InvalidDataException(
            $"Case '{benchmarkCase.Id}' contains an invalid short or empty word.");
        }

        var entries = benchmarkCase.Words
          .Select(word => new PuzzleEntry(word))
          .ToArray();

        if (benchmarkCase.Mode == PuzzleMode.Normal &&
            WordContainmentValidator.TryFindConflict(
              entries,
              out var first,
              out var second))
        {
          throw new InvalidDataException(
            $"Case '{benchmarkCase.Id}' contains conflicting words " +
            $"'{first!.Answer}' and '{second!.Answer}'.");
        }
      }
    }

    private static async Task WriteResultsAsync(
      IReadOnlyList<BenchmarkResult> results,
      string jsonPath,
      string csvPath)
    {
      await File.WriteAllTextAsync(
        jsonPath,
        JsonSerializer.Serialize(results, JsonOptions),
        new UTF8Encoding(false));

      var properties = typeof(BenchmarkResult).GetProperties();
      var csv = new StringBuilder();

      csv.AppendLine(string.Join(',', properties.Select(property => property.Name)));

      foreach (var result in results)
      {
        csv.AppendLine(string.Join(",", properties.Select(property =>
          EscapeCsv(property.GetValue(result)))));
      }

      await File.WriteAllTextAsync(
        csvPath,
        csv.ToString(),
        new UTF8Encoding(false));
    }

    #endregion

    #region Nested Types

    private sealed class ProgressCapture : IProgress<MonteCarloProgress>
    {
      private readonly object _gate = new();
      private MonteCarloProgress? _latest;

      public MonteCarloProgress? Latest
      {
        get
        {
          lock (_gate)
          {
            return _latest;
          }
        }
      }

      public void Report(MonteCarloProgress value)
      {
        lock (_gate)
        {
          _latest = value;
        }
      }
    }

    #endregion
  }

  internal sealed class BenchmarkManifest
  {
    public int SchemaVersion
    {
      get;
      init;
    }

    public string OutputName
    {
      get;
      init;
    } = "benchmark";

    public List<BenchmarkCase> Cases
    {
      get;
      init;
    } = [];
  }

  internal sealed class BenchmarkCase
  {
    public string Id
    {
      get;
      init;
    } = string.Empty;

    public string Factor
    {
      get;
      init;
    } = string.Empty;

    public string Level
    {
      get;
      init;
    } = string.Empty;

    public PuzzleMode Mode
    {
      get;
      init;
    }

    public int Rows
    {
      get;
      init;
    }

    public int Columns
    {
      get;
      init;
    }

    public List<string> Words
    {
      get;
      init;
    } = [];

    public string SecretMessage
    {
      get;
      init;
    } = string.Empty;

    public int Parallelism
    {
      get;
      init;
    } = 1;

    public int Repetitions
    {
      get;
      init;
    } = 1;

    public int TimeoutSeconds
    {
      get;
      init;
    } = 180;

    public int BaseSeed
    {
      get;
      init;
    } = 1;

    public string Notes
    {
      get;
      init;
    } = string.Empty;
  }

  internal sealed class BenchmarkResult
  {
    public DateTimeOffset RecordedAtUtc { get; init; }
    public string MachineName { get; init; } = string.Empty;
    public int LogicalProcessorCount { get; init; }
    public string OperatingSystem { get; init; } = string.Empty;
    public string Runtime { get; init; } = string.Empty;
    public string ProcessArchitecture { get; init; } = string.Empty;
    public string BuildVersion { get; init; } = string.Empty;
    public string CaseId { get; init; } = string.Empty;
    public string Factor { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public int Repetition { get; init; }
    public PuzzleMode Mode { get; init; }
    public int Rows { get; init; }
    public int Columns { get; init; }
    public int CellCount { get; init; }
    public int WordCount { get; init; }
    public long TotalWordCharacters { get; init; }
    public double AverageWordLength { get; init; }
    public int MinimumWordLength { get; init; }
    public int MaximumWordLength { get; init; }
    public int DistinctCharacterCount { get; init; }
    public int SecretMessageLength { get; init; }
    public int RequiredVacantCellCount { get; init; }
    public double RawPackingRatio { get; init; }
    public double EffectivePackingRatio { get; init; }
    public long RequiredIntersectionLowerBound { get; init; }
    public long CompatibleWordPairCount { get; init; }
    public int Parallelism { get; init; }
    public int TimeoutSeconds { get; init; }
    public string Seeds { get; init; } = string.Empty;
    public WoSeCon.EstimatedConstructionTime Estimate { get; init; }
    public string Status { get; init; } = string.Empty;
    public double WallTimeMilliseconds { get; init; }
    public double? GeneratorElapsedMilliseconds { get; init; }
    public long TestedPositions { get; init; }
    public long Backtrackings { get; init; }
    public long AmbiguousBoardsRejected { get; init; }
    public int PlacementFailures { get; init; }
    public int MessageRejections { get; init; }
    public int CancelledAttempts { get; init; }
    public int? WinningAttempt { get; init; }
    public int? WinningSeed { get; init; }
    public double? OccupancyPercentage { get; init; }
    public string Notes { get; init; } = string.Empty;
    public string? Error { get; init; }

    public static BenchmarkResult Create(
      BenchmarkCase benchmarkCase,
      int repetition,
      IReadOnlyList<int> seeds,
      WoSeCon.EstimatedConstructionTime estimate,
      string status,
      TimeSpan wallTime,
      TimeSpan? generatorElapsed,
      long testedPositions,
      long backtrackings,
      long ambiguousBoardsRejected,
      int placementFailures,
      int messageRejections,
      int cancelledAttempts,
      int? winningAttempt,
      int? winningSeed,
      double? occupancyPercentage,
      string? error)
    {
      var cellCount = checked(benchmarkCase.Rows * benchmarkCase.Columns);
      var answerCharacterCount = benchmarkCase.Words.Sum(
        word => (long)word.Length);
      var questionCellCount = benchmarkCase.Mode == PuzzleMode.Quiz
        ? benchmarkCase.Words.Count
        : 0;
      var requiredVacantCellCount = benchmarkCase.Mode == PuzzleMode.Normal
        ? benchmarkCase.SecretMessage.Length
        : 0;
      var availableWordCells = Math.Max(
        1L,
        (long)cellCount - requiredVacantCellCount);

      return new BenchmarkResult
      {
        RecordedAtUtc = DateTimeOffset.UtcNow,
        MachineName = Environment.MachineName,
        LogicalProcessorCount = Environment.ProcessorCount,
        OperatingSystem = RuntimeInformation.OSDescription,
        Runtime = RuntimeInformation.FrameworkDescription,
        ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
        BuildVersion = Assembly
                         .GetExecutingAssembly()
                         .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                         .InformationalVersion ?? string.Empty,
        CaseId = benchmarkCase.Id,
        Factor = benchmarkCase.Factor,
        Level = benchmarkCase.Level,
        Repetition = repetition,
        Mode = benchmarkCase.Mode,
        Rows = benchmarkCase.Rows,
        Columns = benchmarkCase.Columns,
        CellCount = cellCount,
        WordCount = benchmarkCase.Words.Count,
        TotalWordCharacters = answerCharacterCount,
        AverageWordLength = benchmarkCase.Words.Average(word => word.Length),
        MinimumWordLength = benchmarkCase.Words.Min(word => word.Length),
        MaximumWordLength = benchmarkCase.Words.Max(word => word.Length),
        DistinctCharacterCount = benchmarkCase.Words
          .SelectMany(word => word)
          .Distinct()
          .Count(),
        SecretMessageLength = benchmarkCase.SecretMessage.Length,
        RequiredVacantCellCount = requiredVacantCellCount,
        RawPackingRatio = (answerCharacterCount + questionCellCount) /
                          (double)cellCount,
        EffectivePackingRatio = (answerCharacterCount + questionCellCount) /
                                (double)availableWordCells,
        RequiredIntersectionLowerBound = Math.Max(
          0,
          answerCharacterCount + questionCellCount - availableWordCells),
        CompatibleWordPairCount = CountCompatibleWordPairs(
          benchmarkCase.Words),
        Parallelism = benchmarkCase.Parallelism,
        TimeoutSeconds = benchmarkCase.TimeoutSeconds,
        Seeds = string.Join(';', seeds),
        Estimate = estimate,
        Status = status,
        WallTimeMilliseconds = wallTime.TotalMilliseconds,
        GeneratorElapsedMilliseconds = generatorElapsed?.TotalMilliseconds,
        TestedPositions = testedPositions,
        Backtrackings = backtrackings,
        AmbiguousBoardsRejected = ambiguousBoardsRejected,
        PlacementFailures = placementFailures,
        MessageRejections = messageRejections,
        CancelledAttempts = cancelledAttempts,
        WinningAttempt = winningAttempt,
        WinningSeed = winningSeed,
        OccupancyPercentage = occupancyPercentage,
        Notes = benchmarkCase.Notes,
        Error = error
      };
    }

    private static long CountCompatibleWordPairs(
      IReadOnlyList<string> words)
    {
      var characterSets = words
        .Select(word => word.ToHashSet())
        .ToArray();
      var count = 0L;

      for (var first = 0; first < characterSets.Length - 1; first++)
      for (var second = first + 1;
           second < characterSets.Length;
           second++)
      {
        if (characterSets[first].Overlaps(characterSets[second]))
        {
          count++;
        }
      }

      return count;
    }
  }
}
