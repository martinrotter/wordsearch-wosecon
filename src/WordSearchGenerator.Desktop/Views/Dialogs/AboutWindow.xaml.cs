using System.Reflection;
using System.Windows;
using Wose.Desktop.Localization;

namespace Wose.Desktop.Views.Dialogs
{
  public partial class AboutWindow : Window
  {
    #region Properties

    public string VersionText
    {
      get;
    }

    #endregion

    #region Constructors

    public AboutWindow()
    {
      VersionText = AppStrings.Format(
        "AboutVersion",
        GetInformationalVersion());

      InitializeComponent();
      DataContext = this;
    }

    #endregion

    #region Other Stuff

    private static string GetInformationalVersion()
    {
      var assembly = typeof(App).Assembly;

      return assembly
               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
               .InformationalVersion ??
             assembly.GetName().Version?.ToString() ??
             string.Empty;
    }

    #endregion
  }
}
