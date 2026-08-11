using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using Wose.Common;
using Wose.Common.WoSeCon;
using Wose.Desktop.Commands;
using Wose.Desktop.Localization;
using Wose.Desktop.Models;
using Wose.Desktop.Models.Rendering;
using Wose.Desktop.Services;
using Wose.Desktop.Services.Rendering;

namespace Wose.Desktop.ViewModels
{
  public sealed partial class MainWindowViewModel : ValidatableViewModelBase
  {
    #region Fields

    private readonly IBoardHtmlRenderer _boardHtmlRenderer;
    private readonly string _defaultBoardStyleId;
    private readonly IPuzzleGenerator _puzzleGenerator;

    private int _activeAttemptCount;
    private long _ambiguousBoardRejectionCount;
    private int _attemptCount;
    private long _backtrackings;
    private int _blindPercentage;
    private BoardRenderModel? _boardRenderModel;
    private bool _canGenerate;
    private int _cancelledAttemptCount;
    private string _columnsText = NewProjectColumnsText;
    private long _completedCandidateCount;
    private GenerationResult? _currentResult;
    private CancellationTokenSource? _difficultyCancellation;

    private DifficultyDisplayState _difficultyDisplayState =
      DifficultyDisplayState.Unavailable;

    private string _difficultyText = AppStrings.Get("NotEstimated");
    private EditorActionState _editorActionState = EditorActionState.Invalid;
    private string _editorStatusMessage = AppStrings.Get("EnterAtLeastOneWord");
    private string _editorStatusTitle = AppStrings.Get("EditorNeedsAttention");
    private TimeSpan _elapsed;
    private string _entryListHeading = AppStrings.Get("WordsToFind");
    private int _finishedAttemptCount;
    private int _furthestPlacedWordCount;
    private bool _isExporting;
    private bool _isPreviewReady;
    private long _messageCapacityRejectionCount;
    private string _maximumAttemptTimeSecondsText =
      NewProjectMaximumAttemptTimeSecondsText;
    private PuzzleMode _mode;
    private int _placedWordCount;
    private int _placementFailedAttemptCount;
    private string _previewHtml = string.Empty;

    private string _previewMessage =
      AppStrings.Get("GenerateProjectBoard");

    private BoardPreviewMode _previewMode = BoardPreviewMode.Puzzle;
    private string _previewTitle = AppStrings.Get("NoBoardGenerated");
    private double _progressMaximum = 1;
    private double _progressValue;
    private string _puzzleHeading = string.Empty;
    private bool _requireExactMessageFit;
    private string _rowsText = NewProjectRowsText;
    private string _secretMessage = NewProjectSecretMessage;
    private string _selectedStyleId;
    private ParallelismOption _selectedParallelismOption;
    private string _statusText = AppStrings.Get("Idle");
    private long _testedPositions;
    private int _totalWordCount;
    private string _wordsText = NewProjectWordsText;

    #endregion

    #region Properties

    public bool CanGenerate
    {
      get => _canGenerate;
      private set => SetProperty(ref _canGenerate, value);
    }

