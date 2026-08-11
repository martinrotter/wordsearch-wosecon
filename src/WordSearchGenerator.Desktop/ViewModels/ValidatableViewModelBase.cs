using System.Collections;
using System.ComponentModel;

namespace Wose.Desktop.ViewModels
{
  public abstract class ValidatableViewModelBase : ViewModelBase, INotifyDataErrorInfo
  {
    #region Fields

    private readonly Dictionary<string, IReadOnlyList<string>> _errors =
      new(StringComparer.Ordinal);

    #endregion

    #region Properties

    public bool HasErrors => _errors.Count != 0;

    #endregion

    #region Interface Implementations

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
      if (string.IsNullOrEmpty(propertyName))
      {
        return _errors.Values.SelectMany(errors => errors).ToArray();
      }

      return _errors.TryGetValue(propertyName, out var errors)
        ? errors
        : Array.Empty<string>();
    }

    #endregion

    #region Other Stuff

    protected IReadOnlyList<string> GetAllErrors()
    {
      return _errors.Values.SelectMany(errors => errors).ToArray();
    }

    protected void SetErrors(string propertyName, IEnumerable<string> errors)
    {
      ArgumentException.ThrowIfNullOrEmpty(propertyName);
      ArgumentNullException.ThrowIfNull(errors);

      var newErrors = errors
        .Where(error => !string.IsNullOrWhiteSpace(error))
        .Distinct(StringComparer.Ordinal)
        .ToArray();

      var changed = newErrors.Length == 0
        ? _errors.Remove(propertyName)
        : SetErrorList(propertyName, newErrors);

      if (!changed)
      {
        return;
      }

      ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
      OnPropertyChanged(nameof(HasErrors));
    }

    private bool SetErrorList(string propertyName, IReadOnlyList<string> errors)
    {
      if (_errors.TryGetValue(propertyName, out var existingErrors) &&
          existingErrors.SequenceEqual(errors, StringComparer.Ordinal))
      {
        return false;
      }

      _errors[propertyName] = errors;
      return true;
    }

    #endregion
  }
}
