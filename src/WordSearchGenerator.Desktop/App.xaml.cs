using System.Windows;
using Wose.Desktop.Localization;
using Wose.Desktop.Services;
using Wose.Desktop.Services.Exporting;
using Wose.Desktop.Services.Persistence;
using Wose.Desktop.Services.Rendering;
using Wose.Desktop.Services.Settings;
using Wose.Desktop.ViewModels;
using Wose.Desktop.Views;

namespace Wose.Desktop
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
      var boardStyleCatalog = new EmbeddedBoardStyleCatalog();
      var boardHtmlRenderer = new BoardHtmlRenderer(boardStyleCatalog);
      var docxPuzzleExporter = new DocxPuzzleExporter();
      var projectSerializer = new PuzzleProjectSerializer(boardStyleCatalog);
      var mainWindowViewModel = new MainWindowViewModel(
        puzzleGenerator,
        boardHtmlRenderer,
        boardStyleCatalog);
      var mainWindow = new MainWindow(
        mainWindowViewModel,
        projectSerializer,
        docxPuzzleExporter,
        settingsService,
        settings);

      MainWindow = mainWindow;
      MainWindow.Show();
    }

    #endregion
  }
}
