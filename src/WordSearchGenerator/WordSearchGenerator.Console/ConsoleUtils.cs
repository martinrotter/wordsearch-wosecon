using System.Text;
using WordSearchGenerator.Common;

namespace WordSearchGenerator.Console;

public class ConsoleUtils
{
  public static void SetupConsole()
  {
    System.Console.WindowWidth = 200;
    System.Console.WindowHeight = 60;
    System.Console.InputEncoding = System.Console.OutputEncoding = Encoding.UTF8;
    System.Console.Title = Constants.Names.AppName;
    System.Console.BackgroundColor = ConsoleColor.White;
    System.Console.Clear();
    System.Console.ForegroundColor = ConsoleColor.Black;
  }
}