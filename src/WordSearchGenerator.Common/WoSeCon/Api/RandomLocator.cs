namespace WordSearchGenerator.Common.WoSeCon.Api
{
  public class RandomLocator
  {
    #region Properties

    public int ColumnCount
    {
      get;
    }

    public DirectedLocation this[int i]
    {
      get => AvailableLocations[i];
      set => AvailableLocations[i] = value;
    }

    public int RowCount
    {
      get;
    }

    public int Size
    {
      get => (AvailableLocations?.Count).GetValueOrDefault(0);
    }

    private List<DirectedLocation> AvailableLocations
    {
      get;
    } = new List<DirectedLocation>();

    #endregion

    #region Constructors

    public RandomLocator(int rowCount, int columnCount)
    {
      RowCount = rowCount;
      ColumnCount = columnCount;

      foreach (DirectedLocation.LocationDirection d in new[] { DirectedLocation.LocationDirection.Horizontal, DirectedLocation.LocationDirection.Vertical })
      {
        for (int c = 0; c < ColumnCount; c++)
        for (int l = 0; l < RowCount; l++)
        {
          if (!((d == DirectedLocation.LocationDirection.Horizontal && c == ColumnCount - 1) || (d == DirectedLocation.LocationDirection.Vertical && l == RowCount - 1)))
          {
            DirectedLocation dl = new DirectedLocation { Column = c, Row = l, Direction = d };
            AddAvailableLocation(dl);
          }
        }
      }

      // Shuffle the list.
      Random rng = new Random((int)(DateTime.Now - DateTime.Today).TotalMilliseconds);
      AvailableLocations = AvailableLocations.OrderBy(_ => rng.Next()).ToList();
    }

    #endregion

    #region Other Stuff

    public void AddAvailableLocation(DirectedLocation location)
    {
      AvailableLocations.Add(location);
    }

    public static RandomLocator GetWithoutLocations(
      int rowCount,
      int columnCount,
      List<DirectedLocation> visitedLocations)
    {
      RandomLocator loc = new RandomLocator(rowCount, columnCount);

      foreach (DirectedLocation visitedLocation in visitedLocations)
      {
        loc.RemoveAvailableLocation(visitedLocation);
      }

      return loc;
    }

    public void RemoveAvailableLocation(DirectedLocation location)
    {
      AvailableLocations.Remove(location);
    }

    #endregion
  }
}