namespace WordSearchGenerator.Desktop.Models
{
  public sealed record MonteCarloProgress(
    int ActiveAttemptCount,
    int FinishedAttemptCount,
    int PlacementFailureCount,
    int MessageRejectedAttemptCount,
    int CancelledAttemptCount,
    int TotalAttemptCount,
    int PlacedWordCount,
    int FurthestPlacedWordCount,
    int TotalWordCount,
    long TestedPositions,
    long Backtrackings,
    TimeSpan Elapsed);
}