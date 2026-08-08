using System.Diagnostics;
using WordSearchGenerator.Common.WoSeCon.Api;
using static WordSearchGenerator.Common.WoSeCon.Api.Locator;

namespace WordSearchGenerator.Common.WoSeCon
{
  public partial class WoSeCon
  {
    #region Enums

    public enum OperationMode
    {
      Forward,
      Backward
    }

    #endregion

    #region Static Fields

    private const int ProgressPositionCheckMask = 127;

    private static readonly long ProgressReportIntervalTicks =
      Stopwatch.Frequency / 10;

    #endregion

    #region Fields

    private int _isConstructing;

    #endregion

    #region Properties

    public int ColumnCount
    {
      get;
    }

    public Locator GlobalLocator
    {
      get;
      private set;
    }

    public OperationMode Mode
    {
      get;
      private set;
    }

    public int RowCount
    {
      get;
    }

    public List<WordInfo> Words
    {
      get;
    }

    /// <summary>
    ///   In quiz mode, one cell before the word serves as the
    ///   question cell and contains its number and direction.
    /// </summary>
    public bool QuizMode
    {
      get;
    }

    public int Backtrackings
    {
      get;
      private set;
    }

    public long TestedPositions
    {
      get;
      private set;
    }

    private LocationOrderer Orderer
    {
      get;
    }

    private bool HasConstructed
    {
      get;
      set;
    }

    #endregion

    #region Constructors

    public WoSeCon(
      List<WordInfo> words,
      int rowCount,
      int columnCount,
      bool quizMode,
      LocationOrderer orderer = null)
    {
      QuizMode = quizMode;
      RowCount = rowCount;
      ColumnCount = columnCount;
      Orderer = orderer;

      ValidateWords(words);
      Words = CloneAndSort(words);

      ResetState();
    }

    #endregion

    #region Other Stuff

    private static void ValidateWords(IEnumerable<WordInfo> words)
    {
      ArgumentNullException.ThrowIfNull(words);

      if (words.Any(word => word?.Text == null || word.Text.Length < 2))
      {
        throw new ArgumentException(
          "Every word must contain at least two characters.",
          nameof(words));
      }
    }

    private static List<WordInfo> CloneAndSort(IEnumerable<WordInfo> words)
    {
      return words
        .Select(word => (WordInfo)word.Clone())
        .OrderByDescending(wrd => wrd.Text.Length)
        .ToList();
    }

