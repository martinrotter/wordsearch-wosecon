using CommandLine;
using WordSearchGenerator.Common.WoSeCon;
using WordSearchGenerator.Common.WoSeCon.Data;

namespace WordSearchGenerator.Console
{
  internal class App
  {
    #region Vlastnosti

    private CliOptions Options
    {
      get;
    }

    private Common.WordSearchGenerator Generator
    {
      get;
    } = new Common.WordSearchGenerator();

    private ManualResetEvent Handle
    {
      get;
    } = new ManualResetEvent(false);

    #endregion

    #region Konstruktory

    public App(string[] args)
    {
      ConsoleUtils.SetupConsole();

      if (args?.Length == 0)
      {
        args = new[]
        {
          "--help"
        };
      }

      try
      {
        ParserResult<CliOptions> cliParseResult = Parser.Default.ParseArguments<CliOptions>(args);

        if (cliParseResult.Errors.Any())
        {
          foreach (Error error in cliParseResult.Errors)
          {
            if (error.Tag != ErrorType.HelpRequestedError)
            {
              ConsoleUtils.WithBgColor(() => { System.Console.WriteLine(error.Tag); }, ConsoleColor.Red);
            }
          }

          Quit();
        }
        else
        {
          Options = cliParseResult.Value;
        }
      }
      catch (Exception ex)
      {
        ConsoleUtils.WithBgColor(() => { System.Console.WriteLine(ex.Message); }, ConsoleColor.Red);

        Quit();
      }

      Task.Run(() =>
      {
        Words words = new Words(Options.WordsFile);
        WoSeCon wo = new WoSeCon(words.List, Options.Rows, Options.Columns);

        wo.Construct();

        Board board = new Board(wo.Words, wo.RowCount, wo.ColumnCount, Options.Message);

        board.PrintToConsole(); 
        board.PrintWordsToConsole(Options.Debug);
      }).ContinueWith(tsk => { Quit(); });
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