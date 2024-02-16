namespace WordSearchGenerator.Common.WoSeCon.Data
{
  public class WordInfo
  {
    #region Vlastnosti

    public override string ToString()
    {
      return $"{Text} - {Placement.Row}:{Placement.Column} {Placement.Direction}";
    }

    public DirectedLocation Placement
    {
      get;
      set;
    } = null;

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

    #region Metody

    public void AddToTested()
    {
      TestedLocations.Add(Placement);
      Placement = null;
    }

    public bool Conflicts(WordInfo otherWord)
    {
      List<DirectedLocation> w1Ls = GetAllLocations();
      List<DirectedLocation> w2Ls = otherWord.GetAllLocations();

      if (!w2Ls.Any())
      {
        return false;
      }

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

    public List<DirectedLocation> GetAllLocations()
    {
      List<DirectedLocation> rVal = new List<DirectedLocation>();

      if (Placement == null)
      {
        return rVal;
      }

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

      return rVal;
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

    #endregion
  }
}