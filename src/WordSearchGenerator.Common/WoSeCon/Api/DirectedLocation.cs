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

    public static bool IsSameCell(DirectedLocation d1, DirectedLocation d2)
    {
      return d1.Row == d2.Row && d1.Column == d2.Column;
    }

    public static char GetArrowForDirection(DirectedLocation.LocationDirection dir)
    {
      switch (dir)
      {
        case LocationDirection.LeftToRight:
          return '\u2192';

        case LocationDirection.RightToLeft:
          return '\u2190';

        case LocationDirection.TopBottom:
          return '\u2193';

        case LocationDirection.BottomTop:
          return '\u2191';

        case LocationDirection.LeftTopRightBottom:
          return '\u2198';

        case LocationDirection.RightBottomLeftTop:
          return '\u2196';

        case LocationDirection.LeftBottomRightTop:
          return '\u2197';

        case LocationDirection.RightTopLeftBottom:
          return '\u2199';

        default:
          throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
      }
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