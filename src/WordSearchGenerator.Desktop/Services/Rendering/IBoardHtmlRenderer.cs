using WordSearchGenerator.Desktop.Models.Rendering;

namespace WordSearchGenerator.Desktop.Services.Rendering
{
  public interface IBoardHtmlRenderer
  {
    string Render(
      BoardRenderModel model,
      BoardPreviewMode previewMode);
  }
}
