using System.Globalization;

namespace WordSearchGenerator.Desktop.Localization
{
  public static class ApplicationCulture
  {
    #region Static Fields

    public const string CzechCultureName = "cs-CZ";
    public const string EnglishCultureName = "en-US";

    #endregion

    #region Properties

    public static IReadOnlyList<string> SupportedCultureNames
    {
      get;
    } = [EnglishCultureName, CzechCultureName];

    #endregion

    #region Other Stuff

    public static void Apply(string cultureName)
    {
      var culture = CultureInfo.GetCultureInfo(Normalize(cultureName));

      CultureInfo.CurrentCulture = culture;
      CultureInfo.CurrentUICulture = culture;
      CultureInfo.DefaultThreadCurrentCulture = culture;
      CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public static string GetInitialCultureName()
    {
      return Normalize(CultureInfo.CurrentUICulture.Name);
    }

    public static bool IsSupported(string? cultureName)
    {
      return cultureName != null &&
             SupportedCultureNames.Contains(
               cultureName,
               StringComparer.OrdinalIgnoreCase);
    }

    public static string Normalize(string? cultureName)
    {
      if (string.IsNullOrWhiteSpace(cultureName))
      {
        return EnglishCultureName;
      }

      if (cultureName.StartsWith("cs", StringComparison.OrdinalIgnoreCase))
      {
        return CzechCultureName;
      }

      if (cultureName.StartsWith("en", StringComparison.OrdinalIgnoreCase))
      {
        return EnglishCultureName;
      }

      return EnglishCultureName;
    }

    #endregion
  }
}