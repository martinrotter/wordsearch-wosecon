namespace WordSearchGenerator.Desktop.Models
{
  public sealed record MonteCarloProgress(
    int ActiveAttemptCount,
    int FinishedAttemptCount,
    int PlacementFailedAttemptCount,
    int MessageCapacityRejectedAttemptCount,
    int AmbiguityRejectedAttemptCount,
    long CompletedCandidateCount,
    long MessageCapacityRejectionCount,
    long AmbiguousBoardRejectionCount,
    int CancelledAttemptCount,
    int TotalAttemptCount,
    int PlacedWordCount,
    int FurthestPlacedWordCount,
    int TotalWordCount,
    long TestedPositions,
    long Backtrackings,
    TimeSpan Elapsed);
}
