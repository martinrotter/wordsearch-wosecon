using WordSearchGenerator.Desktop.Models;

namespace WordSearchGenerator.Desktop.Services
{
  public interface IPuzzleGenerator
  {
    #region Other Stuff

    Task<GenerationResult> GenerateAsync(
      PuzzleDefinition definition,
      IProgress<MonteCarloProgress>? progress,
      CancellationToken cancellationToken);

    #endregion
  }
}