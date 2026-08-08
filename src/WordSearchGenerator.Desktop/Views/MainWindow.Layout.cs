using System.Diagnostics;
using System.Windows;
using WordSearchGenerator.Desktop.Models.Settings;

namespace WordSearchGenerator.Desktop.Views
{
  public partial class MainWindow
  {
    #region Other Stuff

    private static bool IsFinitePositive(double value)
    {
      return double.IsFinite(value) && value > 0;
    }

    private static bool IsVisibleOnCurrentDesktop(
      double left,
      double top,
      double width,
      double height)
    {
      var savedBounds = new Rect(left, top, width, height);
      var desktopBounds = new Rect(
        SystemParameters.VirtualScreenLeft,
        SystemParameters.VirtualScreenTop,
        SystemParameters.VirtualScreenWidth,
        SystemParameters.VirtualScreenHeight);
      var visibleBounds = Rect.Intersect(savedBounds, desktopBounds);

      return !visibleBounds.IsEmpty &&
             visibleBounds.Width >= 100 &&
             visibleBounds.Height >= 50;
    }

    private void RestoreLayout()
    {
      var placement = _applicationSettings.MainWindowPlacement;

      if (placement == null ||
          !double.IsFinite(placement.Left) ||
          !double.IsFinite(placement.Top) ||
          !IsFinitePositive(placement.Width) ||
          !IsFinitePositive(placement.Height) ||
          !IsFinitePositive(placement.EditorPaneWidth) ||
          !IsVisibleOnCurrentDesktop(
            placement.Left,
            placement.Top,
            placement.Width,
            placement.Height))
      {
        return;
      }

      WindowStartupLocation = WindowStartupLocation.Manual;
      Left = placement.Left;
      Top = placement.Top;
      Width = Math.Clamp(
        placement.Width,
        MinWidth,
        SystemParameters.VirtualScreenWidth);
      Height = Math.Clamp(
        placement.Height,
        MinHeight,
        SystemParameters.VirtualScreenHeight);

      var maximumEditorPaneWidth = Math.Max(
        EditorPaneColumn.MinWidth,
        Width - PreviewPaneColumn.MinWidth - MainSplitterColumn.Width.Value);
      EditorPaneColumn.Width = new GridLength(Math.Clamp(
        placement.EditorPaneWidth,
        EditorPaneColumn.MinWidth,
        maximumEditorPaneWidth));

      if (placement.IsMaximized)
      {
        WindowState = WindowState.Maximized;
      }
    }

    private void SaveLayout()
    {
      var bounds = WindowState == WindowState.Normal
        ? new Rect(Left, Top, Width, Height)
        : RestoreBounds;

      if (!double.IsFinite(bounds.Left) ||
          !double.IsFinite(bounds.Top) ||
          !IsFinitePositive(bounds.Width) ||
          !IsFinitePositive(bounds.Height))
      {
        return;
      }

      var placement = new MainWindowPlacement(
        bounds.Left,
        bounds.Top,
        bounds.Width,
        bounds.Height,
        WindowState == WindowState.Maximized,
        EditorPaneColumn.ActualWidth);
      var settings = _applicationSettings with
      {
        MainWindowPlacement = placement
      };

      try
      {
        _applicationSettingsService.Save(settings);
        _applicationSettings = settings;
      }
      catch (Exception exception)
      {
        // Layout persistence is best effort and must never prevent shutdown.
        Debug.WriteLine(exception);
      }
    }

    #endregion
  }
}