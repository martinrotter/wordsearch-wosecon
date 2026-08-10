namespace Wose.Desktop.Models.Settings
{
  public sealed record ApplicationSettings(
    string UiCulture,
    MainWindowPlacement? MainWindowPlacement = null);

  public sealed record MainWindowPlacement(
    double Left,
    double Top,
    double Width,
    double Height,
    bool IsMaximized,
    double EditorPaneWidth);
}
