using System.IO;
using System.Text.Json;
using WordSearchGenerator.Desktop.Localization;
using WordSearchGenerator.Desktop.Models.Settings;

namespace WordSearchGenerator.Desktop.Services.Settings
{
  public sealed class JsonApplicationSettingsService : IApplicationSettingsService
  {
    #region Static Fields

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = true
    };

    #endregion

    #region Fields

    private readonly string _settingsPath;

    #endregion

    #region Constructors

    public JsonApplicationSettingsService(string? settingsPath = null)
    {
      _settingsPath = settingsPath ??
                      Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "WoSeCon",
                        "settings.json");
    }

    #endregion

    #region Interface Implementations

    public ApplicationSettings Load()
    {
      var defaults = CreateDefaultSettings();

      try
      {
        if (!File.Exists(_settingsPath))
        {
          return defaults;
        }

        var json = File.ReadAllText(_settingsPath);
        var settings = JsonSerializer.Deserialize<ApplicationSettings>(
          json,
          JsonOptions);

        if (settings == null ||
            !ApplicationCulture.IsSupported(settings.UiCulture))
        {
          return defaults;
        }

        return settings with
        {
          UiCulture = ApplicationCulture.Normalize(settings.UiCulture)
        };
      }
      catch (Exception exception)
        when (exception is IOException or
                UnauthorizedAccessException or
                JsonException)
      {
        return defaults;
      }
    }

    public void Save(ApplicationSettings settings)
    {
      ArgumentNullException.ThrowIfNull(settings);

      if (!ApplicationCulture.IsSupported(settings.UiCulture))
      {
        throw new ArgumentException(
          AppStrings.Get("UiCultureUnsupported"),
          nameof(settings));
      }

      var fullPath = Path.GetFullPath(_settingsPath);
      var directory = Path.GetDirectoryName(fullPath) ??
                      throw new InvalidOperationException(
                        AppStrings.Get("SettingsPathNoParent"));
      Directory.CreateDirectory(directory);
      var temporaryPath = Path.Combine(
        directory,
        $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
      var normalized = new ApplicationSettings(
        ApplicationCulture.Normalize(settings.UiCulture),
        settings.MainWindowPlacement);

      try
      {
        File.WriteAllText(
          temporaryPath,
          JsonSerializer.Serialize(normalized, JsonOptions));
        File.Move(temporaryPath, fullPath, true);
      }
      finally
      {
        if (File.Exists(temporaryPath))
        {
          File.Delete(temporaryPath);
        }
      }
    }

    #endregion

    #region Other Stuff

    private static ApplicationSettings CreateDefaultSettings()
    {
      return new ApplicationSettings(
        ApplicationCulture.GetInitialCultureName());
    }

    #endregion
  }
}