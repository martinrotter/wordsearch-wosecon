using WordSearchGenerator.Desktop.Localization;

namespace WordSearchGenerator.Desktop.Models
{
  public sealed class MonteCarloGenerationException : Exception
  {
    #region Properties

    public int AttemptCount
    {
      get;
    }

    public long AmbiguousBoardRejectionCount
    {
      get;
    }

    public int AmbiguityRejectedAttemptCount
    {
      get;
    }

    public long CompletedCandidateCount
    {
      get;
    }

    public long MessageCapacityRejectionCount
    {
      get;
    }

    public int MessageCapacityRejectedAttemptCount
    {
      get;
    }

    public int PlacementFailedAttemptCount
    {
      get;
    }

    #endregion

    #region Constructors

    public MonteCarloGenerationException(
      int attemptCount,
      int placementFailedAttemptCount,
      int messageCapacityRejectedAttemptCount,
      int ambiguityRejectedAttemptCount,
      long completedCandidateCount,
      long messageCapacityRejectionCount,
      long ambiguousBoardRejectionCount)
      : base(CreateMessage(
        attemptCount,
        placementFailedAttemptCount,
        completedCandidateCount,
        messageCapacityRejectionCount,
        ambiguousBoardRejectionCount))
    {
      AttemptCount = attemptCount;
      PlacementFailedAttemptCount = placementFailedAttemptCount;
      MessageCapacityRejectedAttemptCount = messageCapacityRejectedAttemptCount;
      AmbiguityRejectedAttemptCount = ambiguityRejectedAttemptCount;
      CompletedCandidateCount = completedCandidateCount;
      MessageCapacityRejectionCount = messageCapacityRejectionCount;
      AmbiguousBoardRejectionCount = ambiguousBoardRejectionCount;
    }

    #endregion

    #region Other Stuff

    private static string CreateMessage(
      int attemptCount,
      int placementFailedAttemptCount,
      long completedCandidateCount,
      long messageCapacityRejectionCount,
      long ambiguousBoardRejectionCount)
    {
      return AppStrings.Format(
        "MonteCarloFailure",
        attemptCount,
        placementFailedAttemptCount,
        completedCandidateCount,
        messageCapacityRejectionCount,
        ambiguousBoardRejectionCount);
    }

    #endregion
  }
}