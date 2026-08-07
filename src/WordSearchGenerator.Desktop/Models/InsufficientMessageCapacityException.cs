namespace WordSearchGenerator.Desktop.Models
{
  public sealed class InsufficientMessageCapacityException : Exception
  {
    #region Properties

    public int AvailableCellCount
    {
      get;
    }

    public int RequiredCellCount
    {
      get;
    }

    #endregion

    #region Constructors

    public InsufficientMessageCapacityException(
      int requiredCellCount,
      int availableCellCount)
      : base(
        $"The constructed board has {availableCellCount} vacant cells, " +
        $"but the secret message requires {requiredCellCount}.")
    {
      RequiredCellCount = requiredCellCount;
      AvailableCellCount = availableCellCount;
    }

    #endregion
  }
}
