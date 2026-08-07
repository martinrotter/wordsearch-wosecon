using System.Windows;
using WordSearchGenerator.Desktop.Services;
using WordSearchGenerator.Desktop.Services.Rendering;
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

      var puzzleGenerator = new MonteCarloPuzzleGenerator();
      var boardHtmlRenderer = new BoardHtmlRenderer();
      var mainWindowViewModel = new MainWindowViewModel(
        puzzleGenerator,
        boardHtmlRenderer);
      var mainWindow = new MainWindow(mainWindowViewModel);

      MainWindow = mainWindow;
      MainWindow.Show();
    }

    #endregion
  }
}
