using System.IO;
using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Models.Persistence;
using WordSearchGenerator.Desktop.Models.Rendering;

namespace WordSearchGenerator.Desktop.ViewModels
{
  public sealed partial class MainWindowViewModel
  {
    #region Constants

    private const string NewProjectColumnsText = "5";
    private const string NewProjectRowsText = "4";
    private const string NewProjectSecretMessage = "hello";
    private const string NewProjectWordsText = "cat\r\ndog\r\nsun\r\nmap";

    #endregion

    #region Fields

    private bool _isDirty;
    private string? _projectFilePath;
    private bool _suppressDocumentChanges;

    #endregion

    #region Properties

    public bool IsDirty
    {
      get => _isDirty;
      private set
      {
        if (SetProperty(ref _isDirty, value))
        {
          OnPropertyChanged(nameof(WindowTitle));
        }
      }
    }

    public string? ProjectFilePath
    {
      get => _projectFilePath;
      private set
      {
        if (SetProperty(ref _projectFilePath, value))
        {
          OnPropertyChanged(nameof(WindowTitle));
        }
      }
    }

    public string WindowTitle
    {
      get
      {
        var documentName = ProjectFilePath == null
          ? "Untitled"
          : Path.GetFileNameWithoutExtension(ProjectFilePath);
        var dirtyMarker = IsDirty ? " *" : string.Empty;

        return $"{documentName}{dirtyMarker} - WoSeCon";
      }
    }

    #endregion

    #region Other Stuff

    internal void ApplyImportedEntries(IReadOnlyList<PuzzleEntry> entries)
    {
      ArgumentNullException.ThrowIfNull(entries);

      if (Mode == PuzzleMode.Normal)
      {
        WordsText = string.Join(
          Environment.NewLine,
          entries.Select(entry => entry.Answer));
      }
      else
      {
        QuizEntries.Clear();

        foreach (var entry in entries)
        {
          QuizEntries.Add(new QuizEntryViewModel
          {
            Answer = entry.Answer,
            Question = entry.Question ?? string.Empty
          });
        }
      }

      StatusText = Mode == PuzzleMode.Normal
        ? "Words imported"
        : "Quiz entries imported";
    }

    internal void LoadProject(PuzzleProject project, string path)
    {
      ArgumentNullException.ThrowIfNull(project);
      ArgumentException.ThrowIfNullOrWhiteSpace(path);

      _suppressDocumentChanges = true;

      try
      {
        ClearGeneratedBoard();
        var definition = project.Definition;

        Mode = definition.Mode;
        RowsText = definition.Rows.ToString();
        ColumnsText = definition.Columns.ToString();
        SecretMessage = definition.SecretMessage;
        PuzzleHeading = definition.PuzzleHeading;
        EntryListHeading = definition.EntryListHeading;
        SelectedParallelismOption = GetOrAddParallelismOption(
          definition.Generation.ParallelAttempts);

        if (definition.Mode == PuzzleMode.Normal)
        {
          WordsText = string.Join(
            Environment.NewLine,
            definition.Entries.Select(entry => entry.Answer));
          QuizEntries.Clear();
          QuizEntries.Add(new QuizEntryViewModel());
        }
        else
        {
          WordsText = string.Empty;
          QuizEntries.Clear();

          foreach (var entry in definition.Entries)
          {
            QuizEntries.Add(new QuizEntryViewModel
            {
              Answer = entry.Answer,
              Question = entry.Question ?? string.Empty
            });
          }
        }

        RefreshEditorState();

        if (project.GeneratedResult != null)
        {
          RestoreGeneratedResult(project.GeneratedResult);
        }
        else
        {
          ResetGenerationProgress(definition.Entries.Count, 0);
          StatusText = "Project loaded";
          PreviewTitle = "No board generated";
          PreviewMessage = "Generate the puzzle to create its board.";
        }
      }
      finally
      {
        _suppressDocumentChanges = false;
      }

      ProjectFilePath = Path.GetFullPath(path);
      IsDirty = false;
    }

    internal void MarkProjectSaved(string path)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(path);

      ProjectFilePath = Path.GetFullPath(path);
      IsDirty = false;
      StatusText = "Project saved";
    }

