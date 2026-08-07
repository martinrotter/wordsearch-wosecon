using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Models.Persistence;

namespace WordSearchGenerator.Desktop.Services.Persistence
{
  public interface IPuzzleProjectSerializer
  {
    Task<PuzzleProject> LoadAsync(
      string path,
      CancellationToken cancellationToken = default);

    Task SaveAsync(
      string path,
      PuzzleDefinition definition,
      GenerationResult? generatedResult,
      CancellationToken cancellationToken = default);
  }
}
