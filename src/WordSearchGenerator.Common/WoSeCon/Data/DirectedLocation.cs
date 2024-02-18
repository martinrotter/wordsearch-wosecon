﻿namespace WordSearchGenerator.Common.WoSeCon.Data
{
  public class DirectedLocation : IEquatable<DirectedLocation>, ICloneable
  {
    #region Enums

    public enum LocationDirection
    {
      Horizontal = 0,
      Vertical = 1
    }

    #endregion

    #region Properties

    public int Column
    {
      get;
      set;
    }

    public LocationDirection Direction
    {
      get;
      set;
    } = LocationDirection.Horizontal;

    public int Row
    {
      get;
      set;
    }

    #endregion

    #region Interface Implementations

    public object Clone()
    {
      var loc = new DirectedLocation();

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