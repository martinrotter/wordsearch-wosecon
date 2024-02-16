namespace WordSearchGenerator.Common.WoSeCon.Data
{
  public class WordInfo : IEquatable<WordInfo>
  {
    #region Vlastnosti

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
    } = null;

    #endregion

    #region Implementace rozhraní

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

    #region Metody

    public static bool operator ==(WordInfo left, WordInfo right)
    {
      return Equals(left, right);
    }

    public static bool operator !=(WordInfo left, WordInfo right)
    {
      return !Equals(left, right);
    }

    public void AddToTested()
    {
      TestedLocations.Add(Placement);
      Placement = null;
    }

    public bool Conflicts(WordInfo otherWord)
    {
      List<DirectedLocation> w2Ls = otherWord.GetAllLocations();

      if (w2Ls == null || w2Ls.Count == 0)
      {
        return false;
      }

      List<DirectedLocation> w1Ls = GetAllLocations();

      foreach (DirectedLocation l1 in w1Ls)
      {
        foreach (DirectedLocation l2 in w2Ls)
        {
          if (l1 == l2)
          {
            return true;
          }

          if (l1.Row == l2.Row && l1.Column == l2.Column && l1.Direction != l2.Direction)
          {
            if (CharAt(l2) != otherWord.CharAt(l2))
            {
              return true;
            }
          }
        }
      }

      return false;
    }

    public void DeleteTested()
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

    public List<DirectedLocation> GetAllLocations()
    {
      if (Placement == null)
      {
        return new List<DirectedLocation>(0);
      }

      List<DirectedLocation> rVal = new List<DirectedLocation>(Text.Length);

      /*
      var tpl = new Tuple<DirectedLocation, int>(Placement, Text.Length);

      if (KnownLocations.ContainsKey(tpl))
      {
        return KnownLocations[tpl];
      }
      */

      int line = Placement.Row;
      int clmn = Placement.Column;

      if (Placement.Direction == DirectedLocation.LocationDirection.Horizontal)
      {
        for (int i = 0; i < Text.Length; i++)
        {
          DirectedLocation d = new DirectedLocation
          {
            Row = line,
            Column = clmn + i,
            Direction = DirectedLocation.LocationDirection.Horizontal
          };

          rVal.Add(d);
        }
      }
      else
      {
        for (int i = 0; i < Text.Length; i++)
        {
          DirectedLocation d = new DirectedLocation
          {
            Row = line + i,
            Column = clmn,
            Direction = DirectedLocation.LocationDirection.Vertical
          };

          rVal.Add(d);
        }
      }

      //KnownLocations[tpl] = rVal;

      return rVal;
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
      else
      {
        return Text[location.Row - Placement.Row];
      }
    }

    public string ToString(int longestWord, bool showSolution)
    {
      var str = $"{NormalizedText}{(showSolution && Reversed ? "*" : string.Empty)}".PadRight(longestWord + 2);

      if (showSolution)
      {
        str += $"{Placement.Row}:{Placement.Column} {Placement.Direction}" + Environment.NewLine;
      }

      return str;
    }

    #endregion
  }
}