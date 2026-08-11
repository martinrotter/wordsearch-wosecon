namespace Wose.Common.WoSeCon.Api
{
  /// <summary>
  ///   Immutable snapshot of an in-progress WoSeCon search.
  ///   PlacedWordCount may decrease during backtracking, while
  ///   FurthestPlacedWordCount only increases.
  /// </summary>
  public sealed record ConstructionProgress(
    int PlacedWordCount,
    int FurthestPlacedWordCount,
    int TotalWordCount,
    int CurrentWordNumber,
    long TestedPositions,
    int Backtrackings,
    TimeSpan Elapsed);
}
