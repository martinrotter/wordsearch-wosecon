using System.Globalization;
using System.Reflection;
using System.Resources;

namespace WordSearchGenerator.Desktop.Localization
{
  public static class AppStrings
  {
    #region Static Fields

    private static readonly ResourceManager ResourceManager = new(
      "WordSearchGenerator.Desktop.Resources.Localization.Strings",
      Assembly.GetExecutingAssembly());

    #endregion

    #region Other Stuff

    public static string Format(string key, params object?[] arguments)
    {
      return string.Format(
        CultureInfo.CurrentCulture,
        Get(key),
        arguments);
    }

    public static string Get(string key)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(key);

      return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ??
             $"[{key}]";
    }

    internal static string? GetExact(string key, CultureInfo culture)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(key);
      ArgumentNullException.ThrowIfNull(culture);

      return ResourceManager
        .GetResourceSet(culture, true, false)?
        .GetString(key);
    }

    #endregion
  }
}