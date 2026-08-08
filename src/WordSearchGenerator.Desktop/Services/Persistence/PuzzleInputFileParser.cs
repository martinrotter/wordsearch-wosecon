using System.IO;
using WordSearchGenerator.Desktop.Localization;
using WordSearchGenerator.Desktop.Models;

namespace WordSearchGenerator.Desktop.Services.Persistence
{
  public static class PuzzleInputFileParser
  {
    #region Other Stuff

    public static IReadOnlyList<PuzzleEntry> ParseQuizEntries(string source)
    {
      ArgumentNullException.ThrowIfNull(source);

      var rawEntries = new List<PuzzleEntry>();
      var lines = source.ReplaceLineEndings("\n").Split('\n');

      for (var index = 0; index < lines.Length; index++)
      {
        var line = lines[index];

        if (string.IsNullOrWhiteSpace(line))
        {
          continue;
        }

        var separatorIndex = line.IndexOf('\t');

        if (separatorIndex < 0)
        {
          throw new InvalidDataException(AppStrings.Format(
            "QuizImportLineFormat",
            index + 1));
        }

        var answer = line[..separatorIndex].Trim();
        var question = line[(separatorIndex + 1)..].Trim();

        if (answer.Length < 2 || question.Length == 0)
        {
          throw new InvalidDataException(AppStrings.Format(
            "QuizImportLineInvalid",
            index + 1));
        }

        rawEntries.Add(new PuzzleEntry(answer, question));
      }

      var entries = PuzzleInputParser.ParseQuizEntries(rawEntries);

      if (entries.Count == 0)
      {
        throw new InvalidDataException(AppStrings.Get("NoQuizEntriesInFile"));
      }

      return entries;
    }

    public static IReadOnlyList<PuzzleEntry> ParseWords(string source)
    {
      ArgumentNullException.ThrowIfNull(source);

      var entries = PuzzleInputParser.ParseWords(source);

      if (entries.Count == 0)
      {
        throw new InvalidDataException(AppStrings.Get("NoWordsInFile"));
      }

      var shortWord =
        entries.FirstOrDefault(entry => entry.Answer.Length < PuzzleInputParser.MinimumWordLength);

      if (shortWord != null)
      {
        throw new InvalidDataException(AppStrings.Format(
          "WordMinimumLength",
          shortWord.Answer));
      }

      return entries;
    }

    #endregion
  }
}