using Wose.Desktop.Models;

namespace Wose.Desktop.Tests
{
  [TestClass]
  public sealed class SharedLetterAnalyzerTests
  {
    [TestMethod]
    public void FindSharedLettersCountsEachDistinctWordOnce()
    {
      var result = SharedLetterAnalyzer.FindSharedLetters(
        "letter\r\nbetter\r\nletter");

      CollectionAssert.AreEquivalent(
        new[] { 'e', 't', 'r' },
        result.ToArray());
    }

    [TestMethod]
    public void FindSharedLettersIsCaseSensitiveAndIgnoresSpaces()
    {
      var result = SharedLetterAnalyzer.FindSharedLetters(
        "Red Fox\r\nblue fox\r\nROAD");

      CollectionAssert.AreEquivalent(
        new[] { 'R', 'e', 'o', 'x' },
        result.ToArray());
    }
  }
}
