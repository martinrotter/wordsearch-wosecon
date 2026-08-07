using System.IO;
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
          throw new InvalidDataException(
            $"Line {index + 1} must contain an answer, a tab, and a question.");
        }

        var answer = line[..separatorIndex].Trim();
        var question = line[(separatorIndex + 1)..].Trim();

        if (answer.Length < 2 || question.Length == 0)
        {
          throw new InvalidDataException(
            $"Line {index + 1} must contain an answer of at least two " +
            "characters and a non-empty question.");
        }

        rawEntries.Add(new PuzzleEntry(answer, question));
      }

      var entries = PuzzleInputParser.ParseQuizEntries(rawEntries);

      if (entries.Count == 0)
      {
        throw new InvalidDataException(
          "The selected file contains no complete quiz entries.");
      }

      return entries;
    }

    public static IReadOnlyList<PuzzleEntry> ParseWords(string source)
    {
      ArgumentNullException.ThrowIfNull(source);

      var entries = PuzzleInputParser.ParseWords(source);

      if (entries.Count == 0)
      {
        throw new InvalidDataException(
          "The selected file contains no words.");
      }

      var shortWord = entries.FirstOrDefault(entry => entry.Answer.Length < 2);

      if (shortWord != null)
      {
        throw new InvalidDataException(
          $"The word '{shortWord.Answer}' must contain at least two characters.");
      }

      return entries;
    }

    #endregion
  }
}
