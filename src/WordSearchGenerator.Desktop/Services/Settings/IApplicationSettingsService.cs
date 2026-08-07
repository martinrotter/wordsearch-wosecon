using WordSearchGenerator.Desktop.Models.Settings;

namespace WordSearchGenerator.Desktop.Services.Settings
{
  public interface IApplicationSettingsService
  {
    #region Other Stuff

    ApplicationSettings Load();

    void Save(ApplicationSettings settings);

    #endregion
  }
}