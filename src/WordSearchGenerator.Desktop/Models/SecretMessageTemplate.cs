namespace Wose.Desktop.Models
{
  public static class SecretMessageTemplate
  {
    public const string Marker = "<TAJENKA>";

    public static bool TrySplit(
      string? template,
      out string prefix,
      out string suffix)
    {
      prefix = string.Empty;
      suffix = string.Empty;

      if (template == null)
      {
        return false;
      }

      var markerIndex = template.IndexOf(
        Marker,
        StringComparison.OrdinalIgnoreCase);

      if (markerIndex < 0 ||
          template.IndexOf(
            Marker,
            markerIndex + Marker.Length,
            StringComparison.OrdinalIgnoreCase) >= 0)
      {
        return false;
      }

      prefix = template[..markerIndex];
      suffix = template[(markerIndex + Marker.Length)..];
      return true;
    }
  }
}
