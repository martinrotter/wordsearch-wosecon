using WordSearchGenerator.Desktop.Models;

namespace WordSearchGenerator.Desktop.Services
{
  public interface IPuzzleGenerator
  {
    Task<GenerationResult> GenerateAsync(
      PuzzleDefinition definition,
      IProgress<MonteCarloProgress>? progress,
      CancellationToken cancellationToken);
  }
}
