using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wose.Common;
using Wose.Common.WoSeCon.Api;
using Wose.Desktop.Localization;
using Wose.Desktop.Models;
using Wose.Desktop.Models.Persistence;
using Wose.Desktop.Services.Rendering;

namespace Wose.Desktop.Services.Persistence
{
  public sealed class PuzzleProjectSerializer : IPuzzleProjectSerializer
  {
    #region Static Fields

    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    private readonly IBoardStyleCatalog _boardStyleCatalog;

    #endregion

    #region Constructors

    public PuzzleProjectSerializer(IBoardStyleCatalog boardStyleCatalog)
    {
      _boardStyleCatalog = boardStyleCatalog ??
                           throw new ArgumentNullException(
                             nameof(boardStyleCatalog));
    }

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
          AmbiguousBoardRejectionCount = result.AmbiguousBoardRejectionCount,
          AmbiguityRejectedAttemptCount = result.AmbiguityRejectedAttemptCount,
          Backtrackings = result.Backtrackings,
          CancelledAttemptCount = result.CancelledAttemptCount,
          CompletedCandidateCount = result.CompletedCandidateCount,
          ElapsedTicks = result.Elapsed.Ticks,
          MessageCapacityRejectionCount = result.MessageCapacityRejectionCount,
          MessageRejectedAttemptCount = result.MessageCapacityRejectedAttemptCount,
          PlacementFailureCount = result.PlacementFailedAttemptCount,
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
        Entries = definition.Entries.ToList(),
        EntryListHeading = definition.EntryListHeading,
        GeneratedBoard = generated,
        Mode = definition.Mode,
        MaximumAttemptTimeSeconds =
          definition.Generation.MaximumAttemptTimeSeconds,
        ParallelAttempts = definition.Generation.ParallelAttempts,
        PuzzleHeading = definition.PuzzleHeading,
        RequireExactMessageFit = definition.RequireExactMessageFit,
        Rows = definition.Rows,
        SecretMessage = definition.SecretMessage,
        StyleId = definition.StyleId
      };
    }

    private static JsonSerializerOptions CreateOptions()
    {
      var options = new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        RespectNullableAnnotations = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true
      };
      options.Converters.Add(new JsonStringEnumConverter());
      return options;
    }

    private static GenerationResult RestoreGeneratedResult(
      PuzzleDefinition definition,
      GeneratedBoardFile generated)
    {
      if (generated.Placements.Count != definition.Entries.Count)
      {
        throw new InvalidDataException(
          AppStrings.Get("SavedBoardPlacementCount"));
      }

      ValidateNonNegative(generated.AttemptCount, nameof(generated.AttemptCount));
      ValidateNonNegative(
        generated.AmbiguousBoardRejectionCount,
        nameof(generated.AmbiguousBoardRejectionCount));
      ValidateNonNegative(
        generated.AmbiguityRejectedAttemptCount,
        nameof(generated.AmbiguityRejectedAttemptCount));
      ValidateNonNegative(generated.Backtrackings, nameof(generated.Backtrackings));
      ValidateNonNegative(
        generated.CancelledAttemptCount,
        nameof(generated.CancelledAttemptCount));
      ValidateNonNegative(generated.ElapsedTicks, nameof(generated.ElapsedTicks));
      ValidateNonNegative(
        generated.CompletedCandidateCount,
        nameof(generated.CompletedCandidateCount));
      ValidateNonNegative(
        generated.MessageCapacityRejectionCount,
        nameof(generated.MessageCapacityRejectionCount));
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
          definition,
          definition.SecretMessage,
          definition.RequireExactMessageFit);
      }
      catch (Exception exception)
      {
        throw new InvalidDataException(
          AppStrings.Get("SavedBoardCannotReconstruct"),
          exception);
      }

      if (!definition.QuizMode && !board.HasUniqueWordOccurrences())
      {
        throw new InvalidDataException(
          AppStrings.Get("SavedBoardAmbiguous"));
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
        generated.AmbiguityRejectedAttemptCount,
        generated.CompletedCandidateCount,
        generated.MessageCapacityRejectionCount,
        generated.AmbiguousBoardRejectionCount,
        generated.CancelledAttemptCount);
    }

    private PuzzleProject RestoreProject(ProjectFile file)
    {
      if (!Enum.IsDefined(file.Mode))
      {
        throw new InvalidDataException(AppStrings.Get("ProjectModeInvalid"));
      }

      if (file.Entries.Count == 0)
      {
        throw new InvalidDataException(AppStrings.Get("ProjectEntryRequired"));
      }

      if (!_boardStyleCatalog.Contains(file.StyleId))
      {
        throw new InvalidDataException(AppStrings.Format(
          "BoardStyleUnknown",
          file.StyleId));
      }

      var entries = file.Mode == PuzzleMode.Normal
        ? PuzzleInputParser.ParseWords(
          string.Join(
            Environment.NewLine,
            file.Entries.Select(entry => entry.Answer)))
        : PuzzleInputParser.ParseQuizEntries(file.Entries);

      var minimumEntryLength = file.Mode == PuzzleMode.Normal
        ? PuzzleInputParser.MinimumWordLength
        : 2;

      if (entries.Count != file.Entries.Count ||
          entries.Any(entry => entry.Answer.Length < minimumEntryLength) ||
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
          file.SecretMessage,
          file.PuzzleHeading,
          file.EntryListHeading,
          file.StyleId,
          new GenerationOptions(
            file.ParallelAttempts,
            file.MaximumAttemptTimeSeconds),
          file.RequireExactMessageFit);
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

    private sealed class GeneratedBoardFile
    {
      #region Properties

      public required long AmbiguousBoardRejectionCount
      {
        get;
        set;
      }

      public required int AmbiguityRejectedAttemptCount
      {
        get;
        set;
      }

      public required int AttemptCount
      {
        get;
        set;
      }

      public required long Backtrackings
      {
        get;
        set;
      }

      public required int CancelledAttemptCount
      {
        get;
        set;
      }

      public required long CompletedCandidateCount
      {
        get;
        set;
      }

      public required long ElapsedTicks
      {
        get;
        set;
      }

      public required long MessageCapacityRejectionCount
      {
        get;
        set;
      }

      public required int MessageRejectedAttemptCount
      {
        get;
        set;
      }

      public required int PlacementFailureCount
      {
        get;
        set;
      }

      public required List<PlacementFile> Placements
      {
        get;
        set;
      }

      public required long TestedPositions
      {
        get;
        set;
      }

      public required int WinningAttemptBacktrackings
      {
        get;
        set;
      }

      public required long WinningAttemptElapsedTicks
      {
        get;
        set;
      }

      public required int WinningAttemptNumber
      {
        get;
        set;
      }

      public required long WinningAttemptTestedPositions
      {
        get;
        set;
      }

      public required int WinningSeed
      {
        get;
        set;
      }

      #endregion
    }

    private sealed class PlacementFile
    {
      #region Properties

      public required int Column
      {
        get;
        set;
      }

      public required DirectedLocation.LocationDirection Direction
      {
        get;
        set;
      }

      public required int Row
      {
        get;
        set;
      }

      public required int WordNumber
      {
        get;
        set;
      }

      #endregion
    }

    private sealed class ProjectFile
    {
      #region Properties

      public required int Columns
      {
        get;
        set;
      }

      public required List<PuzzleEntry> Entries
      {
        get;
        set;
      }

      public required string EntryListHeading
      {
        get;
        set;
      }

      public required GeneratedBoardFile? GeneratedBoard
      {
        get;
        set;
      }

      public required PuzzleMode Mode
      {
        get;
        set;
      }

      public required int MaximumAttemptTimeSeconds
      {
        get;
        set;
      }

      public required int ParallelAttempts
      {
        get;
        set;
      }

      public required string PuzzleHeading
      {
        get;
        set;
      }

      public bool RequireExactMessageFit
      {
        get;
        set;
      }

      public required int Rows
      {
        get;
        set;
      }

      public required string SecretMessage
      {
        get;
        set;
      }

      public required string StyleId
      {
        get;
        set;
      }

      #endregion
    }

    #endregion
  }
}
