using System.Text;
using CommandLine;
using WordSearchGenerator.Common;
using WordSearchGenerator.Common.WoSeCon;

namespace WordSearchGenerator.Console
{
  internal class App
  {
    #region Properties

    private ManualResetEvent Handle
    {
      get;
    } = new ManualResetEvent(false);

    private CliOptions Options
    {
      get;
    }

    #endregion

    #region Constructors

    public App(string[] args)
    {
      ConsoleUtils.SetupConsole();

      if (args?.Length == 0 && !System.Console.IsInputRedirected)
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
              throw new Exception(error.Tag.ToString());
            }
            else
            {
              Quit();
            }
          }

          Options = new CliOptions();
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
        string loadedWords = null;

        if (System.Console.IsInputRedirected)
        {
          using (StreamReader rdr = new StreamReader(System.Console.OpenStandardInput()))
          {
            loadedWords = rdr.ReadToEnd();
          }
        }
        else
        {
          loadedWords = File.ReadAllText(Options.WordsFile, Encoding.UTF8);
        }

        WordsLoader words = new WordsLoader(loadedWords, Options.ProcessWords);
        WoSeCon wo = new WoSeCon(
          words.Words,
          Options.Rows,
          Options.Columns,
          Options.QuizMode);

        wo.Construct(null);

        Board board = new Board(
          wo.Words,
          wo.RowCount,
          wo.ColumnCount,
          Options.HtmlOutput,
          Options.QuizMode,
          Options.BlindRate,
          Options.Message);

        System.Console.Write(board.Print(Options.HtmlOutput, Options.Debug));

        if (Options.Debug && !Options.HtmlOutput)
        {
          System.Console.Write(board.PrintIntersections());
        }
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
      if (exception is AggregateException aggr)
      {
        System.Console.WriteLine($"Error: {string.Join(", ", aggr.InnerExceptions.Select(ex => ex.Message))}.");

        Environment.ExitCode = 1;
      }
      else if (exception != null)
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