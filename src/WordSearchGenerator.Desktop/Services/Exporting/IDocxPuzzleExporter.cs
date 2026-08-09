using WordSearchGenerator.Desktop.Models.Rendering;

namespace WordSearchGenerator.Desktop.Services.Exporting
{
  public interface IDocxPuzzleExporter
  {
    #region Other Stuff

    Task ExportAsync(
      string path,
      BoardRenderModel model,
      BoardPreviewMode previewMode,
      CancellationToken cancellationToken = default);

    #endregion
  }
}