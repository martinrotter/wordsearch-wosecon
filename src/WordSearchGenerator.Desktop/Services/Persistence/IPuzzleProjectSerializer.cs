using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Models.Persistence;

namespace WordSearchGenerator.Desktop.Services.Persistence
{
  public interface IPuzzleProjectSerializer
  {
    #region Other Stuff

    Task<PuzzleProject> LoadAsync(
      string path,
      CancellationToken cancellationToken = default);

    Task SaveAsync(
      string path,
      PuzzleDefinition definition,
      GenerationResult? generatedResult,
      CancellationToken cancellationToken = default);

    #endregion
  }
}