using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Wose.Desktop.ViewModels
{
  public abstract class ViewModelBase : INotifyPropertyChanged
  {
    #region Interface Implementations

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    #region Other Stuff

    protected bool SetProperty<T>(
      ref T field,
      T value,
      [CallerMemberName] string? propertyName = null)
    {
      if (EqualityComparer<T>.Default.Equals(field, value))
      {
        return false;
      }

      field = value;
      OnPropertyChanged(propertyName);
      return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
  }
}
