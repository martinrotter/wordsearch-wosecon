namespace Wose.Desktop.Models
{
  public static class WordContainmentValidator
  {
    #region Other Stuff

    public static bool TryFindConflict(
      IReadOnlyList<PuzzleEntry> entries,
      out PuzzleEntry? first,
      out PuzzleEntry? second)
    {
      ArgumentNullException.ThrowIfNull(entries);

      var words = entries
        .Select(entry => new WordAndReverse(
          entry,
          Reverse(entry.Answer)))
        .ToArray();

      for (var firstIndex = 0; firstIndex < words.Length - 1; firstIndex++)
      {
        for (var secondIndex = firstIndex + 1;
             secondIndex < words.Length;
             secondIndex++)
        {
          var firstWord = words[firstIndex];
          var secondWord = words[secondIndex];

          if (IsContained(firstWord, secondWord) ||
              IsContained(secondWord, firstWord))
          {
            first = firstWord.Entry;
            second = secondWord.Entry;
            return true;
          }
        }
      }

      first = null;
      second = null;
      return false;
    }

    private static bool IsContained(
      WordAndReverse candidate,
      WordAndReverse container)
    {
      return candidate.Entry.Answer.Length <= container.Entry.Answer.Length &&
             (container.Entry.Answer.Contains(
                candidate.Entry.Answer,
                StringComparison.Ordinal) ||
              container.Entry.Answer.Contains(
                candidate.Reversed,
                StringComparison.Ordinal));
    }

    private static string Reverse(string value)
    {
      return new string(value.Reverse().ToArray());
    }

    #endregion

    #region Nested Types

    private sealed record WordAndReverse(
      PuzzleEntry Entry,
      string Reversed);

    #endregion
  }
}
