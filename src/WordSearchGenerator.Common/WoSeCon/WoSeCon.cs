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

    public WoSeCon(List<WordInfo> words, int rowCount, int columnCount)
    {
      RowCount = rowCount;
      ColumnCount = columnCount;
      Words = words;

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

    private bool WillFit(WordInfo word, DirectedLocation location)
    {
      switch (location.Direction)
      {
        case DirectedLocation.LocationDirection.LeftToRight:
          return location.Column + word.Text.Length <= ColumnCount;

        case DirectedLocation.LocationDirection.RightToLeft:
          return location.Column - word.Text.Length >= -1;

        case DirectedLocation.LocationDirection.TopBottom:
          return location.Row + word.Text.Length <= RowCount;

        case DirectedLocation.LocationDirection.BottomTop:
        default:
          return location.Row - word.Text.Length >= -1;
      }
    }

    public bool IsValidPlacement(WordInfo word, DirectedLocation location)
    {
      word.Placement = location;

      if (!WillFit(word, location))
      {
        word.Placement = null;
        return false;
      }
      /*
      word.Placement = location;
      List<DirectedLocation> letterLocations = word.GetAllLetterLocations();

      foreach (DirectedLocation letterLocation in letterLocations)
      {
        if (letterLocation.Row < 0 || letterLocation.Row > RowCount - 1 || letterLocation.Column < 0 || letterLocation.Column > ColumnCount - 1)
        {
          // We are out of matrix bounds.
          word.Placement = null;
          return false;
        }
      }*/

      foreach (WordInfo wordToCheck in Words)
      {
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

    #endregion
  }
}