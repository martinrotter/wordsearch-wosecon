using WordSearchGenerator.Common.WoSeCon.Api;
using WordSearchGenerator.Desktop.Models;

namespace WordSearchGenerator.Desktop.Services
{
  public interface ISinglePuzzleGenerator
  {
    Task<GenerationResult> GenerateAsync(
      PuzzleDefinition definition,
      IProgress<ConstructionProgress>? progress,
      CancellationToken cancellationToken);
  }
}
