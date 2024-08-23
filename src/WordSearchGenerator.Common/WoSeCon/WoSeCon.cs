using WordSearchGenerator.Common.WoSeCon.Api;

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
    }

    public OperationMode Mode
    {
      get;
      set;
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
    /// In quiz mode, special initial character
    /// is prepended to each word and will server
    /// as "question" cell with direction of the word
    /// and number of question. 
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

    public long TestesPositions
    {
      get;
      private set;
    }

    #endregion

    #region Constructors

    public WoSeCon(List<WordInfo> words, int rowCount, int columnCount, bool quizMode)
    {
      QuizMode = quizMode;
      RowCount = rowCount;
      ColumnCount = columnCount;
      Words = Sort(words);

      GlobalLocator = new RandomLocator(RowCount, ColumnCount);
    }

    #endregion

    #region Other Stuff

    private List<WordInfo> Sort(List<WordInfo> words)
    {
      return words
        .OrderByDescending(wrd => wrd.Text.Length)
        .Select(wrd =>
        {
          if (QuizMode)
          {
            wrd.Text = $"{Constants.Misc.QuizModePlaceholder}{wrd.Text}";
          }

          return wrd;
        })
        .ToList();
    }

    public void Construct(CancellationToken? ct)
    {
      Backtrackings = 0;
      TestesPositions = 0L;

      int wordIndex = 0;
      WordInfo word = Words[wordIndex];

      Mode = OperationMode.Forward;

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

    public bool IsValidPlacement(WordInfo word, DirectedLocation location)
    {
      word.Placement = location;

      if (!word.WillFit(location, RowCount, ColumnCount))
      {
        word.Placement = null;
        return false;
      }

      foreach (WordInfo wordToCheck in Words)
      {
        if (word != wordToCheck)
        {
          if (word.ConflictsWithWord(wordToCheck, QuizMode))
          {
            // Either these words conflict
            // or we are in quiz mode where placeholder
            // cells cannot intersect.
            word.Placement = null;
            return false;
          }
        }
      }

      return true;
    }

    public bool PlaceWord(WordInfo word)
    {
      List<DirectedLocation> testedLocations = word.TestedLocations;
      RandomLocator localLocator = null;

      if (Mode == OperationMode.Backward)
      {
        RandomLocator l = RandomLocator.GetWithoutLocations(RowCount, ColumnCount, testedLocations);
        DirectedLocation wordLocation = word.Placement;
        GlobalLocator.AddAvailableLocation(wordLocation);
        word.MarkAsTestedOnPlacement();
        localLocator = l;
      }
      else
      {
        localLocator = GlobalLocator;
      }

      int locationIndex = 0;

      while (locationIndex < localLocator.Size)
      {
        TestesPositions++;

        DirectedLocation suitableLocation = localLocator[locationIndex];

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