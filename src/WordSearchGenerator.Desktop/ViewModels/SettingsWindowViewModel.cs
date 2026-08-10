using System.Globalization;
using Wose.Desktop.Localization;
using Wose.Desktop.Models.Settings;

namespace Wose.Desktop.ViewModels
{
  public sealed class SettingsWindowViewModel : ViewModelBase
  {
    #region Fields

    private LanguageOption _selectedLanguage;

    #endregion

    #region Properties

    public IReadOnlyList<LanguageOption> Languages
    {
      get;
    }

    public LanguageOption SelectedLanguage
    {
      get => _selectedLanguage;
      set => SetProperty(ref _selectedLanguage, value);
    }

    #endregion

    #region Constructors

    public SettingsWindowViewModel(string cultureName)
    {
      Languages = ApplicationCulture.SupportedCultureNames
        .Select(name => new LanguageOption(
          name,
          CultureInfo.GetCultureInfo(name).NativeName))
        .ToArray();

      _selectedLanguage = Languages.FirstOrDefault(language => string.Equals(
                            language.CultureName,
                            cultureName,
                            StringComparison.OrdinalIgnoreCase)) ??
                          Languages[0];
    }

    #endregion
  }
}
