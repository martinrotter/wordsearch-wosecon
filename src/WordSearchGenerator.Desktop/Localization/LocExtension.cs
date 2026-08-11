using System.Windows.Markup;

namespace Wose.Desktop.Localization
{
  [MarkupExtensionReturnType(typeof(string))]
  public sealed class LocExtension : MarkupExtension
  {
    #region Properties

    [ConstructorArgument("key")]
    public string Key
    {
      get;
    }

    #endregion

    #region Constructors

    public LocExtension(string key)
    {
      Key = key;
    }

    #endregion

    #region Other Stuff

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return AppStrings.Get(Key);
    }

    #endregion
  }
}
