namespace Wose.Desktop.Models
{
  internal static class SharedLetterAnalyzer
  {
    public static IReadOnlySet<char> FindSharedLetters(string? source)
    {
      var words = PuzzleInputParser.ParseWords(source);
      var wordCounts = new Dictionary<char, int>();

      foreach (var word in words)
      {
        foreach (var character in word.Answer
                     .Where(character => !char.IsWhiteSpace(character))
                     .Distinct())
        {
          wordCounts[character] = wordCounts.GetValueOrDefault(character) + 1;
        }
      }

      return wordCounts
        .Where(pair => pair.Value >= 2)
        .Select(pair => pair.Key)
        .ToHashSet();
    }
  }
}
