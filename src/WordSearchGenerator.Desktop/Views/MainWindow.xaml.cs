using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using CefSharp;
using WordSearchGenerator.Desktop.Models.Settings;
using WordSearchGenerator.Desktop.Services.Persistence;
using WordSearchGenerator.Desktop.Services.Settings;
using WordSearchGenerator.Desktop.ViewModels;

namespace WordSearchGenerator.Desktop.Views
{
  public partial class MainWindow : Window
  {
    #region Fields

    private readonly IApplicationSettingsService _applicationSettingsService;
    private readonly IPuzzleProjectSerializer _projectSerializer;
    private readonly MainWindowViewModel _viewModel;
    private ApplicationSettings _applicationSettings;

    private string? _loadedPreviewHtml;

    #endregion

    #region Constructors

    public MainWindow(
      MainWindowViewModel viewModel,
      IPuzzleProjectSerializer projectSerializer,
      IApplicationSettingsService applicationSettingsService,
      ApplicationSettings applicationSettings)
    {
      ArgumentNullException.ThrowIfNull(viewModel);
      ArgumentNullException.ThrowIfNull(projectSerializer);
      ArgumentNullException.ThrowIfNull(applicationSettingsService);
      ArgumentNullException.ThrowIfNull(applicationSettings);

      InitializeComponent();
      _viewModel = viewModel;
      _projectSerializer = projectSerializer;
      _applicationSettingsService = applicationSettingsService;
      _applicationSettings = applicationSettings;
      DataContext = viewModel;
      RestoreLayout();

      _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
      PreviewBrowser.IsBrowserInitializedChanged +=
        PreviewBrowserOnIsBrowserInitializedChanged;
      PreviewBrowser.LoadingStateChanged +=
        PreviewBrowserOnLoadingStateChanged;
      Closing += MainWindowOnClosing;
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
      Closing -= MainWindowOnClosing;
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

      PreviewBrowser.LoadHtml(html, true);
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