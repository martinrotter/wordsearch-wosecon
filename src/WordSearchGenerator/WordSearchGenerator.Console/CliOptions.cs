using CommandLine;

namespace WordSearchGenerator.Console
{
  internal class CliOptions
  {
    #region Vlastnosti

    [Option(
      'd',
      Default = false,
      HelpText = "Also print solution and other information for debugging purposes.")]
    public bool Debug
    {
      get;
      set;
    }


    [Option(
      'c',
      Default = 20,
      HelpText = "Specify number of columns for the puzzle.")]
    public int Columns
    {
      get;
      set;
    }

    [Option(
      'r',
      Default = 20,
      HelpText = "Specify number of rows for the puzzle.")]
    public int Rows
    {
      get;
      set;
    }

    [Option('w',
      "words",
      Required = true,
      HelpText = "Specify words file name. One word per line is expected.")]
    public string WordsFile
    {
      get;
      set;
    }

    [Option('m', HelpText = "Target message to be found when puzzle is completed.")]
    public string Message
    {
      get;
      set;
    }

    #endregion
  }
}