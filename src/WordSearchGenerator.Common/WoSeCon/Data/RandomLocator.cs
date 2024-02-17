namespace WordSearchGenerator.Common.WoSeCon.Data;

public class RandomLocator
{
  #region Properties

  public int ColumnCount { get; }

  public DirectedLocation this[int i]
  {
    get => Locations[i];
    set => Locations[i] = value;
  }

  public int RowCount { get; }

  public int Size => (Locations?.Count).GetValueOrDefault(0);

  private List<DirectedLocation> Locations { get; } = new();

  #endregion

  #region Constructors

  public RandomLocator(int rowCount, int columnCount)
  {
    RowCount = rowCount;
    ColumnCount = columnCount;

    foreach (var d in new[]
             {
               DirectedLocation.LocationDirection.Horizontal,
               DirectedLocation.LocationDirection.Vertical
             })
      for (var c = 0; c < ColumnCount; c++)
      for (var l = 0; l < RowCount; l++)
        if (!((d == DirectedLocation.LocationDirection.Horizontal && c == ColumnCount - 1) ||
              (d == DirectedLocation.LocationDirection.Vertical && l == RowCount - 1)))
        {
          var dL = new DirectedLocation
          {
            Column = c,
            Row = l,
            Direction = d
          };

          Add(dL);
        }

    // Shuffle the list.
    var rng = new Random((int)(DateTime.Now - DateTime.Today).TotalMilliseconds);
    Locations = Locations.OrderBy(_ => rng.Next()).ToList();
  }

  #endregion

  #region Other Stuff

  public void Add(DirectedLocation location)
  {
    Locations.Add(location);
  }

  public RandomLocator Minus(List<DirectedLocation> visitedLocations)
  {
    var loc = new RandomLocator(RowCount, ColumnCount);

    foreach (var visitedLocation in visitedLocations) loc.Remove(visitedLocation);

    return loc;
  }

  public void Remove(DirectedLocation location)
  {
    Locations.Remove(location);
  }

  #endregion
}