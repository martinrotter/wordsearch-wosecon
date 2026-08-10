using System.Globalization;
using System.IO;
using System.Reflection;

namespace Wose.Desktop.Localization
{
  public static class ApplicationCulture
  {
    #region Static Fields

    private static readonly IReadOnlyList<AvailableCulture> AvailableCultures =
      DiscoverAvailableCultures();

    #endregion

    #region Properties

    public static IReadOnlyList<string> SupportedCultureNames
    {
      get;
    } = AvailableCultures.Select(culture => culture.FullName).ToArray();

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
        return AvailableCultures[0].FullName;
      }

      var exactMatch = AvailableCultures.FirstOrDefault(culture =>
        string.Equals(
          culture.FullName,
          cultureName,
          StringComparison.OrdinalIgnoreCase));

      if (exactMatch != null)
      {
        return exactMatch.FullName;
      }

      var languageMatch = AvailableCultures.FirstOrDefault(culture =>
        string.Equals(
          culture.ShortName,
          cultureName,
          StringComparison.OrdinalIgnoreCase) ||
        cultureName.StartsWith(
          $"{culture.ShortName}-",
          StringComparison.OrdinalIgnoreCase));

      return languageMatch?.FullName ?? AvailableCultures[0].FullName;
    }

    private static IReadOnlyList<AvailableCulture> DiscoverAvailableCultures()
    {
      var cultures = new List<AvailableCulture>();

      AddCultureFromResources(
        cultures,
        CultureInfo.InvariantCulture,
        null);

      var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;

      if (assemblyName != null)
      {
        foreach (var directory in Directory.EnumerateDirectories(AppContext.BaseDirectory))
        {
          var satellitePath = Path.Combine(
            directory,
            $"{assemblyName}.resources.dll");

          if (!File.Exists(satellitePath))
          {
            continue;
          }

          try
          {
            var directoryCulture = CultureInfo.GetCultureInfo(
              Path.GetFileName(directory));
            AddCultureFromResources(
              cultures,
              directoryCulture,
              directoryCulture.Name);
          }
          catch (CultureNotFoundException)
          {
            // Not a culture-named satellite-resource directory.
          }
        }
      }

      if (cultures.Count == 0)
      {
        throw new InvalidOperationException(
          "The neutral localization resources do not define valid culture metadata.");
      }

      return cultures;
    }

    private static void AddCultureFromResources(
      ICollection<AvailableCulture> cultures,
      CultureInfo resourceCulture,
      string? expectedFullName)
    {
      var fullName = AppStrings.GetExact("CultureNameFull", resourceCulture);
      var shortName = AppStrings.GetExact("CultureNameShort", resourceCulture);

      if (string.IsNullOrWhiteSpace(fullName) ||
          string.IsNullOrWhiteSpace(shortName) ||
          (expectedFullName != null &&
           !string.Equals(
             fullName,
             expectedFullName,
             StringComparison.OrdinalIgnoreCase)) ||
          cultures.Any(culture => string.Equals(
            culture.FullName,
            fullName,
            StringComparison.OrdinalIgnoreCase)))
      {
        return;
      }

      try
      {
        var culture = CultureInfo.GetCultureInfo(fullName);

        if (!string.Equals(
              culture.TwoLetterISOLanguageName,
              shortName,
              StringComparison.OrdinalIgnoreCase))
        {
          return;
        }

        cultures.Add(new AvailableCulture(culture.Name, shortName));
      }
      catch (CultureNotFoundException)
      {
        // Ignore resource sets with invalid culture metadata.
      }
    }

    #endregion

    #region Nested Types

    private sealed record AvailableCulture(
      string FullName,
      string ShortName);

    #endregion
  }
}
