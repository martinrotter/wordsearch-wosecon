using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using WordSearchGenerator.Common.WoSeCon.Api;

namespace WordSearchGenerator.Common
{
  public class WordsLoader
  {
    #region Properties

    public List<WordInfo> Words
    {
      get;
    }

    #endregion

    #region Constructors

    public WordsLoader(string fileName, bool processCharacters = true)
    {
      Words = ProcessString(File.ReadAllText(fileName, Encoding.UTF8), processCharacters);
    }

    #endregion

    #region Other Stuff

    private List<WordInfo> ProcessString(string lines, bool processCharacters)
    {
      return lines.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries).Select(txt => new WordInfo
      {
        Text = processCharacters ? ProcessCharacters(txt) : txt,
        PrintableText = txt
      }).ToList();
    }

    private static string RemoveDiacritics(string text)
    {
      string normalizedString = text.Normalize(NormalizationForm.FormD);
      StringBuilder stringBuilder = new StringBuilder(normalizedString.Length);

      foreach (char c in normalizedString)
      {
        UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);

        if (unicodeCategory != UnicodeCategory.NonSpacingMark)
        {
          stringBuilder.Append(c);
        }
      }

      return stringBuilder
        .ToString()
        .Normalize(NormalizationForm.FormC);
    }

    private string ProcessCharacters(string txt)
    {
      return Regex.Replace(RemoveDiacritics(txt), "[^a-zA-Z0-9]", string.Empty).ToUpperInvariant();
    }

    #endregion
  }
}