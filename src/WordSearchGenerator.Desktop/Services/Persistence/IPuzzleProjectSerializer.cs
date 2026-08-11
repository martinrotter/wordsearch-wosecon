using Wose.Desktop.Models;
using Wose.Desktop.Models.Persistence;

namespace Wose.Desktop.Services.Persistence
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
