using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WordSearchGenerator.Common;
using WordSearchGenerator.Common.WoSeCon.Api;
using WordSearchGenerator.Desktop.Localization;
using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Models.Persistence;

namespace WordSearchGenerator.Desktop.Services.Persistence
{
  public sealed class PuzzleProjectSerializer : IPuzzleProjectSerializer
  {
    #region Static Fields

    private const int CurrentFormatVersion = 1;

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
                   throw new InvalidDataException(
                     AppStrings.Get("ProjectFileEmpty"));

        return RestoreProject(file);
      }
      catch (JsonException exception)
      {
        throw new InvalidDataException(
          AppStrings.Get("ProjectJsonInvalid"),
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
                        AppStrings.Get("ProjectPathNoParent"));

      if (!Directory.Exists(directory))
      {
        throw new DirectoryNotFoundException(AppStrings.Format(
          "DirectoryDoesNotExist",
          directory));
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

        File.Move(temporaryPath, fullPath, true);
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
              ? throw new InvalidOperationException(AppStrings.Format(
                "GeneratedWordNoPlacement",
                word.WordNumber))
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
          AppStrings.Get("SavedBoardPlacementCount"));
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
          AppStrings.Get("SavedAttemptNumbersInvalid"));
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
          throw new InvalidDataException(AppStrings.Format(
            "SavedPlacementMissing",
            word.WordNumber));
        }

        var placement = matches[0];

        if (!Enum.IsDefined(placement.Direction))
        {
          throw new InvalidDataException(AppStrings.Format(
            "SavedDirectionInvalid",
            word.WordNumber));
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
          throw new InvalidDataException(AppStrings.Format(
            "SavedPlacementOutside",
            word.WordNumber));
        }

        var locations = word.GetAllPlacementLocations(definition.QuizMode);

        if (placedWords.Any(previous => word.ConflictsWithWord(
              locations,
              previous,
              definition.QuizMode)))
        {
          throw new InvalidDataException(AppStrings.Format(
            "SavedPlacementConflict",
            word.WordNumber));
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
          AppStrings.Get("SavedBoardCannotReconstruct"),
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
        throw new InvalidDataException(AppStrings.Format(
          "UnsupportedProjectVersion",
          file.FormatVersion,
          CurrentFormatVersion));
      }

      if (!Enum.IsDefined(file.Mode))
      {
        throw new InvalidDataException(AppStrings.Get("ProjectModeInvalid"));
      }

      if (file.Entries == null || file.Entries.Count == 0)
      {
        throw new InvalidDataException(AppStrings.Get("ProjectEntryRequired"));
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
          (file.Mode == PuzzleMode.Quiz &&
           entries.Any(entry => string.IsNullOrWhiteSpace(entry.Question))))
      {
        throw new InvalidDataException(AppStrings.Get("ProjectEntriesInvalid"));
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
          AppStrings.Get("ProjectSettingsInvalid"),
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
        throw new InvalidDataException(AppStrings.Format(
          "SavedStatisticNegative",
          name));
      }
    }

    #endregion

    #region Nested Types

    private sealed class EntryFile
    {
      #region Properties

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

      #endregion
    }

    private sealed class GeneratedBoardFile
    {
      #region Properties

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

      #endregion
    }

    private sealed class PlacementFile
    {
      #region Properties

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

      #endregion
    }

    private sealed class ProjectFile
    {
      #region Properties

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

      #endregion
    }

    #endregion
  }
}