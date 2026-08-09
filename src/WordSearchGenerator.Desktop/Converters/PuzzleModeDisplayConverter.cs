using System.Globalization;
using System.Windows.Data;
using WordSearchGenerator.Common;
using WordSearchGenerator.Desktop.Localization;

namespace WordSearchGenerator.Desktop.Converters
{
  public sealed class PuzzleModeDisplayConverter : IValueConverter
  {
    #region Interface Implementations

    public object Convert(
      object value,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      return value switch
      {
        PuzzleMode.Normal => AppStrings.Get("ModeNormal"),
        PuzzleMode.Quiz => AppStrings.Get("ModeQuiz"),
        _ => value?.ToString() ?? string.Empty
      };
    }

    public object ConvertBack(
      object value,
      Type targetType,
      object parameter,
      CultureInfo culture)
    {
      throw new NotSupportedException();
    }

    #endregion
  }
}
