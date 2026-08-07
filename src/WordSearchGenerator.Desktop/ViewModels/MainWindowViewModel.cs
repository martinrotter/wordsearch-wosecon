using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using WordSearchGenerator.Common.WoSeCon;
using WordSearchGenerator.Desktop.Commands;
using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Services;

namespace WordSearchGenerator.Desktop.ViewModels
{
  public sealed class MainWindowViewModel : ValidatableViewModelBase
  {
    #region Fields

    private int _activeAttemptCount;
    private int _attemptCount;
    private long _backtrackings;
    private int _cancelledAttemptCount;
    private string _columnsText = "11";
    private bool _canGenerate;
    private GenerationResult? _currentResult;
    private CancellationTokenSource? _difficultyCancellation;
    private DifficultyDisplayState _difficultyDisplayState =
      DifficultyDisplayState.Unavailable;
    private string _difficultyText = "Not estimated";
    private TimeSpan _elapsed;
    private EditorActionState _editorActionState = EditorActionState.Invalid;
    private string _editorStatusMessage = "Enter at least one word.";
    private string _editorStatusTitle = "Puzzle needs attention";
    private int _finishedAttemptCount;
    private int _furthestPlacedWordCount;
    private int _messageRejectedAttemptCount;
    private PuzzleMode _mode;
    private int _placementFailureCount;
    private int _placedWordCount;
    private double _progressMaximum = 1;
    private double _progressValue;
    private string _previewMessage =
      "The generated puzzle will be rendered here. This area will later host the CefSharp preview.";
    private string _previewTitle = "No board generated";
    private ParallelismOption _selectedParallelismOption;
    private string _rowsText = "11";
    private string _secretMessage = string.Empty;
    private string _statusText = "Idle";
    private long _testedPositions;
    private int _totalWordCount;
    private string _wordsText = string.Empty;
    private readonly IPuzzleGenerator _puzzleGenerator;

    #endregion

    #region Properties

    public bool CanGenerate
    {
      get => _canGenerate;
      private set => SetProperty(ref _canGenerate, value);
    }

    public RelayCommand CancelCommand
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

    public string GenerateButtonText => EditorActionState switch
    {
      EditorActionState.Completed => "Generate again",
      EditorActionState.MessageDidNotFit => "Try again",
      EditorActionState.Failed => "Try again",
      EditorActionState.Cancelled => "Generate again",
      _ => "Generate"
    };

    public bool IsEditorEnabled => !IsGenerating;

    public bool IsGenerating => GenerateCommand.IsRunning;

    public IReadOnlyList<PuzzleMode> Modes
    {
      get;
    } = Enum.GetValues<PuzzleMode>();

    public PuzzleMode Mode
    {
      get => _mode;
      set
      {
        if (!SetProperty(ref _mode, value))
        {
          return;
        }

        OnPropertyChanged(nameof(IsNormalMode));
        OnPropertyChanged(nameof(IsQuizMode));
        RefreshEditorState();
      }
    }

    public bool IsNormalMode => Mode == PuzzleMode.Normal;

    public bool IsQuizMode => Mode == PuzzleMode.Quiz;

    public IReadOnlyList<ParallelismOption> ParallelismOptions
    {
      get;
    }

    public double ProgressMaximum
    {
      get => _progressMaximum;
      private set => SetProperty(ref _progressMaximum, value);
    }

    public string ProgressToolTip =>
      "The bar shows the furthest search depth, not elapsed-time completion.\n" +
      $"Workers: {ActiveAttemptCount:N0} active; " +
      $"{FinishedAttemptCount:N0} of {AttemptCount:N0} finished\n" +
      $"Placement failures: {PlacementFailureCount:N0}\n" +
      $"Message-capacity rejections: {MessageRejectedAttemptCount:N0}\n" +
      $"Cancelled attempts: {CancelledAttemptCount:N0}\n" +
      $"Currently placed: {PlacedWordCount:N0} of {TotalWordCount:N0}\n" +
      $"Furthest placed: {FurthestPlacedWordCount:N0} of {TotalWordCount:N0}\n" +
      $"Tested positions: {TestedPositions:N0}\n" +
      $"Backtracks: {Backtrackings:N0}\n" +
      $"Elapsed: {ElapsedText}";

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

