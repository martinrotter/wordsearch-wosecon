using System.Text;

namespace WordSearchGenerator.Common.WoSeCon.Data
{
  public class Words
  {
    #region Vlastnosti

    public WordInfo this[int i]
    {
      get => List[i];
      set => List[i] = value;
    }

    public int Size
    {
      get => (List?.Count).GetValueOrDefault(0);
    }

    public List<WordInfo> List
    {
      get;
    }

    #endregion

    #region Konstruktory

    public Words(string fileName)
    {
      List = File.ReadAllText(fileName, Encoding.UTF8).Split(Environment.NewLine).Select(txt => new WordInfo
      {
        Text = txt,
        TestedLocations = new List<DirectedLocation>()
      }).ToList();
    }

    #endregion
  }
}