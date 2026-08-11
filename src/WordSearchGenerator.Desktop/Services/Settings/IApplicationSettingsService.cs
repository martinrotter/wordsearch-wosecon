using Wose.Desktop.Models.Settings;

namespace Wose.Desktop.Services.Settings
{
  public interface IApplicationSettingsService
  {
    #region Other Stuff

    ApplicationSettings Load();

    void Save(ApplicationSettings settings);

    #endregion
  }
}