    public string PreviewTitle
    {
      get => _previewTitle;
      private set => SetProperty(ref _previewTitle, value);
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
          RefreshEditorState();
        }
      }
    }

    public string SecretMessage
    {
      get => _secretMessage;
      set
      {
        if (SetProperty(ref _secretMessage, value ?? string.Empty))
        {
          RefreshEditorState();
        }
      }
    }

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

    public string WorkersText => AttemptCount == 0
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

    private int MessageRejectedAttemptCount
    {
      get => _messageRejectedAttemptCount;
      set
      {
        if (SetProperty(ref _messageRejectedAttemptCount, value))
        {
          OnPropertyChanged(nameof(ProgressToolTip));
        }
      }
    }

    private int PlacementFailureCount
    {
      get => _placementFailureCount;
      set
      {
        if (SetProperty(ref _placementFailureCount, value))
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
          RefreshEditorState();
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
          RefreshEditorState();
        }
      }
    }

    #endregion

    #region Constructors

    public MainWindowViewModel(IPuzzleGenerator puzzleGenerator)
    {
      ArgumentNullException.ThrowIfNull(puzzleGenerator);

      _puzzleGenerator = puzzleGenerator;
      var automaticParallelism = Math.Max(1, Environment.ProcessorCount);

      ParallelismOptions =
      [
        new ParallelismOption(
          $"Automatic ({automaticParallelism})",
          automaticParallelism),
        new ParallelismOption("1", 1),
        new ParallelismOption("2", 2),
        new ParallelismOption("4", 4),
        new ParallelismOption("8", 8),
        new ParallelismOption("16", 16)
      ];

      _selectedParallelismOption = ParallelismOptions[0];
      GenerateCommand = new AsyncRelayCommand(
        GenerateAsync,
        () => CanGenerate);
      CancelCommand = new RelayCommand(
        GenerateCommand.Cancel,
        () => GenerateCommand.CanBeCanceled);
      GenerateCommand.PropertyChanged += GenerateCommandOnPropertyChanged;

      QuizEntries.CollectionChanged += QuizEntriesOnCollectionChanged;
      QuizEntries.Add(new QuizEntryViewModel());

      RefreshEditorState();
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

    private PuzzleDefinition CreateDefinition()
    {
      var entries = GetNormalizedEntries();

      return new PuzzleDefinition(
        Mode,
        int.Parse(RowsText),
        int.Parse(ColumnsText),
        entries,
        SecretMessage,
        new GenerationOptions(
          SelectedParallelismOption.ParallelAttempts));
    }

    private static string FormatDifficulty(
      WoSeCon.EstimatedConstructionTime estimate)
    {
      return estimate switch
      {
        WoSeCon.EstimatedConstructionTime.FastInSeconds => "Fast — seconds",
        WoSeCon.EstimatedConstructionTime.FastUnderMinute => "Fast — under a minute",
        WoSeCon.EstimatedConstructionTime.SlowFewMinutes => "Slow — a few minutes",
        WoSeCon.EstimatedConstructionTime.SlowerManyMinutes => "Very slow — many minutes",
        WoSeCon.EstimatedConstructionTime.CrazySlowHours => "Extremely slow — possibly hours",
        WoSeCon.EstimatedConstructionTime.LikelyImpossible => "Likely impossible",
        _ => "Unknown"
      };
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
      CurrentResult = null;
      StatusText = "Starting";
      EditorActionState = EditorActionState.Generating;
      EditorStatusTitle = "Generating puzzle";
      EditorStatusMessage =
        $"Running {definition.Generation.ParallelAttempts:N0} independent " +
        "shuffled attempts.";
      PreviewTitle = "Generating board...";
      PreviewMessage =
        "Search progress and diagnostics are available below the preview.";

      var progress = new Progress<MonteCarloProgress>(UpdateProgress);

      try
      {
        var result = await _puzzleGenerator.GenerateAsync(
          definition,
          progress,
          cancellationToken);

        CurrentResult = result;
        Elapsed = result.Elapsed;
        TestedPositions = result.TestedPositions;
        Backtrackings = result.Backtrackings;
        AttemptCount = result.AttemptCount;
        ActiveAttemptCount = 0;
        FinishedAttemptCount = result.AttemptCount;
        PlacementFailureCount = result.PlacementFailureCount;
        MessageRejectedAttemptCount = result.MessageRejectedAttemptCount;
        CancelledAttemptCount = result.CancelledAttemptCount;
        PlacedWordCount = definition.Entries.Count;
        FurthestPlacedWordCount = definition.Entries.Count;
        ProgressValue = definition.Entries.Count;
        StatusText = "Completed";
        EditorActionState = EditorActionState.Completed;
        EditorStatusTitle = "Puzzle generated";
        EditorStatusMessage =
          $"Attempt {result.WinningAttemptNumber:N0} of " +
          $"{result.AttemptCount:N0} won with seed {result.WinningSeed}. " +
          $"Completed in {FormatElapsed(result.Elapsed)}.";
        PreviewTitle = "Board generated";
        PreviewMessage =
          $"{result.Board.RowCount} × {result.Board.ColumnCount}, " +
          $"{definition.Entries.Count} entries, " +
          $"{result.PuzzleOccupancyPercentage:F1}% puzzle occupancy, " +
          $"{result.MessageCellCount} message cells and " +
          $"{result.BlackBoxCount} black boxes. " +
          $"Winning seed: {result.WinningSeed}.";
      }
      catch (OperationCanceledException)
      {
        StatusText = "Cancelled";
        EditorActionState = EditorActionState.Cancelled;
        EditorStatusTitle = "Generation cancelled";
        EditorStatusMessage = "The background search stopped safely.";
        PreviewTitle = "Generation cancelled";
        PreviewMessage = "Adjust the puzzle or start another attempt.";
      }
      catch (MonteCarloGenerationException exception)
      {
        ActiveAttemptCount = 0;
        FinishedAttemptCount = exception.AttemptCount;
        PlacementFailureCount = exception.PlacementFailureCount;
        MessageRejectedAttemptCount = exception.MessageRejectedAttemptCount;

        if (exception.MessageRejectedAttemptCount > 0 &&
            exception.PlacementFailureCount == 0)
        {
          StatusText = "Message did not fit";
          EditorActionState = EditorActionState.MessageDidNotFit;
          EditorStatusTitle =
            "The generated placements left too few message cells";
          EditorStatusMessage = exception.Message;
          PreviewTitle = "Message needs more vacant cells";
          PreviewMessage =
            "Try again for different placements, shorten the message, or enlarge the matrix.";
        }
        else
        {
          StatusText = "No acceptable board";
          EditorActionState = EditorActionState.Failed;
          EditorStatusTitle = "No attempt produced an acceptable board";
          EditorStatusMessage = exception.Message;
          PreviewTitle = "No board generated";
          PreviewMessage =
            "Some attempts may have failed placement while others left too few message cells.";
        }
      }
      catch (Exception exception)
      {
        StatusText = "Failed";
        EditorActionState = EditorActionState.Failed;
        EditorStatusTitle = "Generation failed";
        EditorStatusMessage = exception.Message;
        PreviewTitle = "No board generated";
        PreviewMessage =
          "The words could not be placed in this matrix. Adjust the input and try again.";
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
    }

    private void ResetGenerationProgress(
      int totalWordCount,
      int attemptCount)
    {
      AttemptCount = attemptCount;
      ActiveAttemptCount = attemptCount;
      FinishedAttemptCount = 0;
      PlacementFailureCount = 0;
      MessageRejectedAttemptCount = 0;
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

    private void UpdateProgress(MonteCarloProgress progress)
    {
      ActiveAttemptCount = progress.ActiveAttemptCount;
      FinishedAttemptCount = progress.FinishedAttemptCount;
      AttemptCount = progress.TotalAttemptCount;
      PlacementFailureCount = progress.PlacementFailureCount;
      MessageRejectedAttemptCount = progress.MessageRejectedAttemptCount;
      CancelledAttemptCount = progress.CancelledAttemptCount;
      PlacedWordCount = progress.PlacedWordCount;
      FurthestPlacedWordCount = progress.FurthestPlacedWordCount;
      TotalWordCount = progress.TotalWordCount;
      TestedPositions = progress.TestedPositions;
      Backtrackings = progress.Backtrackings;
      Elapsed = progress.Elapsed;
      ProgressMaximum = Math.Max(1, progress.TotalWordCount);
      ProgressValue = progress.FurthestPlacedWordCount;
      StatusText = "Searching";
      EditorStatusMessage =
        $"{progress.ActiveAttemptCount:N0} active, " +
        $"{progress.FinishedAttemptCount:N0} finished; best depth " +
        $"{progress.FurthestPlacedWordCount:N0} of " +
        $"{progress.TotalWordCount:N0}.";
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

      RefreshEditorState();
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
      RefreshEditorState();
    }

    private void RefreshEditorState()
    {
      ValidateEditor();

      CanGenerate = !HasErrors;
      GenerateCommand?.NotifyCanExecuteChanged();

      if (CanGenerate)
      {
        EditorActionState = EditorActionState.Ready;
        EditorStatusTitle = "Ready to generate";
        EditorStatusMessage =
          $"{GetNormalizedEntries().Count} unique entries will be used.";
      }
      else
      {
        EditorActionState = EditorActionState.Invalid;
        EditorStatusTitle = "Puzzle needs attention";
        EditorStatusMessage = GetAllErrors().FirstOrDefault() ??
                              "Check the puzzle definition.";
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
        DifficultyText = "Not estimated";
        return;
      }

      var cancellation = new CancellationTokenSource();
      _difficultyCancellation = cancellation;
      DifficultyDisplayState = DifficultyDisplayState.Estimating;
      DifficultyText = "Estimating...";
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
          definition.Generation.ParallelAttempts);

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
        DifficultyText = "Not estimated";
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
      var rowsValid = ValidateDimension(RowsText, nameof(RowsText), "Rows");
      var columnsValid = ValidateDimension(
        ColumnsText,
        nameof(ColumnsText),
        "Columns");
      var entries = GetNormalizedEntries();
      var entryErrors = new List<string>();

      if (entries.Count == 0)
      {
        entryErrors.Add(Mode == PuzzleMode.Normal
          ? "Enter at least one word."
          : "Enter at least one complete question and answer.");
      }

      if (Mode == PuzzleMode.Normal)
      {
        var shortWord = entries.FirstOrDefault(entry => entry.Answer.Length < 2);

        if (shortWord != null)
        {
          entryErrors.Add(
            $"The word '{shortWord.Answer}' must contain at least two characters.");
        }
      }
      else if (QuizEntries.Any(entry => !entry.IsEmpty && entry.HasErrors))
      {
        entryErrors.Add("Complete every quiz question and answer.");
      }

      if (rowsValid && columnsValid && entries.Count != 0)
      {
        var rows = int.Parse(RowsText);
        var columns = int.Parse(ColumnsText);
        var maximumLength = Math.Max(rows, columns);
        var extraQuestionCell = Mode == PuzzleMode.Quiz ? 1 : 0;
        var entryThatDoesNotFit = entries.FirstOrDefault(
          entry => entry.Answer.Length + extraQuestionCell > maximumLength);

        if (entryThatDoesNotFit != null)
        {
          entryErrors.Add(
            $"'{entryThatDoesNotFit.Answer}' is too long for this matrix" +
            (Mode == PuzzleMode.Quiz ? " including its question cell." : "."));
        }
      }

      SetErrors(
        Mode == PuzzleMode.Normal ? nameof(WordsText) : nameof(QuizEntries),
        entryErrors);
      SetErrors(
        Mode == PuzzleMode.Normal ? nameof(QuizEntries) : nameof(WordsText),
        []);

      var messageErrors = new List<string>();

      if (rowsValid && columnsValid && entries.Count != 0)
      {
        var rows = int.Parse(RowsText);
        var columns = int.Parse(ColumnsText);
        var extraQuestionCell = Mode == PuzzleMode.Quiz ? 1 : 0;
        var minimumOccupiedCells = entries.Max(
          entry => entry.Answer.Length + extraQuestionCell);
        var maximumMessageLength = (long)rows * columns -
                                   minimumOccupiedCells;

        if (SecretMessage.Length > maximumMessageLength)
        {
          messageErrors.Add(
            "The secret message cannot fit even with maximum word overlap.");
        }
      }

      SetErrors(nameof(SecretMessage), messageErrors);
    }

    private bool ValidateDimension(
      string value,
      string propertyName,
      string displayName)
    {
      var errors = new List<string>();

      if (!int.TryParse(value, out var parsedValue) || parsedValue <= 0)
      {
        errors.Add($"{displayName} must be a positive whole number.");
      }

      SetErrors(propertyName, errors);
      return errors.Count == 0;
    }

    #endregion
  }
}