    public int BlindPercentage
    {
      get => _blindPercentage;
      set
      {
        if (value is < 0 or > PuzzleDefinition.MaximumBlindPercentage)
        {
          throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (SetProperty(ref _blindPercentage, value))
        {
          MarkDocumentChanged(false);
          RefreshPreviewText();
        }
      }
    }

    public bool CanExport => HasPreview && !IsExporting;

    public bool CanExportBrowserPreview => IsPreviewReady && !IsExporting;

    public RelayCommand CancelCommand
    {
      get;
    }

    public RelayCommand ConvertToUppercaseCommand
    {
      get;
    }

    public string BacktrackingsText => Backtrackings.ToString("N0");

    public long Backtrackings
    {
      get => _backtrackings;
      private set
      {
        if (SetProperty(ref _backtrackings, value))
        {
          OnPropertyChanged(nameof(BacktrackingsText));
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    public string ColumnsText
    {
      get => _columnsText;
      set
      {
        if (SetProperty(ref _columnsText, value ?? string.Empty))
        {
          MarkDocumentChanged(true);
          RefreshEditorState();
        }
      }
    }

    public string DifficultyText
    {
      get => _difficultyText;
      private set => SetProperty(ref _difficultyText, value);
    }

    public DifficultyDisplayState DifficultyDisplayState
    {
      get => _difficultyDisplayState;
      private set => SetProperty(ref _difficultyDisplayState, value);
    }

    public string EditorStatusMessage
    {
      get => _editorStatusMessage;
      private set => SetProperty(ref _editorStatusMessage, value);
    }

    public EditorActionState EditorActionState
    {
      get => _editorActionState;
      private set
      {
        if (SetProperty(ref _editorActionState, value))
        {
          OnPropertyChanged(nameof(GenerateButtonText));
        }
      }
    }

    public string EditorStatusTitle
    {
      get => _editorStatusTitle;
      private set => SetProperty(ref _editorStatusTitle, value);
    }

    public string EntryListHeading
    {
      get => _entryListHeading;
      set
      {
        if (SetProperty(ref _entryListHeading, value ?? string.Empty))
        {
          MarkDocumentChanged(false);
          RefreshPreviewText();
        }
      }
    }

    public GenerationResult? CurrentResult
    {
      get => _currentResult;
      private set => SetProperty(ref _currentResult, value);
    }

    public TimeSpan Elapsed
    {
      get => _elapsed;
      private set
      {
        if (SetProperty(ref _elapsed, value))
        {
          OnPropertyChanged(nameof(ElapsedText));
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    public string ElapsedText => FormatElapsed(Elapsed);

    public AsyncRelayCommand GenerateCommand
    {
      get;
    }

    public string GenerateButtonText =>
      EditorActionState switch
      {
        EditorActionState.Completed => AppStrings.Get("GenerateAgain"),
        EditorActionState.MessageDidNotFit => AppStrings.Get("TryAgain"),
        EditorActionState.Failed => AppStrings.Get("TryAgain"),
        EditorActionState.Cancelled => AppStrings.Get("GenerateAgain"),
        _ => AppStrings.Get("Generate")
      };

    public bool HasPreview => !string.IsNullOrEmpty(PreviewHtml);

    public bool IsEditorEnabled => !IsGenerating && !IsExporting;

    public bool IsGenerating => GenerateCommand.IsRunning;

    public bool IsExporting
    {
      get => _isExporting;
      private set
      {
        if (SetProperty(ref _isExporting, value))
        {
          OnPropertyChanged(nameof(CanExport));
          OnPropertyChanged(nameof(CanExportBrowserPreview));
          OnPropertyChanged(nameof(IsEditorEnabled));
          GenerateCommand.NotifyCanExecuteChanged();
          ShowPuzzlePreviewCommand.NotifyCanExecuteChanged();
          ShowSolutionPreviewCommand.NotifyCanExecuteChanged();
        }
      }
    }

    public bool IsPuzzlePreviewSelected => PreviewMode == BoardPreviewMode.Puzzle;

    public bool IsPreviewReady
    {
      get => _isPreviewReady;
      private set
      {
        if (SetProperty(ref _isPreviewReady, value))
        {
          OnPropertyChanged(nameof(CanExportBrowserPreview));
        }
      }
    }

    public bool IsSolutionPreviewSelected => PreviewMode == BoardPreviewMode.Solution;

    public IReadOnlyList<PuzzleMode> Modes
    {
      get;
    } = Enum.GetValues<PuzzleMode>();

    public PuzzleMode Mode
    {
      get => _mode;
      set
      {
        var previousMode = Mode;

        if (!SetProperty(ref _mode, value))
        {
          return;
        }

        OnPropertyChanged(nameof(IsNormalMode));
        OnPropertyChanged(nameof(IsQuizMode));
        OnPropertyChanged(nameof(SecretMessageDescription));
        ConvertToUppercaseCommand.NotifyCanExecuteChanged();

        if (EntryListHeading == GetDefaultEntryListHeading(previousMode))
        {
          EntryListHeading = GetDefaultEntryListHeading(value);
        }

        MarkDocumentChanged(true);
        RefreshEditorState();
      }
    }

    public bool IsNormalMode => Mode == PuzzleMode.Normal;

    public bool IsQuizMode => Mode == PuzzleMode.Quiz;

    public ObservableCollection<ParallelismOption> ParallelismOptions
    {
      get;
    }

    public ObservableCollection<string> BoardStyleIds
    {
      get;
    }

    public double ProgressMaximum
    {
      get => _progressMaximum;
      private set => SetProperty(ref _progressMaximum, value);
    }

    public string ProgressToolTip =>
      AppStrings.Format(
        "ProgressTooltip",
        ActiveAttemptCount,
        FinishedAttemptCount,
        AttemptCount,
        PlacementFailedAttemptCount,
        CompletedCandidateCount,
        MessageCapacityRejectionCount,
        AmbiguousBoardRejectionCount,
        CancelledAttemptCount,
        PlacedWordCount,
        TotalWordCount,
        FurthestPlacedWordCount,
        TestedPositions,
        Backtrackings,
        ElapsedText);

    public double ProgressValue
    {
      get => _progressValue;
      private set => SetProperty(ref _progressValue, value);
    }

    public string PreviewMessage
    {
      get => _previewMessage;
      private set => SetProperty(ref _previewMessage, value);
    }

    public string PreviewHtml
    {
      get => _previewHtml;
      private set
      {
        if (SetProperty(ref _previewHtml, value))
        {
          IsPreviewReady = false;
          OnPropertyChanged(nameof(HasPreview));
          OnPropertyChanged(nameof(CanExport));
          ShowPuzzlePreviewCommand.NotifyCanExecuteChanged();
          ShowSolutionPreviewCommand.NotifyCanExecuteChanged();
        }
      }
    }

    public BoardPreviewMode PreviewMode
    {
      get => _previewMode;
      private set
      {
        if (SetProperty(ref _previewMode, value))
        {
          OnPropertyChanged(nameof(IsPuzzlePreviewSelected));
          OnPropertyChanged(nameof(IsSolutionPreviewSelected));
        }
      }
    }

    public string PreviewTitle
    {
      get => _previewTitle;
      private set => SetProperty(ref _previewTitle, value);
    }

    public string PuzzleHeading
    {
      get => _puzzleHeading;
      set
      {
        if (SetProperty(ref _puzzleHeading, value ?? string.Empty))
        {
          MarkDocumentChanged(false);
          RefreshPreviewText();
        }
      }
    }

    public ObservableCollection<QuizEntryViewModel> QuizEntries
    {
      get;
    } = [];

    public string RowsText
    {
      get => _rowsText;
      set
      {
        if (SetProperty(ref _rowsText, value ?? string.Empty))
        {
          MarkDocumentChanged(true);
          RefreshEditorState();
        }
      }
    }

    public string MaximumAttemptTimeSecondsText
    {
      get => _maximumAttemptTimeSecondsText;
      set
      {
        if (SetProperty(
              ref _maximumAttemptTimeSecondsText,
              value ?? string.Empty))
        {
          MarkDocumentChanged(false);
          RefreshEditorState();
        }
      }
    }

    public RelayCommand ShowPuzzlePreviewCommand
    {
      get;
    }

    public RelayCommand ShowSolutionPreviewCommand
    {
      get;
    }

    public string SecretMessage
    {
      get => _secretMessage;
      set
      {
        if (SetProperty(ref _secretMessage, value ?? string.Empty))
        {
          MarkDocumentChanged(true);
          RefreshEditorState();
        }
      }
    }

    public bool RequireExactMessageFit
    {
      get => _requireExactMessageFit;
      set
      {
        if (SetProperty(ref _requireExactMessageFit, value))
        {
          MarkDocumentChanged(true);
          RefreshEditorState();
        }
      }
    }

    public string SecretMessageDescription =>
      AppStrings.Get(
        IsQuizMode
          ? "SecretMessageQuizDescription"
          : "SecretMessageDescription");

    public string StatusText
    {
      get => _statusText;
      private set => SetProperty(ref _statusText, value);
    }

    public long TestedPositions
    {
      get => _testedPositions;
      private set
      {
        if (SetProperty(ref _testedPositions, value))
        {
          OnPropertyChanged(nameof(TestedPositionsText));
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    public string TestedPositionsText => TestedPositions.ToString("N0");

    public string WorkersText =>
      AttemptCount == 0
        ? "0"
        : $"{ActiveAttemptCount:N0} / {AttemptCount:N0}";

    private int ActiveAttemptCount
    {
      get => _activeAttemptCount;
      set
      {
        if (SetProperty(ref _activeAttemptCount, value))
        {
          OnPropertyChanged(nameof(WorkersText));
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    private int AttemptCount
    {
      get => _attemptCount;
      set
      {
        if (SetProperty(ref _attemptCount, value))
        {
          OnPropertyChanged(nameof(WorkersText));
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    private int CancelledAttemptCount
    {
      get => _cancelledAttemptCount;
      set
      {
        if (SetProperty(ref _cancelledAttemptCount, value))
        {
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    private long CompletedCandidateCount
    {
      get => _completedCandidateCount;
      set
      {
        if (SetProperty(ref _completedCandidateCount, value))
        {
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    private int FinishedAttemptCount
    {
      get => _finishedAttemptCount;
      set
      {
        if (SetProperty(ref _finishedAttemptCount, value))
        {
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    private int FurthestPlacedWordCount
    {
      get => _furthestPlacedWordCount;
      set
      {
        if (SetProperty(ref _furthestPlacedWordCount, value))
        {
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    private long AmbiguousBoardRejectionCount
    {
      get => _ambiguousBoardRejectionCount;
      set
      {
        if (SetProperty(ref _ambiguousBoardRejectionCount, value))
        {
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    private int PlacedWordCount
    {
      get => _placedWordCount;
      set
      {
        if (SetProperty(ref _placedWordCount, value))
        {
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    private long MessageCapacityRejectionCount
    {
      get => _messageCapacityRejectionCount;
      set
      {
        if (SetProperty(ref _messageCapacityRejectionCount, value))
        {
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    private int PlacementFailedAttemptCount
    {
      get => _placementFailedAttemptCount;
      set
      {
        if (SetProperty(ref _placementFailedAttemptCount, value))
        {
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    private int TotalWordCount
    {
      get => _totalWordCount;
      set
      {
        if (SetProperty(ref _totalWordCount, value))
        {
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    public ParallelismOption SelectedParallelismOption
    {
      get => _selectedParallelismOption;
      set
      {
        if (SetProperty(ref _selectedParallelismOption, value))
        {
          MarkDocumentChanged(false);
          RefreshEditorState();
        }
      }
    }

    public string SelectedStyleId
    {
      get => _selectedStyleId;
      set
      {
        if (SetProperty(ref _selectedStyleId, value))
        {
          MarkDocumentChanged(false);

          if (_boardRenderModel != null)
          {
            SetPreviewMode(PreviewMode, true);
          }
        }
      }
    }

    public string WordsText
    {
      get => _wordsText;
      set
      {
        if (SetProperty(ref _wordsText, value ?? string.Empty))
        {
          ConvertToUppercaseCommand.NotifyCanExecuteChanged();
          MarkDocumentChanged(true);
          RefreshEditorState();
        }
      }
    }

    #endregion

    #region Constructors

    public MainWindowViewModel(
      IPuzzleGenerator puzzleGenerator,
      IBoardHtmlRenderer boardHtmlRenderer,
      IBoardStyleCatalog boardStyleCatalog)
    {
      ArgumentNullException.ThrowIfNull(puzzleGenerator);
      ArgumentNullException.ThrowIfNull(boardHtmlRenderer);
      ArgumentNullException.ThrowIfNull(boardStyleCatalog);

      _puzzleGenerator = puzzleGenerator;
      _boardHtmlRenderer = boardHtmlRenderer;
      _defaultBoardStyleId = boardStyleCatalog.DefaultStyleId;
      var automaticParallelism = Math.Max(1, Environment.ProcessorCount);

      BoardStyleIds = new ObservableCollection<string>(
        boardStyleCatalog.StyleIds);

      ParallelismOptions =
      [
        new ParallelismOption(
          AppStrings.Format("AutomaticParallelism", automaticParallelism),
          automaticParallelism),
        new ParallelismOption("1", 1),
        new ParallelismOption("2", 2),
        new ParallelismOption("4", 4),
        new ParallelismOption("8", 8),
        new ParallelismOption("16", 16)
      ];

      _selectedParallelismOption = ParallelismOptions[0];
      _selectedStyleId = BoardStyleIds.Single(styleId =>
        styleId == _defaultBoardStyleId);
      GenerateCommand = new AsyncRelayCommand(
        GenerateAsync,
        () => CanGenerate && !IsExporting);
      CancelCommand = new RelayCommand(
        GenerateCommand.Cancel,
        () => GenerateCommand.CanBeCanceled);
      ConvertToUppercaseCommand = new RelayCommand(
        ConvertEntriesToUppercase,
        CanConvertEntriesToUppercase);
      ShowPuzzlePreviewCommand = new RelayCommand(
        () => SetPreviewMode(BoardPreviewMode.Puzzle),
        () => HasPreview && !IsExporting);
      ShowSolutionPreviewCommand = new RelayCommand(
        () => SetPreviewMode(BoardPreviewMode.Solution),
        () => HasPreview && !IsExporting);
      GenerateCommand.PropertyChanged += GenerateCommandOnPropertyChanged;

      QuizEntries.CollectionChanged += QuizEntriesOnCollectionChanged;
      QuizEntries.Add(new QuizEntryViewModel());

      RefreshEditorState();
      IsDirty = false;
    }

    #endregion

    #region Other Stuff

    public bool TryCreateDefinition(out PuzzleDefinition? definition)
    {
      ValidateEditor();

      if (HasErrors)
      {
        definition = null;
        return false;
      }

      definition = CreateDefinition();
      return true;
    }

    private bool CanConvertEntriesToUppercase()
    {
      if (!IsEditorEnabled)
      {
        return false;
      }

      return IsNormalMode
        ? HasLowercaseConversion(WordsText)
        : QuizEntries.Any(entry =>
          HasLowercaseConversion(entry.Answer));
    }

    private void ConvertEntriesToUppercase()
    {
      if (IsNormalMode)
      {
        WordsText = WordsText.ToUpper(CultureInfo.CurrentCulture);
        return;
      }

      foreach (var entry in QuizEntries)
      {
        entry.Answer = entry.Answer.ToUpper(CultureInfo.CurrentCulture);
      }
    }

    private static bool HasLowercaseConversion(string value)
    {
      return !string.Equals(
        value,
        value.ToUpper(CultureInfo.CurrentCulture),
        StringComparison.Ordinal);
    }

    internal BoardRenderModel? GetCurrentBoardRenderModel()
    {
      return _boardRenderModel;
    }

    internal void ReportExportCompleted(string status)
    {
      IsExporting = false;
      StatusText = status;
    }

    internal void ReportExportFailed(string? status = null)
    {
      IsExporting = false;
      StatusText = status ?? AppStrings.Get("ExportFailed");
    }

    internal void ReportExportStarted(string status)
    {
      IsExporting = true;
      StatusText = status;
    }

    internal void SetPreviewReady(bool isReady)
    {
      IsPreviewReady = isReady && HasPreview;
    }

    private PuzzleDefinition CreateDefinition()
    {
      var entries = GetNormalizedEntries();

      return new PuzzleDefinition(
        Mode,
        int.Parse(RowsText),
        int.Parse(ColumnsText),
        entries,
        SecretMessage,
        PuzzleHeading,
        EntryListHeading,
        SelectedStyleId,
        new GenerationOptions(
          SelectedParallelismOption.ParallelAttempts,
          int.Parse(MaximumAttemptTimeSecondsText)),
        RequireExactMessageFit,
        IsNormalMode ? BlindPercentage : 0);
    }

    private static string FormatDifficulty(
      WoSeCon.EstimatedConstructionTime estimate)
    {
      return estimate switch
      {
        WoSeCon.EstimatedConstructionTime.FastInSeconds =>
          AppStrings.Get("DifficultyFastSeconds"),
        WoSeCon.EstimatedConstructionTime.FastUnderMinute =>
          AppStrings.Get("DifficultyFastMinute"),
        WoSeCon.EstimatedConstructionTime.SlowFewMinutes =>
          AppStrings.Get("DifficultySlowMinutes"),
        WoSeCon.EstimatedConstructionTime.SlowerManyMinutes =>
          AppStrings.Get("DifficultyVerySlow"),
        WoSeCon.EstimatedConstructionTime.CrazySlowHours =>
          AppStrings.Get("DifficultyExtreme"),
        WoSeCon.EstimatedConstructionTime.LikelyImpossible =>
          AppStrings.Get("DifficultyImpossible"),
        _ => AppStrings.Get("Unknown")
      };
    }

    private static string GetDefaultEntryListHeading(PuzzleMode mode)
    {
      return mode == PuzzleMode.Quiz
        ? AppStrings.Get("Questions")
        : AppStrings.Get("WordsToFind");
    }

    private static DifficultyDisplayState GetDifficultyDisplayState(
      WoSeCon.EstimatedConstructionTime estimate)
    {
      return estimate switch
      {
        WoSeCon.EstimatedConstructionTime.FastInSeconds =>
          DifficultyDisplayState.FastInSeconds,
        WoSeCon.EstimatedConstructionTime.FastUnderMinute =>
          DifficultyDisplayState.FastUnderMinute,
        WoSeCon.EstimatedConstructionTime.SlowFewMinutes =>
          DifficultyDisplayState.SlowFewMinutes,
        WoSeCon.EstimatedConstructionTime.SlowerManyMinutes =>
          DifficultyDisplayState.SlowerManyMinutes,
        WoSeCon.EstimatedConstructionTime.CrazySlowHours =>
          DifficultyDisplayState.CrazySlowHours,
        WoSeCon.EstimatedConstructionTime.LikelyImpossible =>
          DifficultyDisplayState.LikelyImpossible,
        _ => DifficultyDisplayState.Unavailable
      };
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
      return elapsed.TotalHours >= 1
        ? elapsed.ToString(@"h\:mm\:ss")
        : elapsed.ToString(@"m\:ss\.f");
    }

    private static string FormatMessageCharacter(char character)
    {
      if (character == ' ')
      {
        return AppStrings.Get("SpaceCharacter");
      }

      return char.IsControl(character)
        ? $"U+{(int)character:X4}"
        : $"'{character}'";
    }

    private IReadOnlyList<PuzzleEntry> GetNormalizedEntries()
    {
      if (Mode == PuzzleMode.Normal)
      {
        return PuzzleInputParser.ParseWords(WordsText);
      }

      return PuzzleInputParser.ParseQuizEntries(
        QuizEntries.Select(entry => new PuzzleEntry(
          entry.Answer,
          entry.Question)));
    }

    private async Task GenerateAsync(CancellationToken cancellationToken)
    {
      if (!TryCreateDefinition(out var definition))
      {
        return;
      }

      ResetGenerationProgress(
        definition!.Entries.Count,
        definition.Generation.ParallelAttempts);
      ClearGeneratedBoard();
      MarkDocumentChanged(false);
      StatusText = AppStrings.Get("Starting");
      EditorActionState = EditorActionState.Generating;
      EditorStatusTitle = AppStrings.Get("GeneratingPuzzle");
      EditorStatusMessage = AppStrings.Format(
        "RunningAttempts",
        definition.Generation.ParallelAttempts);
      PreviewTitle = AppStrings.Get("GeneratingBoard");
      PreviewMessage = AppStrings.Get("ProgressAvailableBelow");

      var progress = new Progress<MonteCarloProgress>(UpdateProgress);

      try
      {
        var result = await _puzzleGenerator.GenerateAsync(
          definition,
          progress,
          cancellationToken);

        CurrentResult = result;
        _boardRenderModel = BoardRenderModel.Create(
          result,
          PuzzleHeading,
          EntryListHeading,
          BlindPercentage);
        SetPreviewMode(BoardPreviewMode.Puzzle, true);
        Elapsed = result.Elapsed;
        TestedPositions = result.TestedPositions;
        Backtrackings = result.Backtrackings;
        AttemptCount = result.AttemptCount;
        ActiveAttemptCount = 0;
        FinishedAttemptCount = result.AttemptCount;
        PlacementFailedAttemptCount = result.PlacementFailedAttemptCount;
        CompletedCandidateCount = result.CompletedCandidateCount;
        MessageCapacityRejectionCount = result.MessageCapacityRejectionCount;
        AmbiguousBoardRejectionCount = result.AmbiguousBoardRejectionCount;
        CancelledAttemptCount = result.CancelledAttemptCount;
        PlacedWordCount = definition.Entries.Count;
        FurthestPlacedWordCount = definition.Entries.Count;
        ProgressValue = definition.Entries.Count;
        StatusText = AppStrings.Get("Completed");
        EditorActionState = EditorActionState.Completed;
        EditorStatusTitle = AppStrings.Get("PuzzleGenerated");
        EditorStatusMessage = AppStrings.Format(
          "WinningAttemptSummary",
          result.WinningAttemptNumber,
          result.AttemptCount,
          result.WinningSeed,
          FormatElapsed(result.Elapsed));
        PreviewTitle = AppStrings.Get("BoardGenerated");
        PreviewMessage = AppStrings.Format(
          "BoardSummary",
          result.Board.Rows,
          result.Board.Columns,
          definition.Entries.Count,
          result.PuzzleOccupancyPercentage,
          result.MessageCellCount,
          result.BlackBoxCount,
          result.WinningSeed);
      }
      catch (OperationCanceledException)
      {
        StatusText = AppStrings.Get("Cancelled");
        EditorActionState = EditorActionState.Cancelled;
        EditorStatusTitle = AppStrings.Get("GenerationCancelled");
        EditorStatusMessage = AppStrings.Get("BackgroundSearchStopped");
        PreviewTitle = AppStrings.Get("GenerationCancelled");
        PreviewMessage = AppStrings.Get("AdjustOrTryAgain");
      }
      catch (MonteCarloGenerationException exception)
      {
        ActiveAttemptCount = 0;
        FinishedAttemptCount = exception.AttemptCount;
        PlacementFailedAttemptCount = exception.PlacementFailedAttemptCount;
        CompletedCandidateCount = exception.CompletedCandidateCount;
        MessageCapacityRejectionCount = exception.MessageCapacityRejectionCount;
        AmbiguousBoardRejectionCount = exception.AmbiguousBoardRejectionCount;

        if (exception.AmbiguousBoardRejectionCount > 0 &&
            exception.MessageCapacityRejectionCount == 0 &&
            exception.PlacementFailedAttemptCount == 0)
        {
          StatusText = AppStrings.Get("AmbiguousBoardsRejected");
          EditorActionState = EditorActionState.Failed;
          EditorStatusTitle = AppStrings.Get("AllBoardsAmbiguous");
          EditorStatusMessage = exception.Message;
          PreviewTitle = AppStrings.Get("NoUnambiguousBoard");
          PreviewMessage = AppStrings.Get("AmbiguityAdvice");
        }
        else if (exception.MessageCapacityRejectionCount > 0 &&
                 exception.AmbiguousBoardRejectionCount == 0 &&
                 exception.PlacementFailedAttemptCount == 0)
        {
          StatusText = AppStrings.Get("MessageDidNotFit");
          EditorActionState = EditorActionState.MessageDidNotFit;
          EditorStatusTitle = AppStrings.Get(
            definition.RequireExactMessageFit
              ? "NoExactMessageFit"
              : "PlacementsTooFewMessageCells");
          EditorStatusMessage = exception.Message;
          PreviewTitle = AppStrings.Get(
            definition.RequireExactMessageFit
              ? "NoExactMessageFit"
              : "MessageNeedsVacantCells");
          PreviewMessage = AppStrings.Get(
            definition.RequireExactMessageFit
              ? "ExactMessageFitAdvice"
              : "MessageFitAdvice");
        }
        else
        {
          StatusText = AppStrings.Get("NoAcceptableBoard");
          EditorActionState = EditorActionState.Failed;
          EditorStatusTitle = AppStrings.Get("NoAttemptProducedBoard");
          EditorStatusMessage = exception.Message;
          PreviewTitle = AppStrings.Get("NoBoardGenerated");
          PreviewMessage = AppStrings.Get("AttemptsFailedDescription");
        }
      }
      catch (Exception exception)
      {
        StatusText = AppStrings.Get("Failed");
        EditorActionState = EditorActionState.Failed;
        EditorStatusTitle = AppStrings.Get("GenerationFailed");
        EditorStatusMessage = exception.Message;
        PreviewTitle = AppStrings.Get("NoBoardGenerated");
        PreviewMessage = AppStrings.Get("WordsCouldNotBePlaced");
      }
    }

    private void GenerateCommandOnPropertyChanged(
      object? sender,
      PropertyChangedEventArgs e)
    {
      if (e.PropertyName != nameof(AsyncRelayCommand.IsRunning) &&
          e.PropertyName != nameof(AsyncRelayCommand.CanBeCanceled))
      {
        return;
      }

      OnPropertyChanged(nameof(IsGenerating));
      OnPropertyChanged(nameof(IsEditorEnabled));
      OnPropertyChanged(nameof(WorkersText));
      CancelCommand.NotifyCanExecuteChanged();
      ConvertToUppercaseCommand.NotifyCanExecuteChanged();
    }

    private void ResetGenerationProgress(
      int totalWordCount,
      int attemptCount)
    {
      AttemptCount = attemptCount;
      ActiveAttemptCount = attemptCount;
      FinishedAttemptCount = 0;
      PlacementFailedAttemptCount = 0;
      CompletedCandidateCount = 0;
      MessageCapacityRejectionCount = 0;
      AmbiguousBoardRejectionCount = 0;
      CancelledAttemptCount = 0;
      TotalWordCount = totalWordCount;
      PlacedWordCount = 0;
      FurthestPlacedWordCount = 0;
      TestedPositions = 0;
      Backtrackings = 0;
      Elapsed = TimeSpan.Zero;
      ProgressMaximum = Math.Max(1, totalWordCount);
      ProgressValue = 0;
      OnPropertyChanged(nameof(ProgressToolTip));
    }

    private void RefreshPreviewText()
    {
      if (CurrentResult == null)
      {
        return;
      }

      _boardRenderModel = BoardRenderModel.Create(
        CurrentResult,
        PuzzleHeading,
        EntryListHeading,
        BlindPercentage);
      SetPreviewMode(PreviewMode, true);
    }

    private void SetPreviewMode(
      BoardPreviewMode previewMode,
      bool force = false)
    {
      if (!force && PreviewMode == previewMode)
      {
        // ToggleButton changes IsChecked before executing its command. Reassert
        // the unchanged preview mode so the selected button cannot be toggled off.
        OnPropertyChanged(nameof(IsPuzzlePreviewSelected));
        OnPropertyChanged(nameof(IsSolutionPreviewSelected));
        return;
      }

      PreviewMode = previewMode;

      if (_boardRenderModel != null)
      {
        PreviewHtml = _boardHtmlRenderer.Render(
          _boardRenderModel,
          previewMode,
          SelectedStyleId);
      }
    }

    private void UpdateProgress(MonteCarloProgress progress)
    {
      ActiveAttemptCount = progress.ActiveAttemptCount;
      FinishedAttemptCount = progress.FinishedAttemptCount;
      AttemptCount = progress.TotalAttemptCount;
      PlacementFailedAttemptCount = progress.PlacementFailedAttemptCount;
      CompletedCandidateCount = progress.CompletedCandidateCount;
      MessageCapacityRejectionCount = progress.MessageCapacityRejectionCount;
      AmbiguousBoardRejectionCount = progress.AmbiguousBoardRejectionCount;
      CancelledAttemptCount = progress.CancelledAttemptCount;
      PlacedWordCount = progress.PlacedWordCount;
      FurthestPlacedWordCount = progress.FurthestPlacedWordCount;
      TotalWordCount = progress.TotalWordCount;
      TestedPositions = progress.TestedPositions;
      Backtrackings = progress.Backtrackings;
      Elapsed = progress.Elapsed;
      ProgressMaximum = Math.Max(1, progress.TotalWordCount);
      ProgressValue = progress.PlacedWordCount;
      StatusText = AppStrings.Get("Searching");
      EditorStatusMessage = AppStrings.Format(
        "SearchProgressSummary",
        progress.PlacedWordCount,
        progress.TotalWordCount,
        progress.CompletedCandidateCount);
    }

    private void QuizEntriesOnCollectionChanged(
      object? sender,
      NotifyCollectionChangedEventArgs e)
    {
      if (e.OldItems != null)
      {
        foreach (QuizEntryViewModel entry in e.OldItems)
        {
          entry.PropertyChanged -= QuizEntryOnPropertyChanged;
          entry.ErrorsChanged -= QuizEntryOnErrorsChanged;
        }
      }

      if (e.NewItems != null)
      {
        foreach (QuizEntryViewModel entry in e.NewItems)
        {
          entry.PropertyChanged += QuizEntryOnPropertyChanged;
          entry.ErrorsChanged += QuizEntryOnErrorsChanged;
        }
      }

      MarkDocumentChanged(true);
      RefreshEditorState();
      ConvertToUppercaseCommand.NotifyCanExecuteChanged();
    }

    private void QuizEntryOnErrorsChanged(
      object? sender,
      DataErrorsChangedEventArgs e)
    {
      RefreshEditorState();
    }

    private void QuizEntryOnPropertyChanged(
      object? sender,
      PropertyChangedEventArgs e)
    {
      MarkDocumentChanged(true);
      RefreshEditorState();
      ConvertToUppercaseCommand.NotifyCanExecuteChanged();
    }

    private void RefreshEditorState()
    {
      ValidateEditor();

      CanGenerate = !HasErrors;
      GenerateCommand?.NotifyCanExecuteChanged();

      if (CanGenerate)
      {
        EditorActionState = EditorActionState.Ready;
        EditorStatusTitle = AppStrings.Get("ReadyToGenerate");
        EditorStatusMessage = AppStrings.Format(
          "UniqueEntriesUsed",
          GetNormalizedEntries().Count);
      }
      else
      {
        EditorActionState = EditorActionState.Invalid;
        EditorStatusTitle = AppStrings.Get("EditorNeedsAttention");
        EditorStatusMessage = GetAllErrors().FirstOrDefault() ??
                              AppStrings.Get("CheckPuzzleDefinition");
      }

      ScheduleDifficultyEstimate();
    }

    private void ScheduleDifficultyEstimate()
    {
      _difficultyCancellation?.Cancel();
      _difficultyCancellation?.Dispose();
      _difficultyCancellation = null;

      if (!CanGenerate)
      {
        DifficultyDisplayState = DifficultyDisplayState.Unavailable;
        DifficultyText = AppStrings.Get("NotEstimated");
        return;
      }

      var cancellation = new CancellationTokenSource();
      _difficultyCancellation = cancellation;
      DifficultyDisplayState = DifficultyDisplayState.Estimating;
      DifficultyText = AppStrings.Get("Estimating");
      _ = UpdateDifficultyAsync(cancellation);
    }

    private async Task UpdateDifficultyAsync(
      CancellationTokenSource cancellation)
    {
      try
      {
        await Task.Delay(300, cancellation.Token);
        var definition = CreateDefinition();
        var estimate = WoSeCon.EstimateDifficulty(
          definition.CreateWordInfos(),
          definition.Rows,
          definition.Columns,
          definition.QuizMode,
          definition.Generation.ParallelAttempts,
          definition.QuizMode ? 0 : definition.SecretMessage.Length);

        cancellation.Token.ThrowIfCancellationRequested();
        DifficultyDisplayState = GetDifficultyDisplayState(estimate);
        DifficultyText = FormatDifficulty(estimate);
      }
      catch (OperationCanceledException)
      {
        // A newer edit superseded this estimate.
      }
      catch (ArgumentException)
      {
        DifficultyDisplayState = DifficultyDisplayState.Unavailable;
        DifficultyText = AppStrings.Get("NotEstimated");
      }
      finally
      {
        if (ReferenceEquals(_difficultyCancellation, cancellation))
        {
          _difficultyCancellation.Dispose();
          _difficultyCancellation = null;
        }
      }
    }

    private void ValidateEditor()
    {
      ValidateMaximumAttemptTime();
      var rowsValid = ValidateDimension(
        RowsText,
        nameof(RowsText),
        AppStrings.Get("Rows"));
      var columnsValid = ValidateDimension(
        ColumnsText,
        nameof(ColumnsText),
        AppStrings.Get("Columns"));
      var entries = GetNormalizedEntries();
      var entryErrors = new List<string>();

      if (entries.Count == 0)
      {
        entryErrors.Add(Mode == PuzzleMode.Normal
          ? AppStrings.Get("EnterAtLeastOneWord")
          : AppStrings.Get("EnterQuizEntry"));
      }

      if (Mode == PuzzleMode.Normal)
      {
        var shortWord =
          entries.FirstOrDefault(entry => entry.Answer.Length < PuzzleInputParser.MinimumWordLength);

        if (shortWord != null)
        {
          entryErrors.Add(AppStrings.Format(
            "WordMinimumLength",
            shortWord.Answer));
        }

        if (shortWord == null &&
            WordContainmentValidator.TryFindConflict(
              entries,
              out var firstConflict,
              out var secondConflict))
        {
          entryErrors.Add(AppStrings.Format(
            "ContainedWords",
            firstConflict!.Answer,
            secondConflict!.Answer));
        }
      }
      else if (QuizEntries.Any(entry => !entry.IsEmpty && entry.HasErrors))
      {
        entryErrors.Add(AppStrings.Get("CompleteQuizEntries"));
      }

      if (rowsValid && columnsValid && entries.Count != 0)
      {
        var rows = int.Parse(RowsText);
        var columns = int.Parse(ColumnsText);
        var maximumLength = Math.Max(rows, columns);
        var extraQuestionCell = Mode == PuzzleMode.Quiz ? 1 : 0;
        var entryThatDoesNotFit =
          entries.FirstOrDefault(entry => entry.Answer.Length + extraQuestionCell > maximumLength);

        if (entryThatDoesNotFit != null)
        {
          entryErrors.Add(AppStrings.Format(
            Mode == PuzzleMode.Quiz ? "QuizEntryTooLong" : "EntryTooLong",
            entryThatDoesNotFit.Answer));
        }
      }

      SetErrors(
        Mode == PuzzleMode.Normal ? nameof(WordsText) : nameof(QuizEntries),
        entryErrors);
      SetErrors(
        Mode == PuzzleMode.Normal ? nameof(QuizEntries) : nameof(WordsText),
        []);

      var messageErrors = new List<string>();

      if (Mode == PuzzleMode.Normal &&
          rowsValid &&
          columnsValid &&
          entries.Count != 0)
      {
        var rows = int.Parse(RowsText);
        var columns = int.Parse(ColumnsText);
        var minimumOccupiedCells = entries.Max(entry => entry.Answer.Length);
        var maximumMessageLength = (long)rows * columns -
                                   minimumOccupiedCells;

        if (SecretMessage.Length > maximumMessageLength)
        {
          messageErrors.Add(AppStrings.Get("SecretMessageTooLong"));
        }
      }
      else if (Mode == PuzzleMode.Quiz &&
               entries.Count != 0 &&
               entryErrors.Count == 0)
      {
        var availableCharacters = entries
          .SelectMany(entry => entry.Answer)
          .GroupBy(character => character)
          .ToDictionary(group => group.Key, group => group.Count());
        var unavailableCharacter = SecretMessage
          .GroupBy(character => character)
          .Select(group => new
          {
            Character = group.Key,
            Required = group.Count(),
            Available = availableCharacters.GetValueOrDefault(group.Key)
          })
          .FirstOrDefault(counts => counts.Required > counts.Available);

        if (unavailableCharacter != null)
        {
          messageErrors.Add(AppStrings.Format(
            "SecretMessageCharacterUnavailable",
            FormatMessageCharacter(unavailableCharacter.Character),
            unavailableCharacter.Available,
            unavailableCharacter.Required));
        }
      }

      SetErrors(nameof(SecretMessage), messageErrors);
    }

    private void ValidateMaximumAttemptTime()
    {
      var errors = new List<string>();

      if (!int.TryParse(
            MaximumAttemptTimeSecondsText,
            out var maximumAttemptTimeSeconds) ||
          maximumAttemptTimeSeconds < 0 ||
          maximumAttemptTimeSeconds >
          GenerationOptions.MaximumAttemptTimeSecondsLimit)
      {
        errors.Add(AppStrings.Get("MaximumAttemptTimeRange"));
      }

      SetErrors(nameof(MaximumAttemptTimeSecondsText), errors);
    }

    private bool ValidateDimension(
      string value,
      string propertyName,
      string displayName)
    {
      var errors = new List<string>();

      if (!int.TryParse(value, out var parsedValue) || parsedValue <= 0)
      {
        errors.Add(AppStrings.Format("PositiveWholeNumber", displayName));
      }

      SetErrors(propertyName, errors);
      return errors.Count == 0;
    }

    #endregion
  }
}
