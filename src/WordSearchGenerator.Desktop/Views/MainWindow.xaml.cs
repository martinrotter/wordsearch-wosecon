using System.Windows;
using WordSearchGenerator.Desktop.ViewModels;

namespace WordSearchGenerator.Desktop.Views
{
  public partial class MainWindow : Window
  {
    public MainWindow(MainWindowViewModel viewModel)
    {
      InitializeComponent();
      DataContext = viewModel;
    }
  }
}
