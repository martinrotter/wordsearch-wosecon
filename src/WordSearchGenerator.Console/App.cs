using CommandLine;
using WordSearchGenerator.Common.WoSeCon;
using WordSearchGenerator.Common.WoSeCon.Data;

namespace WordSearchGenerator.Console
{
  internal class App
  {
    #region Properties

    private ManualResetEvent Handle { get; } = new ManualResetEvent(false);

    private CliOptions Options { get; }

    #endregion

    #region Constructors

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
        var cliParseResult = Parser.Default.ParseArguments<CliOptions>(args);

        if (cliParseResult.Errors.Any())
        {
          foreach (var error in cliParseResult.Errors)
          {
            if (error.Tag != ErrorType.HelpRequestedError)
            {
              throw new Exception(error.Tag.ToString());
            }
          }
        }
        else
        {
          Options = cliParseResult.Value;
        }
      }
      catch (Exception ex)
      {
        QuitCheckException(ex);
      }

      Task.Run(() =>
      {
        var words = new Words(Options.WordsFile);
        var wo = new WoSeCon(words.List, Options.Rows, Options.Columns);

        wo.Construct();

        var board = new Board(wo.Words, wo.RowCount, wo.ColumnCount, Options.Message);

        board.PrintToConsole();
        board.PrintWordsToConsole(Options.Debug);
      }).ContinueWith(AboutToQuit);
    }

    #endregion

    #region Other Stuff

    private void AboutToQuit(Task tsk)
    {
      QuitCheckException(tsk.Exception);
    }

    private void QuitCheckException(Exception exception)
    {
      if (exception != null)
      {
        System.Console.WriteLine($"Error: {exception.Message}.");
        Environment.ExitCode = 1;
      }
      else
      {
        Environment.ExitCode = 0;
      }

      Quit();
    }

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