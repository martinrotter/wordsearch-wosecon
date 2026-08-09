using WordSearchGenerator.Desktop.Models.Rendering;

namespace WordSearchGenerator.Desktop.Services.Rendering
{
  public interface IBoardHtmlRenderer
  {
    #region Other Stuff

    string Render(
      BoardRenderModel model,
      BoardPreviewMode previewMode,
      string styleId);

    #endregion
  }
}
