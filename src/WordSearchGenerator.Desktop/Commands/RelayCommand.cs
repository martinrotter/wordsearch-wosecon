using System.Windows.Input;

namespace Wose.Desktop.Commands
{
  public sealed class RelayCommand : ICommand
  {
    #region Fields

    private readonly Func<bool>? _canExecute;
    private readonly Action _execute;

    #endregion

    #region Constructors

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
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
      return _canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
      _execute();
    }

    #endregion

    #region Other Stuff

    public void NotifyCanExecuteChanged()
    {
      CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion
  }
}
