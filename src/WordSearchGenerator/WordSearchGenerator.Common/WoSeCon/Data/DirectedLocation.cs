namespace WordSearchGenerator.Common.WoSeCon.Data
{
  public class DirectedLocation
  {
    #region Enumy

    public enum LocationDirection
    {
      Horizontal = 0,
      Vertical = 1
    }

    #endregion

    #region Vlastnosti

    public int Column
    {
      get;
      set;
    } = 0;

    public LocationDirection Direction
    {
      get;
      set;
    } = LocationDirection.Horizontal;

    public int Row
    {
      get;
      set;
    } = 0;

    #endregion
  }
}