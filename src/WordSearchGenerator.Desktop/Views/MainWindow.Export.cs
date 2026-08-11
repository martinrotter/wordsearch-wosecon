using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CefSharp;
using CefSharp.Wpf;
using Microsoft.Win32;
using Wose.Desktop.Localization;
using Wose.Desktop.Models.Rendering;

namespace Wose.Desktop.Views
{
  public partial class MainWindow
  {
    #region Constants

    private const int BoardPngLongSide = 2400;
    private const int DocxBoardPngLongSide = 3600;

    #endregion

    #region Fields

    private string? _lastExportDirectory;

    #endregion

    #region Other Stuff

    private async Task<byte[]> CaptureBoardPngAsync(int targetLongSide)
    {
      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetLongSide);

      await EnsurePreviewIsReadyAsync();
      var bounds = await PreviewBrowser
        .EvaluateScriptAsync<BoardCaptureBounds>(
          """
          (() => {
            const board = document.querySelector('.matrix');

            if (!board) {
              return null;
            }

            const overlayId = 'wosecon-board-export-overlay';
            document.getElementById(overlayId)?.remove();

            const sourceBounds = board.getBoundingClientRect();
            const viewportWidth = window.innerWidth;
            const viewportHeight = window.innerHeight;
            const fitScale = Math.min(
              viewportWidth / sourceBounds.width,
              viewportHeight / sourceBounds.height);
            const overlay = document.createElement('div');
            overlay.id = overlayId;
            Object.assign(overlay.style, {
              position: 'fixed',
              inset: '0',
              zIndex: '2147483647',
              overflow: 'hidden',
              background: '#ffffff'
            });

            const clone = board.cloneNode(true);
            Object.assign(clone.style, {
              position: 'absolute',
              left: '0',
              top: '0',
              width: `${sourceBounds.width}px`,
              margin: '0',
              transformOrigin: 'top left',
              transform: `scale(${fitScale})`
            });
            overlay.appendChild(clone);
            document.body.appendChild(overlay);

            const bounds = clone.getBoundingClientRect();
            return {
              x: bounds.left,
              y: bounds.top,
              width: bounds.width,
              height: bounds.height,
              viewportWidth,
              viewportHeight
            };
          })()
          """);

      try
      {
        if (bounds == null ||
            bounds.Width <= 0 ||
            bounds.Height <= 0 ||
            bounds.ViewportWidth <= 0 ||
            bounds.ViewportHeight <= 0)
        {
          throw new InvalidOperationException(
            AppStrings.Get("NoBoardToExport"));
        }

        await PreviewBrowser.WaitForRenderIdleAsync(
          150,
          TimeSpan.FromSeconds(5));
        var frame = await CaptureBrowserPaintFrameAsync();
        return EncodeBoardPng(frame, bounds, targetLongSide);
      }
      finally
      {
        await PreviewBrowser.EvaluateScriptAsync<bool>(
          """
          (() => {
            document.getElementById('wosecon-board-export-overlay')?.remove();
            return true;
          })()
          """);
        await PreviewBrowser.WaitForRenderIdleAsync(
          150,
          TimeSpan.FromSeconds(5));
      }
    }

    private async Task<BrowserPaintFrame> CaptureBrowserPaintFrameAsync()
    {
      var completion = new TaskCompletionSource<BrowserPaintFrame>(
        TaskCreationOptions.RunContinuationsAsynchronously);

      void PreviewBrowserOnPaint(object? sender, PaintEventArgs e)
      {
        if (e.IsPopup || completion.Task.IsCompleted)
        {
          return;
        }

        var pixels = new byte[checked(e.Width * e.Height * 4)];
        Marshal.Copy(e.Buffer, pixels, 0, pixels.Length);
        completion.TrySetResult(
          new BrowserPaintFrame(pixels, e.Width, e.Height));
      }

      PreviewBrowser.Paint += PreviewBrowserOnPaint;

      try
      {
        PreviewBrowser.GetBrowser().GetHost().Invalidate(
          PaintElementType.View);
        return await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
      }
      finally
      {
        PreviewBrowser.Paint -= PreviewBrowserOnPaint;
      }
    }

