﻿using CommandLine;

namespace WordSearchGenerator.Console
{
  internal class CliOptions
  {
    #region Properties

    [Option('c', Default = 20, HelpText = "Specify number of columns for the puzzle.")]
    public int Columns
    {
      get;
      set;
    }

    [Option('d', Default = false, HelpText = "Also print solution and other information for debugging purposes.")]
    public bool Debug
    {
      get;
      set;
    }

    [Option('b', Default = 0.0, HelpText = "Number from 0.0 to 1.0. Defines % of cells to be marked as " +
                                           "'hidden' so that puzzle solving person has to determine " +
                                           "what character belongs to those hidden spots.")]
    public double BlindRate
    {
      get;
      set;
    }

    [Option('h', Default = false, HelpText = "Output the puzzle in a nicely formatted HTML document. " +
                                             "Use redirection to save to some file.")]

    public bool HtmlOutput
    {
      get;
      set;
    }

    [Option(
      'p',
      Default = true,
      HelpText = "Process input wordlist - replace accented characters, " +
                 "convert to uppercase and remove spaces and other non-word characters.")]
    public bool ProcessWords
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

    [Option('r', Default = 20, HelpText = "Specify number of rows for the puzzle.")]
    public int Rows
    {
      get;
      set;
    }

    [Option('w', "words", Required = true, HelpText = "Specify words file name. One word per line is expected.")]
    public string WordsFile
    {
      get;
      set;
    }

    #endregion
  }
}