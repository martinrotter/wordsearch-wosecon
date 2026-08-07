using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WordSearchGenerator.Common;
using WordSearchGenerator.Common.WoSeCon.Api;
using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Models.Persistence;

namespace WordSearchGenerator.Desktop.Services.Persistence
{
  public sealed class PuzzleProjectSerializer : IPuzzleProjectSerializer
  {
    #region Constants

    private const int CurrentFormatVersion = 1;

    #endregion

    #region Static Fields

    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    #endregion

    #region Interface Implementations

    public async Task<PuzzleProject> LoadAsync(
      string path,
      CancellationToken cancellationToken = default)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(path);

      try
      {
        await using var stream = new FileStream(
          path,
          FileMode.Open,
          FileAccess.Read,
          FileShare.Read,
          64 * 1024,
          FileOptions.Asynchronous | FileOptions.SequentialScan);
        var file = await JsonSerializer.DeserializeAsync<ProjectFile>(
          stream,
          JsonOptions,
          cancellationToken) ??
                   throw new InvalidDataException("The project file is empty.");

        return RestoreProject(file);
      }
      catch (JsonException exception)
      {
        throw new InvalidDataException(
          "The project file does not contain valid WoSeCon JSON.",
          exception);
      }
    }

    public async Task SaveAsync(
      string path,
      PuzzleDefinition definition,
      GenerationResult? generatedResult,
      CancellationToken cancellationToken = default)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(path);
      ArgumentNullException.ThrowIfNull(definition);

      var fullPath = Path.GetFullPath(path);
      var directory = Path.GetDirectoryName(fullPath) ??
                      throw new InvalidOperationException(
                        "The project path has no parent directory.");

      if (!Directory.Exists(directory))
      {
        throw new DirectoryNotFoundException(
          $"The directory '{directory}' does not exist.");
      }

      var file = CreateFile(definition, generatedResult);
      var temporaryPath = Path.Combine(
        directory,
        $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

      try
      {
        await using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       64 * 1024,
                       FileOptions.Asynchronous | FileOptions.WriteThrough))
        {
          await JsonSerializer.SerializeAsync(
            stream,
            file,
            JsonOptions,
            cancellationToken);
          await stream.FlushAsync(cancellationToken);
        }

