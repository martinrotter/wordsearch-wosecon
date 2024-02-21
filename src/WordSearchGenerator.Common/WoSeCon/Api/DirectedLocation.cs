namespace WordSearchGenerator.Common.WoSeCon.Api
{
  public class DirectedLocation : IEquatable<DirectedLocation>, ICloneable
  {
    #region Enums

    public enum LocationDirection
    {
      // Horizontal →.
      LeftToRight = 1,

      // Horizontal ←.
      RightToLeft = 10,

      // Vertical ↓.
      TopBottom = 2,

      // Vertical ↑.
      BottomTop = 9,

      // Diagonal ↘.
      LeftTopRightBottom = 3,

      // Diagonal ↖.
      RightBottomLeftTop = 8,

      // Diagonal ↗.
      LeftBottomRightTop = 4,

      // Diagonal ↙.
      RightTopLeftBottom = 7
    }

    #endregion

    #region Properties

    /// <summary>
    /// If both directions lie in one "line", then returns true.
    /// </summary>
    public static bool IsSameLine(LocationDirection d1, LocationDirection d2)
    {
      return d1 == d2 || (int)d1 + (int)d2 == 11;
    }

    public int Column
    {
      get;
      set;
    }

    public LocationDirection Direction
    {
      get;
      set;
    } = LocationDirection.LeftToRight;

    public int Row
    {
      get;
      set;
    }

    #endregion

    #region Interface Implementations

    public object Clone()
    {
      DirectedLocation loc = new DirectedLocation();

      loc.Column = Column;
      loc.Row = Row;
      loc.Direction = Direction;

      return loc;
    }

    public bool Equals(DirectedLocation other)
    {
      if (ReferenceEquals(null, other))
      {
        return false;
      }

      if (ReferenceEquals(this, other))
      {
        return true;
      }

      return Column == other.Column && Direction == other.Direction && Row == other.Row;
    }

    #endregion

    #region Other Stuff

    public static bool operator ==(DirectedLocation left, DirectedLocation right)
    {
      return Equals(left, right);
    }

    public static bool operator !=(DirectedLocation left, DirectedLocation right)
    {
      return !Equals(left, right);
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

      return Equals((DirectedLocation)obj);
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Column, (int)Direction, Row);
    }

    #endregion
  }
}