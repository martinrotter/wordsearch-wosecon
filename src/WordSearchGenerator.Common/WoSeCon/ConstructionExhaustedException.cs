namespace WordSearchGenerator.Common.WoSeCon
{
  public sealed class ConstructionExhaustedException : InvalidOperationException
  {
    #region Constructors

    public ConstructionExhaustedException()
      : base("Given words cannot fit into the grid.")
    {
    }

    #endregion
  }
}
