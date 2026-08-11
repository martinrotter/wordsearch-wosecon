using System.Windows;
using Wose.Desktop.Views.Dialogs;

namespace Wose.Desktop.Views
{
  public partial class MainWindow
  {
    #region Other Stuff

    private void AboutOnClick(object sender, RoutedEventArgs e)
    {
      var dialog = new AboutWindow
      {
        Owner = this
      };

      dialog.ShowDialog();
    }

    #endregion
  }
}
