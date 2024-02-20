namespace WordSearchGenerator.Common.WoSeCon.Api
{
  public class WordInfo : IEquatable<WordInfo>, ICloneable
  {
    #region Properties

    public DirectedLocation Placement
    {
      get;
      set;
    }

    public List<DirectedLocation> TestedLocations
    {
      get;
      set;
    } = new List<DirectedLocation>();

    public string Text
    {
      get;
      set;
    }

    #endregion

    #region Interface Implementations

    public object Clone()
    {
      WordInfo wrd = new WordInfo();

      wrd.Text = (string)Text.Clone();

      if (Placement != null)
      {
        wrd.Placement = (DirectedLocation)Placement.Clone();
      }

      if (TestedLocations != null)
      {
        wrd.TestedLocations = TestedLocations.CloneList();
      }

      return wrd;
    }

    public bool Equals(WordInfo other)
    {
      if (ReferenceEquals(null, other))
      {
        return false;
      }

      if (ReferenceEquals(this, other))
      {
        return true;
      }

      return Text == other.Text;
    }

    #endregion

    #region Other Stuff

    public static bool operator ==(WordInfo left, WordInfo right)
    {
      return Equals(left, right);
    }

    public static bool operator !=(WordInfo left, WordInfo right)
    {
      return !Equals(left, right);
    }

    public void MarkAsTestedOnPlacement()
    {
      TestedLocations.Add(Placement);
      Placement = null;
    }

    public bool ConflictsWithWord(WordInfo otherWord)
    {
      List<DirectedLocation> otherWordLocations = otherWord.GetAllLetterLocations();

      if (otherWordLocations == null || otherWordLocations.Count == 0)
      {
        return false;
      }

      List<DirectedLocation> wordLocations = GetAllLetterLocations();

      foreach (DirectedLocation wordLetter in wordLocations)
      foreach (DirectedLocation otherWordLetter in otherWordLocations)
      {
        if (wordLetter == otherWordLetter)
        {
          // Two words have same letter in same position and direction.
          // Not allowed.
          return true;
        }

        if (wordLetter.Row == otherWordLetter.Row &&
            wordLetter.Column == otherWordLetter.Column &&
            wordLetter.Direction != otherWordLetter.Direction)
        {
          // Two words intersect.
          if (CharAt(otherWordLetter) != otherWord.CharAt(otherWordLetter))
          {
            return true;
          }
        }
      }

      return false;
    }

    public void ClearTestedLocations()
    {
      TestedLocations.Clear();
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj))
      {
        return false;
      }

      if (ReferenceEquals(this, obj))
      {
        return true;
      }

      if (obj.GetType() != GetType())
      {
        return false;
      }

      return Equals((WordInfo)obj);
    }

    /*
    public Dictionary<Tuple<DirectedLocation, int>, List<DirectedLocation>> KnownLocations
    {
      get;
    } = new Dictionary<Tuple<DirectedLocation, int>, List<DirectedLocation>>();
    */

    public List<DirectedLocation> GetAllLetterLocations()
    {
      if (Placement == null)
      {
        return new List<DirectedLocation>(0);
      }

      List<DirectedLocation> letterLocations = new List<DirectedLocation>(Text.Length);

      /*
      var tpl = new Tuple<DirectedLocation, int>(Placement, Text.Length);

      if (KnownLocations.TryGetValue(tpl, out List<DirectedLocation> locations))
      {
        return locations;
      }
      */

      int row = Placement.Row;
      int column = Placement.Column;

      bool tweakRow = Placement.Direction != DirectedLocation.LocationDirection.LeftToRight &&
                      Placement.Direction != DirectedLocation.LocationDirection.RightToLeft;

      bool tweakColumn = Placement.Direction != DirectedLocation.LocationDirection.TopBottom &&
                         Placement.Direction != DirectedLocation.LocationDirection.BottomTop;

      bool addRow = Placement.Direction == DirectedLocation.LocationDirection.TopBottom ||
                    Placement.Direction == DirectedLocation.LocationDirection.LeftTopRightBottom ||
                    Placement.Direction == DirectedLocation.LocationDirection.RightTopLeftBottom;

      bool addColumn = Placement.Direction == DirectedLocation.LocationDirection.LeftToRight ||
                       Placement.Direction == DirectedLocation.LocationDirection.LeftTopRightBottom ||
                       Placement.Direction == DirectedLocation.LocationDirection.LeftBottomRightTop;

      for (int i = 0; i < Text.Length; i++)
      {
        DirectedLocation d = new DirectedLocation
        {
          Row = tweakRow ? addRow ? row + i : row - i : row,
          Column = tweakColumn ? addColumn ? column + i : column - i : column,
          Direction = Placement.Direction
        };

        letterLocations.Add(d);
      }

      //KnownLocations[tpl] = rVal;

      return letterLocations;
    }

    public override int GetHashCode()
    {
      return Text != null ? Text.GetHashCode() : 0;
    }

    public char CharAt(DirectedLocation location)
    {
      if (Placement.Direction == DirectedLocation.LocationDirection.LeftToRight ||
          Placement.Direction == DirectedLocation.LocationDirection.RightToLeft)
      {
        int idx = location.Column - Placement.Column;
        return Text[idx < 0 ? -idx : idx];
      }
      else
      {
        int idx = location.Row - Placement.Row;
        return Text[idx < 0 ? -idx : idx];
      }
    }

    public string ToString(int longestWord, bool showSolution)
    {
      string str = Text.PadRight(longestWord + 2);

      if (showSolution)
      {
        str += $"{Placement.Row}:{Placement.Column} {Placement.Direction}" + Environment.NewLine;
      }

      return str;
    }

    public bool WillFit(DirectedLocation location, int rowCount, int columnCount)
    {
      switch (location.Direction)
      {
        case DirectedLocation.LocationDirection.RightTopLeftBottom:
          return location.Column - Text.Length >= -1 &&
                 location.Row + Text.Length <= rowCount;

        case DirectedLocation.LocationDirection.RightBottomLeftTop:
          return location.Column - Text.Length >= -1 &&
                 location.Row - Text.Length >= -1;

        case DirectedLocation.LocationDirection.LeftTopRightBottom:
          return location.Row + Text.Length <= rowCount &&
                 location.Column + Text.Length <= columnCount;

        case DirectedLocation.LocationDirection.LeftBottomRightTop:
          return location.Row - Text.Length >= -1 &&
                 location.Column + Text.Length <= columnCount;

        case DirectedLocation.LocationDirection.LeftToRight:
          return location.Column + Text.Length <= columnCount;

        case DirectedLocation.LocationDirection.RightToLeft:
          return location.Column - Text.Length >= -1;

        case DirectedLocation.LocationDirection.TopBottom:
          return location.Row + Text.Length <= rowCount;

        case DirectedLocation.LocationDirection.BottomTop:
        default:
          return location.Row - Text.Length >= -1;
      }
    }

    #endregion
  }
}