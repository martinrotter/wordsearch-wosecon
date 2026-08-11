using Wose.Desktop.Models.Rendering;

namespace Wose.Desktop.Services.Exporting
{
  public interface IDocxPuzzleExporter
  {
    #region Other Stuff

    Task ExportAsync(
      string path,
      BoardRenderModel model,
      BoardPreviewMode previewMode,
      byte[] boardPng,
      CancellationToken cancellationToken = default);

    #endregion
  }
}
