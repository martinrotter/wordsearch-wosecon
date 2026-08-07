using WordSearchGenerator.Desktop.Localization;

namespace WordSearchGenerator.Desktop.Models
{
  public sealed class GenerationOptions
  {
    #region Properties

    public int ParallelAttempts
    {
      get;
    }

    #endregion

    #region Constructors

    public GenerationOptions(int parallelAttempts)
    {
      if (parallelAttempts <= 0)
      {
        throw new ArgumentOutOfRangeException(
          nameof(parallelAttempts),
          parallelAttempts,
          AppStrings.Get("ParallelAttemptsPositive"));
      }

      ParallelAttempts = parallelAttempts;
    }

    #endregion
  }
}