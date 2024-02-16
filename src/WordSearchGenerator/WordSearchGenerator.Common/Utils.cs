namespace WordSearchGenerator.Common
{
  public static class Utils
  {
    #region Metody

    public static string Reverse(this string s)
    {
      char[] rev = s.ToCharArray();
      Array.Reverse(rev);
      return new string(rev);
    }

    public static T TakeFirst<T>(this List<T> list)
    {
      if (list == null || list.Count == 0)
      {
        return default(T);
      }
      else
      {
        T first = list[0];
        list.RemoveAt(0);
        return first;
      }
    }

    #endregion
  }
}