    /// <summary>
    ///   Constructs the word-search placement synchronously.
    /// </summary>
    /// <remarks>
    ///   Progress reports are throttled to avoid overwhelming a captured UI
    ///   synchronization context. Concurrent calls on the same instance are
    ///   rejected; use one WoSeCon instance per parallel attempt.
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    ///   The cancellation token was cancelled. Partial construction state is
    ///   cleared before the exception is rethrown.
    /// </exception>
    /// <param name="completionValidator">
    ///   Optional validator invoked after every complete word placement. When
    ///   it returns <see langword="false" />, construction backtracks and
    ///   searches for another complete placement.
    /// </param>
    public void Construct(
      IProgress<ConstructionProgress> progress = null,
      CancellationToken cancellationToken = default,
      Func<IReadOnlyList<WordInfo>, bool> completionValidator = null)
    {
      if (Interlocked.CompareExchange(ref _isConstructing, 1, 0) != 0)
      {
        throw new InvalidOperationException(
          "Construction is already running on this WoSeCon instance.");
      }

      var stopwatch = Stopwatch.StartNew();
      var nextProgressReportAt = 0L;
      var furthestPlacedWordCount = 0;
      WordInfo currentWord = null;

      void ReportProgress(bool force = false)
      {
        if (progress == null)
        {
          return;
        }

        var now = Stopwatch.GetTimestamp();

        if (!force && now < nextProgressReportAt)
        {
          return;
        }

        nextProgressReportAt = now + ProgressReportIntervalTicks;

        progress.Report(new ConstructionProgress(
          Words.Count(word => word.Placement != null),
          furthestPlacedWordCount,
          Words.Count,
          currentWord?.WordNumber ?? 0,
          TestedPositions,
          Backtrackings,
          stopwatch.Elapsed));
      }

      try
      {
        cancellationToken.ThrowIfCancellationRequested();

        if (HasConstructed)
        {
          ResetState();
        }
        else
        {
          ResetWordsAndStatistics();
        }

        HasConstructed = true;

        var wordIndex = 0;
        var word = Words[wordIndex];
        currentWord = word;
        Action reportCandidateProgress = progress == null ? null : () => ReportProgress();

        ReportProgress(true);

        while (true)
        {
          cancellationToken.ThrowIfCancellationRequested();

          var placed = PlaceWord(
            word,
            cancellationToken,
            reportCandidateProgress);

          cancellationToken.ThrowIfCancellationRequested();

          if (placed)
          {
            var placedWordCount = wordIndex + 1;
            furthestPlacedWordCount = Math.Max(
              furthestPlacedWordCount,
              placedWordCount);

            if (wordIndex == Words.Count - 1)
            {
              if (completionValidator == null || completionValidator(Words))
              {
                // Last word was placed and the complete layout is accepted.
                ReportProgress(true);
                break;
              }

              // The words fit, but the caller's complete-layout constraint
              // rejected this arrangement. Continue with the next placement.
              Backtrackings++;
              Mode = OperationMode.Backward;
              ReportProgress();
              continue;
            }

            ++wordIndex;
            word = Words[wordIndex];
            currentWord = word;
            Mode = OperationMode.Forward;
          }
          else
          {
            if (wordIndex == 0)
            {
              throw new Exception("given words cannot fit into the grid");
            }

            word.ClearTestedLocations();
            --wordIndex;
            Backtrackings++;
            word = Words[wordIndex];
            currentWord = word;
            Mode = OperationMode.Backward;
          }

          ReportProgress();
        }
      }
      catch (OperationCanceledException)
      {
        ResetState();
        HasConstructed = false;
        throw;
      }
      finally
      {
        Volatile.Write(ref _isConstructing, 0);
      }
    }

    private void ResetState()
    {
      ResetWordsAndStatistics();
      GlobalLocator = new Locator(RowCount, ColumnCount, Orderer);
    }

    private void ResetWordsAndStatistics()
    {
      foreach (var word in Words)
      {
        word.Placement = null;
        word.ClearTestedLocations();
      }

      Mode = OperationMode.Forward;
      Backtrackings = 0;
      TestedPositions = 0L;
    }

    public bool IsValidPlacement(WordInfo word, DirectedLocation location)
    {
      word.Placement = location;

      if (!word.WillFit(location, RowCount, ColumnCount, QuizMode))
      {
        word.Placement = null;
        return false;
      }

      var wordLocations = word.GetAllPlacementLocations(QuizMode);

      foreach (var wordToCheck in Words)
      {
        if (ReferenceEquals(word, wordToCheck))
        {
          continue;
        }

        if (word.ConflictsWithWord(wordLocations, wordToCheck, QuizMode))
        {
          // Either these words conflict
          // or we are in quiz mode where question
          // cells cannot intersect.
          word.Placement = null;
          return false;
        }
      }

      return true;
    }

    public bool PlaceWord(WordInfo word)
    {
      return PlaceWord(word, CancellationToken.None, null);
    }

    private bool PlaceWord(
      WordInfo word,
      CancellationToken cancellationToken,
      Action reportProgress)
    {
      Locator localLocator = null;

      if (Mode == OperationMode.Backward)
      {
        var wordLocation = word.Placement;
        GlobalLocator.AddAvailableLocation(wordLocation);
        word.MarkAsTestedOnPlacement();
        localLocator = GlobalLocator.Minus(word.TestedLocations);
      }
      else
      {
        localLocator = GlobalLocator;
      }

      var locationIndex = 0;

      while (locationIndex < localLocator.Size)
      {
        cancellationToken.ThrowIfCancellationRequested();
        TestedPositions++;

        var suitableLocation = localLocator[locationIndex];

        if (IsValidPlacement(word, suitableLocation))
        {
          GlobalLocator.RemoveAvailableLocation(suitableLocation);
          return true;
        }

        if ((TestedPositions & ProgressPositionCheckMask) == 0)
        {
          reportProgress?.Invoke();
        }

        locationIndex++;
      }

      return false;
    }

    #endregion
  }
}