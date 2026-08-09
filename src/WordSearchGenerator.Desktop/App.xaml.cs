using System.Windows;
using WordSearchGenerator.Desktop.Localization;
using WordSearchGenerator.Desktop.Services;
using WordSearchGenerator.Desktop.Services.Exporting;
using WordSearchGenerator.Desktop.Services.Persistence;
using WordSearchGenerator.Desktop.Services.Rendering;
using WordSearchGenerator.Desktop.Services.Settings;
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

      var settingsService = new JsonApplicationSettingsService();
      var settings = settingsService.Load();

      ApplicationCulture.Apply(settings.UiCulture);

      var puzzleGenerator = new MonteCarloPuzzleGenerator();
      var boardHtmlRenderer = new BoardHtmlRenderer();
      var boardPngRenderer = new BoardPngRenderer();
      var docxPuzzleExporter = new DocxPuzzleExporter(boardPngRenderer);
      var projectSerializer = new PuzzleProjectSerializer();
      var mainWindowViewModel = new MainWindowViewModel(
        puzzleGenerator,
        boardHtmlRenderer);
      var mainWindow = new MainWindow(
        mainWindowViewModel,
        projectSerializer,
        boardPngRenderer,
        docxPuzzleExporter,
        settingsService,
        settings);

      MainWindow = mainWindow;
      MainWindow.Show();
    }

    #endregion
  }
}