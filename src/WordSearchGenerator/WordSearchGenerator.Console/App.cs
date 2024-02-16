using System.Diagnostics;
using System.Runtime.InteropServices;
using WordSearchGenerator.Common.WoSeCon;
using WordSearchGenerator.Common.WoSeCon.Data;

namespace WordSearchGenerator.Console
{
  internal class App
  {
    #region Vlastnosti

    private ManualResetEvent Handle
    {
      get;
    } = new ManualResetEvent(false);

    private Common.WordSearchGenerator Generator
    {
      get;
    } = new Common.WordSearchGenerator();

    #endregion

    #region Konstruktory

    public App()
    {
      ConsoleUtils.SetupConsole();

      Task.Run(() =>
      {
        Words words = new Words("words.txt");
        WoSeCon wo = new WoSeCon(words.List, 8, 9);

        wo.Construct();

        var board = new Board(wo.Words, wo.RowCount, wo.ColumnCount);
        var boardStr = board.Print();

        System.Console.Write(boardStr);

        foreach (WordInfo word in wo.Words)
        {
          System.Console.WriteLine(word.ToString());
        }

      }).ContinueWith(tsk => {
        Quit();
      });
    }

    #endregion

    #region Metody

    public void Exec()
    {
      Handle.WaitOne();
    }

    public void Quit()
    {
      Handle.Set();
    }

    #endregion
  }
}