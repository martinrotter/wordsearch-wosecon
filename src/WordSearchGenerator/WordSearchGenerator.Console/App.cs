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
        string message = null;
        Words words = new Words("words.txt");
        WoSeCon wo = new WoSeCon(words.List, 24, 23);

        wo.Construct();

        var board = new Board(wo.Words, wo.RowCount, wo.ColumnCount, message);
        var boardStr = board.Print();
        var wordsStr = board.PrintWords();

        System.Console.Write(boardStr);
        System.Console.WriteLine(wordsStr);

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