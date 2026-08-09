namespace WordSearchGenerator.Common
{
  public static class Utils
  {
    #region Other Stuff

    public static List<T> CloneList<T>(this List<T> list) where T : ICloneable
    {
      return list.Select(it => (T)it.Clone()).ToList();
    }

    #endregion
  }
}
