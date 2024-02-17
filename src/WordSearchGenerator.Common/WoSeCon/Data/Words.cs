using System.Text;

namespace WordSearchGenerator.Common.WoSeCon.Data;

public class Words
{
  #region Properties

  public WordInfo this[int i]
  {
    get => List[i];
    set => List[i] = value;
  }

  public List<WordInfo> List { get; }

  public int Size => (List?.Count).GetValueOrDefault(0);

  #endregion

  #region Constructors

  public Words(string fileName)
  {
    List = File
      .ReadAllText(fileName, Encoding.UTF8)
      .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
      .Select(txt => new WordInfo
      {
        Text = txt,
        TestedLocations = new List<DirectedLocation>()
      }).ToList();
  }

  #endregion
}