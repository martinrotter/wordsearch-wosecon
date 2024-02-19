using WordSearchGenerator.Common.WoSeCon.Data;

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

    public RandomLocator Locator
    {
      get;
      set;
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
      set;
    }

    #endregion

    #region Constructors

    public WoSeCon(List<WordInfo> words, int rowCount, int columnCount)
    {
      RowCount = rowCount;
      ColumnCount = columnCount;
      Words = Twist(words).ToList();

      Locator = new RandomLocator(RowCount, ColumnCount);
    }

    #endregion

    #region Other Stuff

    public int Construct()
    {
      var backtrackings = 0;
      var cWordIndex = 0;
      var cWord = Words[cWordIndex];

      Mode = OperationMode.Forward;

      while (true)
      {
        if (LocateOne(cWord))
        {
          if (cWordIndex == Words.Count - 1)
          {
            break;
          }

          ++cWordIndex;
          cWord = Words[cWordIndex];
          Mode = OperationMode.Forward;
        }
        else
        {
          if (cWordIndex == 0)
          {
            throw new Exception("given words cannot fit into the grid");
          }

          cWord.DeleteTested();
          --cWordIndex;
          backtrackings++;
          cWord = Words[cWordIndex];
          Mode = OperationMode.Backward;
        }
      }

      return backtrackings;
    }

    public bool IsValidPlacement(WordInfo cWord, DirectedLocation dL)
    {
      cWord.Placement = dL;
      var wordLocations = cWord.GetAllLocations();

      foreach (var l in wordLocations)
      {
        if (l.Row > RowCount - 1 || l.Column > ColumnCount - 1)
        {
          cWord.Placement = null;
          return false;
        }
      }

      for (var i = 0; i < Words.Count; i++)
      {
        var wToCheck = Words[i];

        if (cWord != wToCheck)
        {
          if (cWord.Conflicts(wToCheck))
          {
            cWord.Placement = null;
            return false;
          }
        }
      }

      return true;
    }

    public bool LocateOne(WordInfo cWord)
    {
      var tested = cWord.TestedLocations;
      RandomLocator localLocator = null;
      
      if (Mode == OperationMode.Backward)
      {
        var l = RandomLocator.Minus(RowCount, ColumnCount, tested);
        var dL = cWord.Placement;
        Locator.Add(dL);
        cWord.AddToTested();
        localLocator = l;
      }
      else
      {
        localLocator = Locator;
      }

      var locationIndex = 0;

      while (locationIndex < localLocator.Size)
      {
        var suitableLocation = localLocator[locationIndex];

        if (IsValidPlacement(cWord, suitableLocation))
        {
          Locator.Remove(suitableLocation);
          return true;
        }

        locationIndex++;
      }

      return false;
    }

    private IEnumerable<WordInfo> Twist(List<WordInfo> words)
    {
      var rng = new Random((int)(DateTime.Now - DateTime.Today).TotalMilliseconds);

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