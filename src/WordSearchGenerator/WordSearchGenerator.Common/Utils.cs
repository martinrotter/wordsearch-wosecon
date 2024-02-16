using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordSearchGenerator.Common
{
  public static class Utils
  {
    public static T TakeFirst<T>(this List<T> list)
    {
      if (list == null || list.Count == 0)
      {
        return default(T);
      }
      else
      {
        var first = list[0];
        list.RemoveAt(0);
        return first;
      }
    }

    public static string Reverse(this string s)
    {
      var rev = s.ToCharArray();
      Array.Reverse(rev);
      return new string(rev);
    }
  }
}
