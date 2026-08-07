namespace WordSearchGenerator.Desktop.Models
{
  public sealed class MonteCarloGenerationException : Exception
  {
    #region Properties

    public int AttemptCount
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
      int messageRejectedAttemptCount)
      : base(CreateMessage(
        attemptCount,
        placementFailureCount,
        messageRejectedAttemptCount))
    {
      AttemptCount = attemptCount;
      PlacementFailureCount = placementFailureCount;
      MessageRejectedAttemptCount = messageRejectedAttemptCount;
    }

    #endregion

    #region Other Stuff

    private static string CreateMessage(
      int attemptCount,
      int placementFailureCount,
      int messageRejectedAttemptCount)
    {
      return $"None of {attemptCount} attempts produced an acceptable board. " +
             $"Placement failures: {placementFailureCount}; " +
             $"message-capacity rejections: {messageRejectedAttemptCount}.";
    }

    #endregion
  }
}
