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

    public WordsLoader(string words, bool processCharacters = true)
    {
      Words = ProcessString(words, processCharacters);
    }

    #endregion

    #region Other Stuff

    private List<WordInfo> ProcessString(string lines, bool processCharacters)
    {
      int wordNumber = 1;

      return lines.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries).Select(txt =>
      {
        var wi = new WordInfo
        {
          PrintableText = txt,
          WordNumber = wordNumber++
        };

        if (txt.Contains(Constants.Misc.QuizModeQuestionSeparator))
        {
          var parts = txt.Split(Constants.Misc.QuizModeQuestionSeparator);

          wi.Text = parts[0];
          wi.QuizQuestion = parts[1];
        }
        else
        {
          wi.Text = txt;
        }

        if (processCharacters)
        {
          wi.Text = ProcessCharacters(wi.Text);
        }

        return wi;
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