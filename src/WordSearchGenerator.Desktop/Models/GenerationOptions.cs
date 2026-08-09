using WordSearchGenerator.Desktop.Localization;

namespace WordSearchGenerator.Desktop.Models
{
  public sealed class GenerationOptions
  {
    #region Constants

    public const int MaximumAttemptTimeSecondsLimit = 86400;

    #endregion

    #region Properties

    public int MaximumAttemptTimeSeconds
    {
      get;
    }

    public int ParallelAttempts
    {
      get;
    }

    #endregion

    #region Constructors

    public GenerationOptions(
      int parallelAttempts,
      int maximumAttemptTimeSeconds)
    {
      if (parallelAttempts <= 0)
      {
        throw new ArgumentOutOfRangeException(
          nameof(parallelAttempts),
          parallelAttempts,
          AppStrings.Get("ParallelAttemptsPositive"));
      }

      if (maximumAttemptTimeSeconds < 0 ||
          maximumAttemptTimeSeconds > MaximumAttemptTimeSecondsLimit)
      {
        throw new ArgumentOutOfRangeException(
          nameof(maximumAttemptTimeSeconds),
          maximumAttemptTimeSeconds,
          AppStrings.Get("MaximumAttemptTimeRange"));
      }

      ParallelAttempts = parallelAttempts;
      MaximumAttemptTimeSeconds = maximumAttemptTimeSeconds;
    }

    #endregion
  }
}
