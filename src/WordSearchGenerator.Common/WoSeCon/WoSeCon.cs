using WordSearchGenerator.Common.WoSeCon.Api;
using static WordSearchGenerator.Common.WoSeCon.Api.RandomLocator;

namespace WordSearchGenerator.Common.WoSeCon
{
  public class WoSeCon
  {
    #region Enums

    public enum OperationMode
    {
      Forward,
      Backward
    }

    #endregion

    #region Properties

    public int ColumnCount
    {
      get;
    }

    public RandomLocator GlobalLocator
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

    public WoSeCon(List<WordInfo> words, int rowCount, int columnCount, bool quizMode,
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

    public void Construct(CancellationToken? ct)
    {
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

      while (true)
      {
        if (ct?.IsCancellationRequested == true)
        {
          throw new TaskCanceledException();
        }

        if (PlaceWord(word))
        {
          if (wordIndex == Words.Count - 1)
          {
            // Last word was placed, we are done.
            break;
          }

          ++wordIndex;
          word = Words[wordIndex];
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
          Mode = OperationMode.Backward;
        }
      }
    }

    private void ResetState()
    {
      ResetWordsAndStatistics();
      GlobalLocator = new RandomLocator(RowCount, ColumnCount, Orderer);
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

      foreach (var wordToCheck in Words)
      {
        if (ReferenceEquals(word, wordToCheck))
        {
          continue;
        }

        if (word.ConflictsWithWord(wordToCheck, QuizMode))
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
      RandomLocator localLocator = null;

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
        TestedPositions++;

        var suitableLocation = localLocator[locationIndex];

        if (IsValidPlacement(word, suitableLocation))
        {
          GlobalLocator.RemoveAvailableLocation(suitableLocation);
          return true;
        }

        locationIndex++;
      }

      return false;
    }

    #endregion
  }
}
