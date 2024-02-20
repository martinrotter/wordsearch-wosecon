using System.Text;
using WordSearchGenerator.Common;

namespace WordSearchGenerator.Console
{
  public class ConsoleUtils
  {
    #region Other Stuff

    public static void SetupConsole()
    {
      System.Console.InputEncoding = System.Console.OutputEncoding = Encoding.UTF8;
      System.Console.Title = Constants.Names.AppName;

      /*
      try
      {
        if (!System.Console.IsOutputRedirected)
        {
          System.Console.WindowWidth = 200;
          System.Console.WindowHeight = 60;
          System.Console.BackgroundColor = ConsoleColor.White;
          System.Console.Clear();
          System.Console.ForegroundColor = ConsoleColor.Black;
        }
      }
      catch
      {
        // Muted.
      }
      */
    }

    public static void WithBgColor(Action act, ConsoleColor color)
    {
      ConsoleColor back = System.Console.BackgroundColor;
      System.Console.BackgroundColor = color;
      act();
      System.Console.BackgroundColor = back;
    }

    #endregion
  }
}