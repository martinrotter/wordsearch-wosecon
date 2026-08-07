using System.Windows;
using WordSearchGenerator.Desktop.Localization;
using WordSearchGenerator.Desktop.Models.Settings;
using WordSearchGenerator.Desktop.Services.Settings;
using WordSearchGenerator.Desktop.Views.Dialogs;

namespace WordSearchGenerator.Desktop.Views
{
  public partial class MainWindow
  {
    #region Other Stuff

    private void SettingsOnClick(object sender, RoutedEventArgs e)
    {
      var dialog = new SettingsWindow(_applicationSettings.UiCulture)
      {
        Owner = this
      };

      if (dialog.ShowDialog() != true)
      {
        return;
      }

      var selectedCulture = dialog.ViewModel.SelectedLanguage.CultureName;

      if (string.Equals(
            selectedCulture,
            _applicationSettings.UiCulture,
            StringComparison.OrdinalIgnoreCase))
      {
        return;
      }

      var settings = new ApplicationSettings(
        JsonApplicationSettingsService.CurrentFormatVersion,
        selectedCulture);

      try
      {
        _applicationSettingsService.Save(settings);
        _applicationSettings = settings;
        MessageBox.Show(
          this,
          AppStrings.Get("LanguageRestartMessage"),
          AppStrings.Get("LanguageRestartTitle"),
          MessageBoxButton.OK,
          MessageBoxImage.Information);
      }
      catch (Exception exception)
      {
        MessageBox.Show(
          this,
          $"{AppStrings.Get("SettingsSaveFailed")}\n\n{exception.Message}",
          AppStrings.Get("SettingsSaveFailedTitle"),
          MessageBoxButton.OK,
          MessageBoxImage.Error);
      }
    }

    #endregion
  }
}