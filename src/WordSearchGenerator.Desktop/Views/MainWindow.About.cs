using System.Windows;
using WordSearchGenerator.Desktop.Views.Dialogs;

namespace WordSearchGenerator.Desktop.Views
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