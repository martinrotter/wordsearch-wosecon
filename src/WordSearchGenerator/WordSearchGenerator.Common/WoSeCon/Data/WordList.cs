namespace WordSearchGenerator.Common.WoSeCon.Data
{
  public class WordList
  {
    #region Vlastnosti

    public WordInfo this[int i]
    {
      get => Words[i];
      set => Words[i] = value;
    }

    public int Size
    {
      get => (Words?.Count).GetValueOrDefault(0);
    }

    private List<WordInfo> Words
    {
      get;
      set;
    } = new List<WordInfo>();

    #endregion
  }
}