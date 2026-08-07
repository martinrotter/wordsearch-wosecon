using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using CefSharp;
using WordSearchGenerator.Desktop.ViewModels;

namespace WordSearchGenerator.Desktop.Views
{
  public partial class MainWindow : Window
  {
    #region Fields

    private string? _loadedPreviewHtml;
    private readonly MainWindowViewModel _viewModel;

    #endregion

    #region Constructors

    public MainWindow(MainWindowViewModel viewModel)
    {
      ArgumentNullException.ThrowIfNull(viewModel);

      InitializeComponent();
      _viewModel = viewModel;
      DataContext = viewModel;

      _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
      PreviewBrowser.IsBrowserInitializedChanged +=
        PreviewBrowserOnIsBrowserInitializedChanged;
      PreviewBrowser.LoadingStateChanged +=
        PreviewBrowserOnLoadingStateChanged;
      Closed += MainWindowOnClosed;
    }

    #endregion

    #region Other Stuff

    private void MainWindowOnClosed(object? sender, EventArgs e)
    {
      _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
      PreviewBrowser.IsBrowserInitializedChanged -=
        PreviewBrowserOnIsBrowserInitializedChanged;
      PreviewBrowser.LoadingStateChanged -=
        PreviewBrowserOnLoadingStateChanged;
      Closed -= MainWindowOnClosed;
    }

    private void PreviewBrowserOnIsBrowserInitializedChanged(
      object? sender,
      DependencyPropertyChangedEventArgs e)
    {
      if (e.NewValue is true)
      {
        QueuePreviewUpdate();
      }
    }

    private void PreviewBrowserOnLoadingStateChanged(
      object? sender,
      LoadingStateChangedEventArgs e)
    {
      Dispatcher.BeginInvoke(
        DispatcherPriority.DataBind,
        new Action(() => _viewModel.SetPreviewReady(!e.IsLoading)));
    }

    private void QueuePreviewUpdate()
    {
      Dispatcher.BeginInvoke(
        DispatcherPriority.Loaded,
        new Action(UpdatePreviewBrowser));
    }

    private void UpdatePreviewBrowser()
    {
      if (!PreviewBrowser.IsBrowserInitialized || PreviewBrowser.IsDisposed)
      {
        return;
      }

      var html = _viewModel.PreviewHtml;

      if (string.Equals(html, _loadedPreviewHtml, StringComparison.Ordinal))
      {
        return;
      }

      _loadedPreviewHtml = html;
      _viewModel.SetPreviewReady(false);

      if (string.IsNullOrEmpty(html))
      {
        PreviewBrowser.Load("about:blank");
        return;
      }

      PreviewBrowser.LoadHtml(html, base64Encode: true);
    }

    private void ViewModelOnPropertyChanged(
      object? sender,
      PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(MainWindowViewModel.PreviewHtml))
      {
        QueuePreviewUpdate();
      }
    }

    #endregion
  }
}