    private static byte[] EncodeBoardPng(
      BrowserPaintFrame frame,
      BoardCaptureBounds bounds,
      int targetLongSide)
    {
      var pixelScaleX = frame.Width / bounds.ViewportWidth;
      var pixelScaleY = frame.Height / bounds.ViewportHeight;
      var left = Math.Clamp(
        (int)Math.Floor(bounds.X * pixelScaleX),
        0,
        frame.Width - 1);
      var top = Math.Clamp(
        (int)Math.Floor(bounds.Y * pixelScaleY),
        0,
        frame.Height - 1);
      var right = Math.Clamp(
        (int)Math.Ceiling((bounds.X + bounds.Width) * pixelScaleX),
        left + 1,
        frame.Width);
      var bottom = Math.Clamp(
        (int)Math.Ceiling((bounds.Y + bounds.Height) * pixelScaleY),
        top + 1,
        frame.Height);
      var cropWidth = right - left;
      var cropHeight = bottom - top;
      var bitmap = BitmapSource.Create(
        frame.Width,
        frame.Height,
        96,
        96,
        PixelFormats.Bgra32,
        null,
        frame.Pixels,
        checked(frame.Width * 4));
      var cropped = new CroppedBitmap(
        bitmap,
        new Int32Rect(left, top, cropWidth, cropHeight));
      var outputScale = targetLongSide /
                        (double)Math.Max(cropWidth, cropHeight);
      var outputWidth = Math.Max(1, (int)Math.Round(cropWidth * outputScale));
      var outputHeight = Math.Max(1, (int)Math.Round(cropHeight * outputScale));
      var visual = new DrawingVisual();
      RenderOptions.SetBitmapScalingMode(
        visual,
        BitmapScalingMode.HighQuality);

      using (var drawing = visual.RenderOpen())
      {
        drawing.DrawImage(
          cropped,
          new Rect(0, 0, outputWidth, outputHeight));
      }

      var output = new RenderTargetBitmap(
        outputWidth,
        outputHeight,
        96,
        96,
        PixelFormats.Pbgra32);
      output.Render(visual);

      var encoder = new PngBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(output));

      using var stream = new MemoryStream();
      encoder.Save(stream);
      return stream.ToArray();
    }

    private string CreateSuggestedFileName(string extension)
    {
      var projectBaseName = string.IsNullOrWhiteSpace(_viewModel.ProjectFilePath)
        ? null
        : Path.GetFileNameWithoutExtension(_viewModel.ProjectFilePath);
      var baseName = !string.IsNullOrWhiteSpace(projectBaseName)
        ? projectBaseName
        : string.IsNullOrWhiteSpace(_viewModel.PuzzleHeading)
          ? AppStrings.Get("DefaultExportBaseName")
          : _viewModel.PuzzleHeading.Trim();
      var modeSuffix = _viewModel.PreviewMode == BoardPreviewMode.Solution
        ? AppStrings.Get("SolutionFileSuffix")
        : AppStrings.Get("PuzzleFileSuffix");
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

      return $"{sanitized}{modeSuffix}{extension}";
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
        AppStrings.Get("PngFilter"));

      if (path == null)
      {
        return;
      }

      try
      {
        _viewModel.ReportExportStarted(AppStrings.Get("SavingPng"));
        var png = await CaptureBoardPngAsync(BoardPngLongSide);

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
        var boardPng = await CaptureBoardPngAsync(DocxBoardPngLongSide);

        await _docxPuzzleExporter.ExportAsync(
          path,
          model,
          _viewModel.PreviewMode,
          boardPng);
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
      string filter)
    {
      var dialog = new SaveFileDialog
      {
        AddExtension = true,
        CheckPathExists = true,
        DefaultExt = extension,
        FileName = CreateSuggestedFileName(extension),
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

    #region Nested Types

    private sealed class BoardCaptureBounds
    {
      #region Properties

      public double Height
      {
        get;
        init;
      }

      public double Width
      {
        get;
        init;
      }

      public double ViewportHeight
      {
        get;
        init;
      }

      public double ViewportWidth
      {
        get;
        init;
      }

      public double X
      {
        get;
        init;
      }

      public double Y
      {
        get;
        init;
      }

      #endregion
    }

    private sealed record BrowserPaintFrame(
      byte[] Pixels,
      int Width,
      int Height);

    #endregion
  }
}