    internal void NewProject()
    {
      _suppressDocumentChanges = true;

      try
      {
        ClearGeneratedBoard();
        Mode = PuzzleMode.Normal;
        RowsText = NewProjectRowsText;
        ColumnsText = NewProjectColumnsText;
        WordsText = NewProjectWordsText;
        SecretMessage = NewProjectSecretMessage;
        PuzzleHeading = string.Empty;
        EntryListHeading = GetDefaultEntryListHeading(PuzzleMode.Normal);
        SelectedParallelismOption = ParallelismOptions[0];
        QuizEntries.Clear();
        QuizEntries.Add(new QuizEntryViewModel());
        ResetGenerationProgress(GetNormalizedEntries().Count, 0);
        StatusText = "Idle";
        PreviewTitle = "No board generated";
        PreviewMessage = "Generate a puzzle to see its printable board here.";
        RefreshEditorState();
      }
      finally
      {
        _suppressDocumentChanges = false;
      }

      ProjectFilePath = null;
      IsDirty = false;
    }

    internal bool TryCreateProjectSnapshot(
      out PuzzleDefinition? definition,
      out GenerationResult? generatedResult)
    {
      if (!TryCreateDefinition(out definition))
      {
        generatedResult = null;
        return false;
      }

      generatedResult = CurrentResult;
      return true;
    }

    private void ClearGeneratedBoard()
    {
      CurrentResult = null;
      _boardRenderModel = null;
      PreviewHtml = string.Empty;
      PreviewMode = BoardPreviewMode.Puzzle;
    }

    private ParallelismOption GetOrAddParallelismOption(int parallelAttempts)
    {
      var option = ParallelismOptions.FirstOrDefault(
        candidate => candidate.ParallelAttempts == parallelAttempts);

      if (option != null)
      {
        return option;
      }

      option = new ParallelismOption(
        parallelAttempts.ToString(),
        parallelAttempts);
      ParallelismOptions.Add(option);
      return option;
    }

    private void InvalidateGeneratedBoard()
    {
      if (CurrentResult == null && string.IsNullOrEmpty(PreviewHtml))
      {
        return;
      }

      ClearGeneratedBoard();
      ResetGenerationProgress(GetNormalizedEntries().Count, 0);
      StatusText = "Modified";
      PreviewTitle = "Board needs regeneration";
      PreviewMessage =
        "Puzzle content changed. Generate again to create an updated board.";
    }

    private void MarkDocumentChanged(bool invalidateGeneratedBoard)
    {
      if (_suppressDocumentChanges)
      {
        return;
      }

      if (invalidateGeneratedBoard)
      {
        InvalidateGeneratedBoard();
      }

      IsDirty = true;
    }

    private void RestoreGeneratedResult(GenerationResult result)
    {
      CurrentResult = result;
      _boardRenderModel = BoardRenderModel.Create(
        result,
        PuzzleHeading,
        EntryListHeading);
      SetPreviewMode(BoardPreviewMode.Puzzle, force: true);
      Elapsed = result.Elapsed;
      TestedPositions = result.TestedPositions;
      Backtrackings = result.Backtrackings;
      AttemptCount = result.AttemptCount;
      ActiveAttemptCount = 0;
      FinishedAttemptCount = result.AttemptCount;
      PlacementFailureCount = result.PlacementFailureCount;
      MessageRejectedAttemptCount = result.MessageRejectedAttemptCount;
      CancelledAttemptCount = result.CancelledAttemptCount;
      PlacedWordCount = result.Definition.Entries.Count;
      FurthestPlacedWordCount = result.Definition.Entries.Count;
      ProgressMaximum = Math.Max(1, result.Definition.Entries.Count);
      ProgressValue = result.Definition.Entries.Count;
      StatusText = "Project loaded";
      EditorActionState = EditorActionState.Completed;
      EditorStatusTitle = "Generated board restored";
      EditorStatusMessage =
        $"Winning seed {result.WinningSeed} was restored without regeneration.";
      PreviewTitle = "Board generated";
      PreviewMessage =
        $"{result.Board.RowCount} x {result.Board.ColumnCount}, " +
        $"{result.Definition.Entries.Count} entries, " +
        $"{result.PuzzleOccupancyPercentage:F1}% puzzle occupancy, " +
        $"{result.MessageCellCount} message cells and " +
        $"{result.BlackBoxCount} black boxes. " +
        $"Winning seed: {result.WinningSeed}.";
    }

    #endregion
  }
}
