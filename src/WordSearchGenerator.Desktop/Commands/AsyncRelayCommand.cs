using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace WordSearchGenerator.Desktop.Commands
{
  public sealed class AsyncRelayCommand : ICommand, INotifyPropertyChanged
  {
    #region Fields

    private readonly Func<bool>? _canExecute;
    private readonly Func<CancellationToken, Task> _execute;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isRunning;

    #endregion

    #region Properties

    public bool IsRunning
    {
      get => _isRunning;
      private set
      {
        if (_isRunning == value)
        {
          return;
        }

        _isRunning = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(CanBeCanceled));
        NotifyCanExecuteChanged();
      }
    }

    public bool CanBeCanceled =>
      IsRunning &&
      _cancellationTokenSource is { IsCancellationRequested: false };

    #endregion

    #region Constructors

    public AsyncRelayCommand(
      Func<CancellationToken, Task> execute,
      Func<bool>? canExecute = null)
    {
      ArgumentNullException.ThrowIfNull(execute);

      _execute = execute;
      _canExecute = canExecute;
    }

    #endregion

    #region Interface Implementations

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
      return !IsRunning && (_canExecute?.Invoke() ?? true);
    }

    public async void Execute(object? parameter)
    {
      await ExecuteAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    #region Other Stuff

    public async Task ExecuteAsync()
    {
      if (!CanExecute(null))
      {
        return;
      }

      using var cancellationTokenSource = new CancellationTokenSource();
      _cancellationTokenSource = cancellationTokenSource;
      IsRunning = true;

      try
      {
        await _execute(cancellationTokenSource.Token);
      }
      finally
      {
        _cancellationTokenSource = null;
        IsRunning = false;
      }
    }

    public void Cancel()
    {
      if (!CanBeCanceled)
      {
        return;
      }

      _cancellationTokenSource!.Cancel();
      OnPropertyChanged(nameof(CanBeCanceled));
    }

    public void NotifyCanExecuteChanged()
    {
      CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}