namespace WordSearchGenerator.Common.WoSeCon.Api
{
  public class WordInfo : IEquatable<WordInfo>, ICloneable
  {
    #region Properties

    public string NormalizedText
    {
      get => Reversed ? Text.Reverse() : Text;
    }

    public DirectedLocation Placement
    {
      get;
      set;
    }

    public bool Reversed
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

      wrd.Reversed = Reversed;
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
          return true;
        }

        if (wordLetter.Row == otherWordLetter.Row && wordLetter.Column == otherWordLetter.Column && wordLetter.Direction != otherWordLetter.Direction)
        {
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

      int line = Placement.Row;
      int clmn = Placement.Column;

      if (Placement.Direction == DirectedLocation.LocationDirection.Horizontal)
      {
        for (int i = 0; i < Text.Length; i++)
        {
          DirectedLocation d = new DirectedLocation { Row = line, Column = clmn + i, Direction = DirectedLocation.LocationDirection.Horizontal };

          letterLocations.Add(d);
        }
      }
      else
      {
        for (int i = 0; i < Text.Length; i++)
        {
          DirectedLocation d = new DirectedLocation { Row = line + i, Column = clmn, Direction = DirectedLocation.LocationDirection.Vertical };

          letterLocations.Add(d);
        }
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
      if (Placement.Direction == DirectedLocation.LocationDirection.Horizontal)
      {
        return Text[location.Column - Placement.Column];
      }

      return Text[location.Row - Placement.Row];
    }

    public string ToString(int longestWord, bool showSolution)
    {
      string str = $"{NormalizedText}{(showSolution && Reversed ? "*" : string.Empty)}".PadRight(longestWord + 2);

      if (showSolution)
      {
        str += $"{Placement.Row}:{Placement.Column} {Placement.Direction}" + Environment.NewLine;
      }

      return str;
    }

    #endregion
  }
}