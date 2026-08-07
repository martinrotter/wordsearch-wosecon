namespace WordSearchGenerator.Desktop.Models.Settings
{
  public sealed record ApplicationSettings(
    int FormatVersion,
    string UiCulture);
}