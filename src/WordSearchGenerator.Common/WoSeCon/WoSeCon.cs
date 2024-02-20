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

    #endregion

    #region Constructors

    public WoSeCon(List<WordInfo> words, int rowCount, int columnCount, bool twistWords = false)
    {
      RowCount = rowCount;
      ColumnCount = columnCount;
      Words = twistWords ? Twist(words).ToList() : words;

      GlobalLocator = new RandomLocator(RowCount, ColumnCount);
    }

    #endregion

    #region Other Stuff

    public int Construct()
    {
      int backtrackings = 0;
      int wordIndex = 0;
      WordInfo word = Words[wordIndex];

      Mode = OperationMode.Forward;

      while (true)
      {
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
          backtrackings++;
          word = Words[wordIndex];
          Mode = OperationMode.Backward;
        }
      }

      return backtrackings;
    }

    public bool IsValidPlacement(WordInfo word, DirectedLocation location)
    {
      word.Placement = location;
      List<DirectedLocation> letterLocations = word.GetAllLetterLocations();

      foreach (DirectedLocation l in letterLocations)
      {
        if (l.Row > RowCount - 1 || l.Column > ColumnCount - 1)
        {
          // We are out of matrix bounds.
          word.Placement = null;
          return false;
        }
      }

      for (int i = 0; i < Words.Count; i++)
      {
        WordInfo wordToCheck = Words[i];

        if (word != wordToCheck)
        {
          if (word.ConflictsWithWord(wordToCheck))
          {
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

    private IEnumerable<WordInfo> Twist(List<WordInfo> words)
    {
      Random rng = new Random((int)(DateTime.Now - DateTime.Today).TotalMilliseconds);

      return words.OrderByDescending(wrd => wrd.Text.Length).Select(wrd =>
      {
        if (rng.Next() % 2 == 0)
        {
          wrd.Text = wrd.Text.Reverse();
          wrd.Reversed = true;
        }

        return wrd;
      });
    }

    #endregion
  }
}