namespace WordSearchGenerator.Common;

public static class Utils
{
  #region Other Stuff

  public static string Reverse(this string s)
  {
    var rev = s.ToCharArray();
    Array.Reverse(rev);
    return new string(rev);
  }

  public static T TakeFirst<T>(this List<T> list)
  {
    if (list == null || list.Count == 0)
    {
      return default;
    }

    var first = list[0];
    list.RemoveAt(0);
    return first;
  }

  #endregion
}