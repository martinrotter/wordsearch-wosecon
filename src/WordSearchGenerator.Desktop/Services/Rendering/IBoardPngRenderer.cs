using WordSearchGenerator.Desktop.Models.Rendering;

namespace WordSearchGenerator.Desktop.Services.Rendering
{
  public interface IBoardPngRenderer
  {
    #region Other Stuff

    byte[] Render(
      BoardRenderModel model,
      BoardPreviewMode previewMode,
      int targetLongSide = 2400);

    #endregion
  }
}