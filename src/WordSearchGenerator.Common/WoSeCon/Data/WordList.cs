namespace WordSearchGenerator.Common.WoSeCon.Data
{
  public class WordList
  {
    #region Properties

    public WordInfo this[int i]
    {
      get => Words[i];
      set => Words[i] = value;
    }

    public int Size => (Words?.Count).GetValueOrDefault(0);

    private List<WordInfo> Words { get; } = new List<WordInfo>();

    #endregion
  }
}