        File.Move(temporaryPath, fullPath, overwrite: true);
      }
      finally
      {
        if (File.Exists(temporaryPath))
        {
          File.Delete(temporaryPath);
        }
      }
    }

    #endregion

    #region Other Stuff

    private static ProjectFile CreateFile(
      PuzzleDefinition definition,
      GenerationResult? result)
    {
      var generated = result == null
        ? null
        : new GeneratedBoardFile
        {
          AttemptCount = result.AttemptCount,
          Backtrackings = result.Backtrackings,
          CancelledAttemptCount = result.CancelledAttemptCount,
          ElapsedTicks = result.Elapsed.Ticks,
          MessageRejectedAttemptCount = result.MessageRejectedAttemptCount,
          PlacementFailureCount = result.PlacementFailureCount,
          Placements = result.Board.Words
            .Select(word => word.Placement == null
              ? throw new InvalidOperationException(
                $"Generated word {word.WordNumber} has no placement.")
              : new PlacementFile
              {
                Column = word.Placement.Column,
                Direction = word.Placement.Direction,
                Row = word.Placement.Row,
                WordNumber = word.WordNumber
              })
            .OrderBy(placement => placement.WordNumber)
            .ToList(),
          TestedPositions = result.TestedPositions,
          WinningAttemptBacktrackings = result.WinningAttemptBacktrackings,
          WinningAttemptElapsedTicks = result.WinningAttemptElapsed.Ticks,
          WinningAttemptNumber = result.WinningAttemptNumber,
          WinningAttemptTestedPositions = result.WinningAttemptTestedPositions,
          WinningSeed = result.WinningSeed
        };

      return new ProjectFile
      {
        Columns = definition.Columns,
        Entries = definition.Entries
          .Select(entry => new EntryFile
          {
            Answer = entry.Answer,
            Question = entry.Question
          })
          .ToList(),
        EntryListHeading = definition.EntryListHeading,
        FormatVersion = CurrentFormatVersion,
        GeneratedBoard = generated,
        Mode = definition.Mode,
        ParallelAttempts = definition.Generation.ParallelAttempts,
        PuzzleHeading = definition.PuzzleHeading,
        Rows = definition.Rows,
        SecretMessage = definition.SecretMessage
      };
    }

    private static JsonSerializerOptions CreateOptions()
    {
      var options = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
      };
      options.Converters.Add(new JsonStringEnumConverter());
      return options;
    }

    private static GenerationResult RestoreGeneratedResult(
      PuzzleDefinition definition,
      GeneratedBoardFile generated)
    {
      if (generated.Placements == null ||
          generated.Placements.Count != definition.Entries.Count)
      {
        throw new InvalidDataException(
          "The saved board does not contain one placement for every entry.");
      }

      ValidateNonNegative(generated.AttemptCount, nameof(generated.AttemptCount));
      ValidateNonNegative(generated.Backtrackings, nameof(generated.Backtrackings));
      ValidateNonNegative(
        generated.CancelledAttemptCount,
        nameof(generated.CancelledAttemptCount));
      ValidateNonNegative(generated.ElapsedTicks, nameof(generated.ElapsedTicks));
      ValidateNonNegative(
        generated.MessageRejectedAttemptCount,
        nameof(generated.MessageRejectedAttemptCount));
      ValidateNonNegative(
        generated.PlacementFailureCount,
        nameof(generated.PlacementFailureCount));
      ValidateNonNegative(
        generated.TestedPositions,
        nameof(generated.TestedPositions));
      ValidateNonNegative(
        generated.WinningAttemptBacktrackings,
        nameof(generated.WinningAttemptBacktrackings));
      ValidateNonNegative(
        generated.WinningAttemptElapsedTicks,
        nameof(generated.WinningAttemptElapsedTicks));
      ValidateNonNegative(
        generated.WinningAttemptTestedPositions,
        nameof(generated.WinningAttemptTestedPositions));

      if (generated.AttemptCount == 0 ||
          generated.WinningAttemptNumber <= 0 ||
          generated.WinningAttemptNumber > generated.AttemptCount)
      {
        throw new InvalidDataException(
          "The saved generation attempt numbers are invalid.");
      }

      var placementsByNumber = generated.Placements
        .GroupBy(placement => placement.WordNumber)
        .ToDictionary(group => group.Key, group => group.ToList());
      var words = definition.CreateWordInfos();
      var placedWords = new List<WordInfo>(words.Count);

      foreach (var word in words)
      {
        if (!placementsByNumber.TryGetValue(word.WordNumber, out var matches) ||
            matches.Count != 1)
        {
          throw new InvalidDataException(
            $"Word {word.WordNumber} has a missing or duplicate placement.");
        }

        var placement = matches[0];

        if (!Enum.IsDefined(placement.Direction))
        {
          throw new InvalidDataException(
            $"Word {word.WordNumber} has an invalid direction.");
        }

        word.Placement = new DirectedLocation
        {
          Column = placement.Column,
          Direction = placement.Direction,
          Row = placement.Row
        };

        if (!word.WillFit(
              word.Placement,
              definition.Rows,
              definition.Columns,
              definition.QuizMode))
        {
          throw new InvalidDataException(
            $"Word {word.WordNumber} is placed outside the matrix.");
        }

        var locations = word.GetAllPlacementLocations(definition.QuizMode);

        if (placedWords.Any(previous => word.ConflictsWithWord(
              locations,
              previous,
              definition.QuizMode)))
        {
          throw new InvalidDataException(
            $"Word {word.WordNumber} conflicts with another saved placement.");
        }

        placedWords.Add(word);
      }

      Board board;

      try
      {
        board = new Board(
          words,
          definition.Rows,
          definition.Columns,
          definition.QuizMode,
          definition.SecretMessage);
      }
      catch (Exception exception)
      {
        throw new InvalidDataException(
          "The saved placements cannot reconstruct the board.",
          exception);
      }

      return new GenerationResult(
        definition,
        board,
        TimeSpan.FromTicks(generated.ElapsedTicks),
        generated.TestedPositions,
        generated.Backtrackings,
        generated.AttemptCount,
        generated.WinningAttemptNumber,
        generated.WinningSeed,
        TimeSpan.FromTicks(generated.WinningAttemptElapsedTicks),
        generated.WinningAttemptTestedPositions,
        generated.WinningAttemptBacktrackings,
        generated.PlacementFailureCount,
        generated.MessageRejectedAttemptCount,
        generated.CancelledAttemptCount);
    }

    private static PuzzleProject RestoreProject(ProjectFile file)
    {
      if (file.FormatVersion != CurrentFormatVersion)
      {
        throw new InvalidDataException(
          $"Unsupported project format version {file.FormatVersion}. " +
          $"This application supports version {CurrentFormatVersion}.");
      }

      if (!Enum.IsDefined(file.Mode))
      {
        throw new InvalidDataException("The project mode is invalid.");
      }

      if (file.Entries == null || file.Entries.Count == 0)
      {
        throw new InvalidDataException(
          "The project must contain at least one word or quiz entry.");
      }

      var rawEntries = file.Entries.Select(entry => new PuzzleEntry(
        entry.Answer ?? string.Empty,
        entry.Question));
      var entries = file.Mode == PuzzleMode.Normal
        ? PuzzleInputParser.ParseWords(
          string.Join(Environment.NewLine, rawEntries.Select(entry => entry.Answer)))
        : PuzzleInputParser.ParseQuizEntries(rawEntries);

      if (entries.Count != file.Entries.Count ||
          entries.Any(entry => entry.Answer.Length < 2) ||
          file.Mode == PuzzleMode.Quiz &&
          entries.Any(entry => string.IsNullOrWhiteSpace(entry.Question)))
      {
        throw new InvalidDataException(
          "The project contains blank, duplicate, or one-character entries.");
      }

      PuzzleDefinition definition;

      try
      {
        definition = new PuzzleDefinition(
          file.Mode,
          file.Rows,
          file.Columns,
          entries,
          file.SecretMessage ?? string.Empty,
          file.PuzzleHeading ?? string.Empty,
          file.EntryListHeading ?? string.Empty,
          new GenerationOptions(file.ParallelAttempts));
      }
      catch (Exception exception)
        when (exception is ArgumentException)
      {
        throw new InvalidDataException(
          "The project settings are invalid.",
          exception);
      }

      var result = file.GeneratedBoard == null
        ? null
        : RestoreGeneratedResult(definition, file.GeneratedBoard);

      return new PuzzleProject(definition, result);
    }

    private static void ValidateNonNegative(long value, string name)
    {
      if (value < 0)
      {
        throw new InvalidDataException($"Saved statistic '{name}' is negative.");
      }
    }

    #endregion

    #region Nested Types

    private sealed class EntryFile
    {
      public string? Answer
      {
        get;
        set;
      }

      public string? Question
      {
        get;
        set;
      }
    }

    private sealed class GeneratedBoardFile
    {
      public int AttemptCount
      {
        get;
        set;
      }

      public long Backtrackings
      {
        get;
        set;
      }

      public int CancelledAttemptCount
      {
        get;
        set;
      }

      public long ElapsedTicks
      {
        get;
        set;
      }

      public int MessageRejectedAttemptCount
      {
        get;
        set;
      }

      public int PlacementFailureCount
      {
        get;
        set;
      }

      public List<PlacementFile>? Placements
      {
        get;
        set;
      }

      public long TestedPositions
      {
        get;
        set;
      }

      public int WinningAttemptBacktrackings
      {
        get;
        set;
      }

      public long WinningAttemptElapsedTicks
      {
        get;
        set;
      }

      public int WinningAttemptNumber
      {
        get;
        set;
      }

      public long WinningAttemptTestedPositions
      {
        get;
        set;
      }

      public int WinningSeed
      {
        get;
        set;
      }
    }

    private sealed class PlacementFile
    {
      public int Column
      {
        get;
        set;
      }

      public DirectedLocation.LocationDirection Direction
      {
        get;
        set;
      }

      public int Row
      {
        get;
        set;
      }

      public int WordNumber
      {
        get;
        set;
      }
    }

    private sealed class ProjectFile
    {
      public int Columns
      {
        get;
        set;
      }

      public List<EntryFile>? Entries
      {
        get;
        set;
      }

      public string? EntryListHeading
      {
        get;
        set;
      }

      public int FormatVersion
      {
        get;
        set;
      }

      public GeneratedBoardFile? GeneratedBoard
      {
        get;
        set;
      }

      public PuzzleMode Mode
      {
        get;
        set;
      }

      public int ParallelAttempts
      {
        get;
        set;
      }

      public string? PuzzleHeading
      {
        get;
        set;
      }

      public int Rows
      {
        get;
        set;
      }

      public string? SecretMessage
      {
        get;
        set;
      }
    }

    #endregion
  }
}
