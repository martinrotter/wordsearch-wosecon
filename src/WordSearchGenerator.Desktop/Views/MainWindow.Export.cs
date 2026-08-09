using System.IO;
using System.Text;
using System.Windows;
using CefSharp;
using Microsoft.Win32;
using WordSearchGenerator.Desktop.Localization;
using WordSearchGenerator.Desktop.Models.Rendering;

namespace WordSearchGenerator.Desktop.Views
{
  public partial class MainWindow
  {
    #region Fields

    private string? _lastExportDirectory;

    #endregion

    #region Other Stuff

    private string CreateSuggestedFileName(
      string extension,
      bool boardOnly = false)
    {
      var baseName = string.IsNullOrWhiteSpace(_viewModel.PuzzleHeading)
        ? AppStrings.Get("DefaultExportBaseName")
        : _viewModel.PuzzleHeading.Trim();
      var modeSuffix = _viewModel.PreviewMode == BoardPreviewMode.Solution
        ? AppStrings.Get("SolutionFileSuffix")
        : AppStrings.Get("PuzzleFileSuffix");
      var boardSuffix = boardOnly
        ? AppStrings.Get("BoardFileSuffix")
        : string.Empty;
      var invalidCharacters = Path.GetInvalidFileNameChars().ToHashSet();
      var sanitized = new string(baseName
          .Select(character => invalidCharacters.Contains(character)
            ? '-'
            : character)
          .ToArray())
        .Trim(' ', '.');

      if (string.IsNullOrWhiteSpace(sanitized))
      {
        sanitized = AppStrings.Get("DefaultExportBaseName");
      }

      return $"{sanitized}{modeSuffix}{boardSuffix}{extension}";
    }

    private async Task EnsurePreviewIsReadyAsync()
    {
      if (!_viewModel.IsPreviewReady || !PreviewBrowser.IsBrowserInitialized)
      {
        throw new InvalidOperationException(
          AppStrings.Get("PreviewNotRendered"));
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
        AppStrings.Format(
          "OperationCouldNotComplete",
          operation,
          exception.Message),
        AppStrings.Get("ExportFailed"),
        MessageBoxButton.OK,
        MessageBoxImage.Error);
    }

    private async void PrintCurrentPreviewOnClick(
      object sender,
      RoutedEventArgs e)
    {
      try
      {
        _viewModel.ReportExportStarted(AppStrings.Get("PreparingPrint"));
        await EnsurePreviewIsReadyAsync();
        PreviewBrowser.Print();
        _viewModel.ReportExportCompleted(AppStrings.Get("PrintDialogOpened"));
      }
      catch (Exception exception)
      {
        HandleExportError(AppStrings.Get("Printing"), exception);
      }
    }

    private async void SaveCurrentBoardAsPngOnClick(
      object sender,
      RoutedEventArgs e)
    {
      var path = ShowExportSaveDialog(
        ".png",
        AppStrings.Get("PngFilter"),
        true);

      if (path == null)
      {
        return;
      }

      try
      {
        _viewModel.ReportExportStarted(AppStrings.Get("SavingPng"));
        var model = _viewModel.GetCurrentBoardRenderModel() ??
                    throw new InvalidOperationException(
                      AppStrings.Get("NoBoardToExport"));
        var png = _boardPngRenderer.Render(
          model,
          _viewModel.PreviewMode);

        await File.WriteAllBytesAsync(path, png);
        _viewModel.ReportExportCompleted(AppStrings.Get("PngSaved"));
      }
      catch (Exception exception)
      {
        HandleExportError(AppStrings.Get("PngExport"), exception);
      }
    }

    private async void SaveCurrentPreviewAsHtmlOnClick(
      object sender,
      RoutedEventArgs e)
    {
      var path = ShowExportSaveDialog(
        ".html",
        AppStrings.Get("HtmlFilter"));

      if (path == null)
      {
        return;
      }

      try
      {
        _viewModel.ReportExportStarted(AppStrings.Get("SavingHtml"));
        await File.WriteAllTextAsync(
          path,
          _viewModel.PreviewHtml,
          new UTF8Encoding(false));
        _viewModel.ReportExportCompleted(AppStrings.Get("HtmlSaved"));
      }
      catch (Exception exception)
      {
        HandleExportError(AppStrings.Get("HtmlExport"), exception);
      }
    }

    private async void SaveCurrentPreviewAsDocxOnClick(
      object sender,
      RoutedEventArgs e)
    {
      var path = ShowExportSaveDialog(
        ".docx",
        AppStrings.Get("DocxFilter"));

      if (path == null)
      {
        return;
      }

      try
      {
        _viewModel.ReportExportStarted(AppStrings.Get("SavingDocx"));
        var model = _viewModel.GetCurrentBoardRenderModel() ??
                    throw new InvalidOperationException(
                      AppStrings.Get("NoBoardToExport"));

        await _docxPuzzleExporter.ExportAsync(
          path,
          model,
          _viewModel.PreviewMode);
        _viewModel.ReportExportCompleted(AppStrings.Get("DocxSaved"));
      }
      catch (Exception exception)
      {
        HandleExportError(AppStrings.Get("DocxExport"), exception);
      }
    }

    private async void SaveCurrentPreviewAsPdfOnClick(
      object sender,
      RoutedEventArgs e)
    {
      var path = ShowExportSaveDialog(
        ".pdf",
        AppStrings.Get("PdfFilter"));

      if (path == null)
      {
        return;
      }

      try
      {
        _viewModel.ReportExportStarted(AppStrings.Get("SavingPdf"));
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
          throw new IOException(AppStrings.Get("ChromiumPdfFailed"));
        }

        _viewModel.ReportExportCompleted(AppStrings.Get("PdfSaved"));
      }
      catch (Exception exception)
      {
        HandleExportError(AppStrings.Get("PdfExport"), exception);
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
        Title = AppStrings.Get("SaveCurrentPreviewTitle")
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