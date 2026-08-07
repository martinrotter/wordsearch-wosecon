using System.Windows;
using WordSearchGenerator.Desktop.Services;
using WordSearchGenerator.Desktop.ViewModels;
using WordSearchGenerator.Desktop.Views;

namespace WordSearchGenerator.Desktop
{
  public partial class App : Application
  {
    #region Other Stuff

    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      var singlePuzzleGenerator = new SinglePuzzleGenerator();
      var mainWindowViewModel = new MainWindowViewModel(singlePuzzleGenerator);
      var mainWindow = new MainWindow(mainWindowViewModel);

      MainWindow = mainWindow;
      MainWindow.Show();
    }

    #endregion
  }
}
