using System.IO;
using System.Text;
using System.Windows;
using CefSharp;
using Microsoft.Win32;
using WordSearchGenerator.Desktop.Models.Rendering;
using WordSearchGenerator.Desktop.Services.Rendering;

namespace WordSearchGenerator.Desktop.Views
{
  public partial class MainWindow
  {
    #region Fields

    private readonly BoardPngRenderer _boardPngRenderer = new();
    private string? _lastExportDirectory;

    #endregion

    #region Other Stuff

    private string CreateSuggestedFileName(
      string extension,
      bool boardOnly = false)
    {
      var baseName = string.IsNullOrWhiteSpace(_viewModel.PuzzleHeading)
        ? "word-search"
        : _viewModel.PuzzleHeading.Trim();
      var modeSuffix = _viewModel.PreviewMode == BoardPreviewMode.Solution
        ? "-solution"
        : "-puzzle";
      var boardSuffix = boardOnly ? "-board" : string.Empty;
      var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
      var sanitized = new string(baseName
        .Select(character => invalidCharacters.Contains(character)
          ? '-'
          : character)
        .ToArray())
        .Trim(' ', '.');

      if (string.IsNullOrWhiteSpace(sanitized))
      {
        sanitized = "word-search";
      }

      return $"{sanitized}{modeSuffix}{boardSuffix}{extension}";
    }

    private async Task EnsurePreviewIsReadyAsync()
    {
      if (!_viewModel.IsPreviewReady || !PreviewBrowser.IsBrowserInitialized)
      {
        throw new InvalidOperationException(
          "The current preview has not finished rendering.");
      }

      await PreviewBrowser.WaitForRenderIdleAsync(
        150,
        TimeSpan.FromSeconds(5));
    }

    private void HandleExportError(string operation, Exception exception)
    {
      _viewModel.ReportExportFailed();

      MessageBox.Show(
        this,
        $"{operation} could not be completed.\n\n{exception.Message}",
        "Export failed",
        MessageBoxButton.OK,
        MessageBoxImage.Error);
    }

    private async void PrintCurrentPreviewOnClick(
      object sender,
      RoutedEventArgs e)
    {
      try
      {
        _viewModel.ReportExportStarted("Preparing print");
        await EnsurePreviewIsReadyAsync();
        PreviewBrowser.Print();
        _viewModel.ReportExportCompleted("Print dialog opened");
      }
      catch (Exception exception)
      {
        HandleExportError("Printing", exception);
      }
    }

    private async void SaveCurrentBoardAsPngOnClick(
      object sender,
      RoutedEventArgs e)
    {
      var path = ShowExportSaveDialog(
        ".png",
        "PNG image (*.png)|*.png",
        boardOnly: true);

      if (path == null)
      {
        return;
      }

      try
      {
        _viewModel.ReportExportStarted("Saving PNG");
        await EnsurePreviewIsReadyAsync();
        var model = _viewModel.GetCurrentBoardRenderModel() ??
                    throw new InvalidOperationException(
                      "There is no generated board to export.");
        var png = _boardPngRenderer.Render(
          model,
          _viewModel.PreviewMode);

        await File.WriteAllBytesAsync(path, png);
        _viewModel.ReportExportCompleted("PNG saved");
      }
      catch (Exception exception)
      {
        HandleExportError("PNG export", exception);
      }
    }

    private async void SaveCurrentPreviewAsHtmlOnClick(
      object sender,
      RoutedEventArgs e)
    {
      var path = ShowExportSaveDialog(
        ".html",
        "HTML document (*.html)|*.html");

      if (path == null)
      {
        return;
      }

      try
      {
        _viewModel.ReportExportStarted("Saving HTML");
        await EnsurePreviewIsReadyAsync();
        await File.WriteAllTextAsync(
          path,
          _viewModel.PreviewHtml,
          new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        _viewModel.ReportExportCompleted("HTML saved");
      }
      catch (Exception exception)
      {
        HandleExportError("HTML export", exception);
      }
    }

    private async void SaveCurrentPreviewAsPdfOnClick(
      object sender,
      RoutedEventArgs e)
    {
      var path = ShowExportSaveDialog(
        ".pdf",
        "PDF document (*.pdf)|*.pdf");

      if (path == null)
      {
        return;
      }

      try
      {
        _viewModel.ReportExportStarted("Saving PDF");
        await EnsurePreviewIsReadyAsync();
        var succeeded = await PreviewBrowser.PrintToPdfAsync(
          path,
          new PdfPrintSettings
          {
            DisplayHeaderFooter = false,
            PreferCssPageSize = true,
            PrintBackground = true
          });

        if (!succeeded)
        {
          throw new IOException("Chromium could not create the PDF file.");
        }

        _viewModel.ReportExportCompleted("PDF saved");
      }
      catch (Exception exception)
      {
        HandleExportError("PDF export", exception);
      }
    }

    private string? ShowExportSaveDialog(
      string extension,
      string filter,
      bool boardOnly = false)
    {
      var dialog = new SaveFileDialog
      {
        AddExtension = true,
        CheckPathExists = true,
        DefaultExt = extension,
        FileName = CreateSuggestedFileName(extension, boardOnly),
        Filter = filter,
        OverwritePrompt = true,
        Title = "Save current preview"
      };

      if (_lastExportDirectory != null &&
          Directory.Exists(_lastExportDirectory))
      {
        dialog.InitialDirectory = _lastExportDirectory;
      }

      if (dialog.ShowDialog(this) != true)
      {
        return null;
      }

      _lastExportDirectory = Path.GetDirectoryName(dialog.FileName);
      return dialog.FileName;
    }

    #endregion
  }
}
