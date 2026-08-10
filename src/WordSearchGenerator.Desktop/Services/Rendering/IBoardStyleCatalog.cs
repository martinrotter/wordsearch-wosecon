namespace Wose.Desktop.Services.Rendering
{
  public interface IBoardStyleCatalog
  {
    #region Properties

    string DefaultStyleId
    {
      get;
    }

    IReadOnlyList<string> StyleIds
    {
      get;
    }

    #endregion

    #region Other Stuff

    bool Contains(string styleId);

    string GetCss(string styleId);

    #endregion
  }
}
