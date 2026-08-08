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

    public int MessageRejectedAttemptCount
    {
      get;
    }

    public int PlacementFailureCount
    {
      get;
    }

    #endregion

    #region Constructors

    public MonteCarloGenerationException(
      int attemptCount,
      int placementFailureCount,
      int messageRejectedAttemptCount,
      long ambiguousBoardRejectionCount)
      : base(CreateMessage(
        attemptCount,
        placementFailureCount,
        messageRejectedAttemptCount,
        ambiguousBoardRejectionCount))
    {
      AttemptCount = attemptCount;
      PlacementFailureCount = placementFailureCount;
      MessageRejectedAttemptCount = messageRejectedAttemptCount;
      AmbiguousBoardRejectionCount = ambiguousBoardRejectionCount;
    }

    #endregion

    #region Other Stuff

    private static string CreateMessage(
      int attemptCount,
      int placementFailureCount,
      int messageRejectedAttemptCount,
      long ambiguousBoardRejectionCount)
    {
      return AppStrings.Format(
        "MonteCarloFailure",
        attemptCount,
        placementFailureCount,
        messageRejectedAttemptCount,
        ambiguousBoardRejectionCount);
    }

    #endregion
  }
}