namespace WordSearchGenerator.Desktop.Models
{
  public static class PuzzleInputParser
  {
    #region Static Fields

    public const int MinimumWordLength = 3;

    #endregion

    #region Other Stuff

    public static IReadOnlyList<PuzzleEntry> ParseQuizEntries(
      IEnumerable<PuzzleEntry> source)
    {
      ArgumentNullException.ThrowIfNull(source);

      var answers = new HashSet<string>(StringComparer.Ordinal);
      var result = new List<PuzzleEntry>();

      foreach (var entry in source)
      {
        if (entry == null)
        {
          continue;
        }

        var answer = entry.Answer?.Trim() ?? string.Empty;
        var question = entry.Question?.Trim() ?? string.Empty;

        if (answer.Length == 0 && question.Length == 0)
        {
          continue;
        }

        if (!answers.Add(answer))
        {
          continue;
        }

        result.Add(new PuzzleEntry(answer, question));
      }

      return result;
    }

    public static IReadOnlyList<PuzzleEntry> ParseWords(string? source)
    {
      if (string.IsNullOrEmpty(source))
      {
        return [];
      }

      var words = new HashSet<string>(StringComparer.Ordinal);
      var result = new List<PuzzleEntry>();

      foreach (var line in source.ReplaceLineEndings("\n").Split('\n'))
      {
        var word = line.Trim();

        if (word.Length == 0 || !words.Add(word))
        {
          continue;
        }

        result.Add(new PuzzleEntry(word));
      }

      return result;
    }

    #endregion
  }
}