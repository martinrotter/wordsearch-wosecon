using WordSearchGenerator.Desktop.Localization;
using WordSearchGenerator.Desktop.Models.Settings;

namespace WordSearchGenerator.Desktop.ViewModels
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
      Languages =
      [
        new LanguageOption(
          ApplicationCulture.EnglishCultureName,
          AppStrings.Get("LanguageEnglish")),
        new LanguageOption(
          ApplicationCulture.CzechCultureName,
          AppStrings.Get("LanguageCzech"))
      ];

      _selectedLanguage = Languages.FirstOrDefault(language => string.Equals(
                            language.CultureName,
                            cultureName,
                            StringComparison.OrdinalIgnoreCase)) ??
                          Languages[0];
    }

    #endregion
  }
}