namespace Wose.Common.WoSeCon.Api
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
    } = [];

    public string Text
    {
      get;
      set;
    }

    /// <summary>
    ///   Should start from 1 for a collection of words.
    /// </summary>
    public int WordNumber
    {
      get;
      set;
    }

    #endregion

    #region Interface Implementations

    public object Clone()
    {
      var wrd = new WordInfo();

      wrd.Text = (string)Text.Clone();
      wrd.WordNumber = WordNumber;
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

    public bool ConflictsWithWord(
      IReadOnlyList<DirectedLocation> wordLocations,
      WordInfo otherWord,
      bool quizMode)
    {
      var otherWordLocations = otherWord.GetAllPlacementLocations(quizMode);

      if (otherWordLocations == null || otherWordLocations.Count == 0)
      {
        return false;
      }

      var firstMyWord = true;

      foreach (var wordLetterLoc in wordLocations)
      {
        var firstOtherWord = true;

        foreach (var otherWordLetterLoc in otherWordLocations)
        {
          if (quizMode &&
              (firstMyWord || firstOtherWord) &&
              wordLetterLoc.Row == otherWordLetterLoc.Row &&
              wordLetterLoc.Column == otherWordLetterLoc.Column)
          {
            return true;
          }

          if (wordLetterLoc.Row == otherWordLetterLoc.Row &&
              wordLetterLoc.Column == otherWordLetterLoc.Column &&
              DirectedLocation.IsSameLine(wordLetterLoc.Direction, otherWordLetterLoc.Direction))
          {
            // Two words share same position of one of their letters
            // with the same direction.
            // Not allowed.
            return true;
          }

          if (wordLetterLoc.Row == otherWordLetterLoc.Row &&
              wordLetterLoc.Column == otherWordLetterLoc.Column &&
              wordLetterLoc.Direction != otherWordLetterLoc.Direction)
          {
            // Two words intersect.
            if (CharAt(otherWordLetterLoc, quizMode) !=
                otherWord.CharAt(otherWordLetterLoc, quizMode))
            {
              return true;
            }
          }

          firstOtherWord = false;
        }

        firstMyWord = false;
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

    public List<DirectedLocation> GetAllPlacementLocations(bool quizMode)
    {
      if (Placement == null)
      {
        return new List<DirectedLocation>(0);
      }

      var placementLength = Text.Length + (quizMode ? 1 : 0);
      var placementLocations = new List<DirectedLocation>(placementLength);

      var row = Placement.Row;
      var column = Placement.Column;

      var tweakRow = Placement.Direction != DirectedLocation.LocationDirection.LeftToRight &&
                     Placement.Direction != DirectedLocation.LocationDirection.RightToLeft;

      var tweakColumn = Placement.Direction != DirectedLocation.LocationDirection.TopBottom &&
                        Placement.Direction != DirectedLocation.LocationDirection.BottomTop;

      var addRow = Placement.Direction == DirectedLocation.LocationDirection.TopBottom ||
                   Placement.Direction == DirectedLocation.LocationDirection.LeftTopRightBottom ||
                   Placement.Direction == DirectedLocation.LocationDirection.RightTopLeftBottom;

      var addColumn = Placement.Direction == DirectedLocation.LocationDirection.LeftToRight ||
                      Placement.Direction == DirectedLocation.LocationDirection.LeftTopRightBottom ||
                      Placement.Direction == DirectedLocation.LocationDirection.LeftBottomRightTop;

      for (var i = 0; i < placementLength; i++)
      {
        var d = new DirectedLocation
        {
          Row = tweakRow ? addRow ? row + i : row - i : row,
          Column = tweakColumn ? addColumn ? column + i : column - i : column,
          Direction = Placement.Direction
        };

        placementLocations.Add(d);
      }

      return placementLocations;
    }

    public override int GetHashCode()
    {
      return Text != null ? Text.GetHashCode() : 0;
    }

    public char CharAt(DirectedLocation location, bool quizMode)
    {
      int placementIndex;

      if (Placement.Direction == DirectedLocation.LocationDirection.LeftToRight ||
          Placement.Direction == DirectedLocation.LocationDirection.RightToLeft)
      {
        var idx = location.Column - Placement.Column;
        placementIndex = idx < 0 ? -idx : idx;
      }
      else
      {
        var idx = location.Row - Placement.Row;
        placementIndex = idx < 0 ? -idx : idx;
      }

      var textIndex = placementIndex - (quizMode ? 1 : 0);

      if (textIndex < 0)
      {
        throw new InvalidOperationException("A quiz question cell does not contain a letter.");
      }

      return Text[textIndex];
    }

    public bool WillFit(
      DirectedLocation location,
      int rowCount,
      int columnCount,
      bool quizMode)
    {
      var placementLength = Text.Length + (quizMode ? 1 : 0);

      switch (location.Direction)
      {
        case DirectedLocation.LocationDirection.RightTopLeftBottom:
          return location.Column - placementLength >= -1 &&
                 location.Row + placementLength <= rowCount;

        case DirectedLocation.LocationDirection.RightBottomLeftTop:
          return location.Column - placementLength >= -1 &&
                 location.Row - placementLength >= -1;

        case DirectedLocation.LocationDirection.LeftTopRightBottom:
          return location.Row + placementLength <= rowCount &&
                 location.Column + placementLength <= columnCount;

        case DirectedLocation.LocationDirection.LeftBottomRightTop:
          return location.Row - placementLength >= -1 &&
                 location.Column + placementLength <= columnCount;

        case DirectedLocation.LocationDirection.LeftToRight:
          return location.Column + placementLength <= columnCount;

        case DirectedLocation.LocationDirection.RightToLeft:
          return location.Column - placementLength >= -1;

        case DirectedLocation.LocationDirection.TopBottom:
          return location.Row + placementLength <= rowCount;

        case DirectedLocation.LocationDirection.BottomTop:
        default:
          return location.Row - placementLength >= -1;
      }
    }

    #endregion
  }
}
