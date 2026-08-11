using System.Windows;
using Wose.Desktop.ViewModels;

namespace Wose.Desktop.Views.Dialogs
{
  public partial class SettingsWindow : Window
  {
    #region Properties

    public SettingsWindowViewModel ViewModel
    {
      get;
    }

    #endregion

    #region Constructors

    public SettingsWindow(string cultureName)
    {
      InitializeComponent();
      ViewModel = new SettingsWindowViewModel(cultureName);
      DataContext = ViewModel;
    }

    #endregion

    #region Other Stuff

    private void OkOnClick(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
    }

    #endregion
  }
}
