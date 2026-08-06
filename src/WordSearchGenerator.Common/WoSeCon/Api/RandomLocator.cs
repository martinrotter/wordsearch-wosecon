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

    public delegate IReadOnlyList<DirectedLocation> LocationOrderer(IReadOnlyList<DirectedLocation> locations);

    public RandomLocator(int rowCount, int columnCount, LocationOrderer orderer = null)
    {
      RowCount = rowCount;
      ColumnCount = columnCount;

      foreach (DirectedLocation.LocationDirection d in Enum.GetValues<DirectedLocation.LocationDirection>())
      {
        for (int column = 0; column < ColumnCount; column++)
        for (int row = 0; row < RowCount; row++)
        {
          if (!(
                (d == DirectedLocation.LocationDirection.LeftToRight &&
                 column == ColumnCount - 1) ||
                (d == DirectedLocation.LocationDirection.RightToLeft &&
                 column == 0) ||
                (d == DirectedLocation.LocationDirection.TopBottom &&
                 row == RowCount - 1) ||
                (d == DirectedLocation.LocationDirection.BottomTop &&
                 row == 0) ||
                (d == DirectedLocation.LocationDirection.LeftTopRightBottom &&
                 (row == RowCount - 1 || column == ColumnCount - 1)) ||
                (d == DirectedLocation.LocationDirection.LeftBottomRightTop &&
                 (row == 0 || column == ColumnCount - 1)) ||
                (d == DirectedLocation.LocationDirection.RightTopLeftBottom &&
                 (row == RowCount - 1 || column == 0)) ||
                (d == DirectedLocation.LocationDirection.RightBottomLeftTop &&
                 (row == 0 || column == 0))))
          {
            DirectedLocation dl = new DirectedLocation
            {
              Column = column,
              Row = row,
              Direction = d
            };

            AddAvailableLocation(dl);
          }
        }
      }

      if (orderer != null)
      {
        // Shuffle the list.
        AvailableLocations = orderer(AvailableLocations).ToList();
      }
    }

    private RandomLocator(
      int rowCount,
      int columnCount,
      List<DirectedLocation> availableLocations)
    {
      RowCount = rowCount;
      ColumnCount = columnCount;
      AvailableLocations = availableLocations;
    }

    #endregion

    #region Other Stuff

    public void AddAvailableLocation(DirectedLocation location)
    {
      AvailableLocations.Add(location);
    }

    public RandomLocator Minus(List<DirectedLocation> locations)
    {
      HashSet<DirectedLocation> locationsToRemove = locations.ToHashSet();

      List<DirectedLocation> remainingLocations = AvailableLocations
        .Where(location => !locationsToRemove.Contains(location))
        .ToList();

      return new RandomLocator(RowCount, ColumnCount, remainingLocations);
    }

    public void RemoveAvailableLocation(DirectedLocation location)
    {
      AvailableLocations.Remove(location);
    }

    #endregion
  }
}
