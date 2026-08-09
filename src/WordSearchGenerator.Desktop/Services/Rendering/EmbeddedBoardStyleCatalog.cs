using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;
using WordSearchGenerator.Desktop.Localization;

namespace WordSearchGenerator.Desktop.Services.Rendering
{
  public sealed class EmbeddedBoardStyleCatalog : IBoardStyleCatalog
  {
    #region Constants

    public const string EditorialStyleId = "editorial";

    private const string ResourcePrefix =
      "WordSearchGenerator.Desktop.Resources.BoardStyles.";

    #endregion

    #region Fields

    private readonly IReadOnlyDictionary<string, string> _styles;

    #endregion

    #region Properties

    public string DefaultStyleId => EditorialStyleId;

    public IReadOnlyList<string> StyleIds
    {
      get;
    }

    #endregion

    #region Constructors

    public EmbeddedBoardStyleCatalog()
    {
      var assembly = typeof(EmbeddedBoardStyleCatalog).Assembly;
      var styles = new Dictionary<string, string>(StringComparer.Ordinal);

      foreach (var resourceName in assembly
                 .GetManifestResourceNames()
                 .Where(name => name.StartsWith(
                                  ResourcePrefix,
                                  StringComparison.Ordinal) &&
                                name.EndsWith(
                                  ".css",
                                  StringComparison.Ordinal))
                 .OrderBy(name => name, StringComparer.Ordinal))
      {
        var styleId = resourceName[
          ResourcePrefix.Length..^".css".Length];

        if (styleId.Length == 0 || styleId.Contains('.'))
        {
          throw new InvalidOperationException(AppStrings.Format(
            "BoardStyleResourceNameInvalid",
            resourceName));
        }

        using var stream = assembly.GetManifestResourceStream(resourceName) ??
                           throw new InvalidOperationException(
                             AppStrings.Format(
                               "BoardStyleResourceMissing",
                               resourceName));
        using var reader = new StreamReader(
          stream,
          Encoding.UTF8,
          true);
        var css = reader.ReadToEnd();

        if (string.IsNullOrWhiteSpace(css))
        {
          throw new InvalidOperationException(AppStrings.Format(
            "BoardStyleEmpty",
            styleId));
        }

        if (!styles.TryAdd(styleId, css))
        {
          throw new InvalidOperationException(AppStrings.Format(
            "BoardStyleDuplicate",
            styleId));
        }
      }

      if (!styles.ContainsKey(DefaultStyleId))
      {
        throw new InvalidOperationException(AppStrings.Format(
          "BoardStyleDefaultMissing",
          DefaultStyleId));
      }

      _styles = new ReadOnlyDictionary<string, string>(styles);
      StyleIds = new ReadOnlyCollection<string>(styles.Keys.ToArray());
    }

    #endregion

    #region Interface Implementations

    public bool Contains(string styleId)
    {
      return !string.IsNullOrWhiteSpace(styleId) &&
             _styles.ContainsKey(styleId);
    }

    public string GetCss(string styleId)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(styleId);

      return _styles.TryGetValue(styleId, out var css)
        ? css
        : throw new ArgumentException(
          AppStrings.Format("BoardStyleUnknown", styleId),
          nameof(styleId));
    }

    #endregion
  }
}
