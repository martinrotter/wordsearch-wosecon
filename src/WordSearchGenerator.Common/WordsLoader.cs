using System.Text;
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

    public WordsLoader(string fileName)
    {
      Words = File.ReadAllText(fileName, Encoding.UTF8).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(txt => new WordInfo { Text = txt }).ToList();
    }

    #endregion
  }
}