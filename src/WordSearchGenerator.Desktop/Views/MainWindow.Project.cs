using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using WordSearchGenerator.Desktop.Localization;
using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Services.Persistence;

namespace WordSearchGenerator.Desktop.Views
{
  public partial class MainWindow
  {
    #region Fields

    private bool _closingAfterConfirmation;
    private string? _lastProjectDirectory;

    #endregion

    #region Other Stuff

    private async Task<bool> ConfirmUnsavedChangesAsync()
    {
      if (!_viewModel.IsDirty)
      {
        return true;
      }

      var result = MessageBox.Show(
        this,
        AppStrings.Get("UnsavedChangesMessage"),
        AppStrings.Get("UnsavedChangesTitle"),
        MessageBoxButton.YesNoCancel,
        MessageBoxImage.Warning);

      return result switch
      {
        MessageBoxResult.Yes => await SaveProjectAsync(false),
        MessageBoxResult.No => true,
        _ => false
      };
    }

    private void ExitOnClick(object sender, RoutedEventArgs e)
    {
      Close();
    }

    private async void ImportEntriesOnClick(
      object sender,
      RoutedEventArgs e)
    {
      await ImportEntriesAsync();
    }

    private async Task ImportEntriesAsync()
    {
      if (!_viewModel.IsEditorEnabled)
      {
        return;
      }

      var dialog = new OpenFileDialog
      {
        AddExtension = true,
        CheckFileExists = true,
        DefaultExt = ".txt",
        Filter = _viewModel.Mode == PuzzleMode.Normal
          ? AppStrings.Get("TextFilesFilter")
          : AppStrings.Get("TabTextFilesFilter"),
        InitialDirectory = GetInitialDirectory(),
        Multiselect = false,
        Title = _viewModel.Mode == PuzzleMode.Normal
          ? AppStrings.Get("ImportWordsTitle")
          : AppStrings.Get("ImportQuizTitle")
      };

      if (dialog.ShowDialog(this) != true)
      {
        return;
      }

      try
      {
        _viewModel.ReportExportStarted(AppStrings.Get("ImportingEntries"));
        var utf8 = new UTF8Encoding(
          false,
          true);
        var source = await File.ReadAllTextAsync(dialog.FileName, utf8);
        var entries =
          _viewModel.Mode == PuzzleMode.Normal
            ? PuzzleInputFileParser.ParseWords(source)
            : PuzzleInputFileParser.ParseQuizEntries(source);

        _viewModel.ApplyImportedEntries(entries);
        _lastProjectDirectory = Path.GetDirectoryName(dialog.FileName);
        _viewModel.ReportExportCompleted(AppStrings.Format(
          "ImportedCount",
          entries.Count,
          AppStrings.Get(_viewModel.Mode == PuzzleMode.Normal
            ? "WordsCountNoun"
            : "QuizEntriesCountNoun")));
      }
      catch (Exception exception)
      {
        _viewModel.ReportExportFailed(AppStrings.Get("ImportFailed"));
        MessageBox.Show(
          this,
          exception.Message,
          AppStrings.Get("CouldNotImport"),
          MessageBoxButton.OK,
          MessageBoxImage.Error);
      }
    }

    private string? GetInitialDirectory()
    {
      if (_viewModel.ProjectFilePath != null)
      {
        return Path.GetDirectoryName(_viewModel.ProjectFilePath);
      }

      return _lastProjectDirectory;
    }

    private static string GetSuggestedProjectName(string heading)
    {
      var name = heading.Trim();

      if (name.Length == 0)
      {
        return AppStrings.Get("UntitledProjectFile");
      }

      foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
      {
        name = name.Replace(invalidCharacter, '_');
      }

      return $"{name}.wosecon";
    }

    private async void MainWindowOnClosing(
      object? sender,
      CancelEventArgs e)
    {
      if (!_closingAfterConfirmation && _viewModel.IsDirty)
      {
        e.Cancel = true;

        if (await ConfirmUnsavedChangesAsync())
        {
          _closingAfterConfirmation = true;
          _ = Dispatcher.BeginInvoke(
            DispatcherPriority.ApplicationIdle,
            new Action(Close));
        }

        return;
      }

      SaveLayout();
    }

    private async void MainWindowOnPreviewKeyDown(
      object sender,
      KeyEventArgs e)
    {
      var modifiers = Keyboard.Modifiers;

      switch (e.Key, modifiers)
      {
        case (Key.N, ModifierKeys.Control):
          e.Handled = true;
          if (_viewModel.IsEditorEnabled)
          {
            await NewProjectAsync();
          }

          break;
        case (Key.O, ModifierKeys.Control):
          e.Handled = true;
          if (_viewModel.IsEditorEnabled)
          {
            await OpenProjectAsync();
          }

          break;
        case (Key.S, ModifierKeys.Control):
          e.Handled = true;
          if (_viewModel.IsEditorEnabled)
          {
            await SaveProjectAsync(false);
          }

          break;
        case (Key.S, ModifierKeys.Control | ModifierKeys.Shift):
          e.Handled = true;
          if (_viewModel.IsEditorEnabled)
          {
            await SaveProjectAsync(true);
          }

          break;
        case (Key.I, ModifierKeys.Control):
          e.Handled = true;
          if (_viewModel.IsEditorEnabled)
          {
            await ImportEntriesAsync();
          }

          break;
        case (Key.P, ModifierKeys.Control):
          e.Handled = true;
          if (_viewModel.CanExportBrowserPreview)
          {
            PrintCurrentPreviewOnClick(sender, e);
          }

          break;
        case (Key.P, ModifierKeys.Control | ModifierKeys.Shift):
          e.Handled = true;
          if (_viewModel.CanExportBrowserPreview)
          {
            SaveCurrentPreviewAsPdfOnClick(sender, e);
          }

          break;
        case (Key.H, ModifierKeys.Control | ModifierKeys.Shift):
          e.Handled = true;
          if (_viewModel.CanExport)
          {
            SaveCurrentPreviewAsHtmlOnClick(sender, e);
          }

          break;
        case (Key.W, ModifierKeys.Control | ModifierKeys.Shift):
          e.Handled = true;
          if (_viewModel.CanExport)
          {
            SaveCurrentPreviewAsDocxOnClick(sender, e);
          }

          break;
        case (Key.G, ModifierKeys.Control | ModifierKeys.Shift):
          e.Handled = true;
          if (_viewModel.CanExport)
          {
            SaveCurrentBoardAsPngOnClick(sender, e);
          }

          break;
        case (Key.OemComma, ModifierKeys.Control):
          e.Handled = true;
          if (_viewModel.IsEditorEnabled)
          {
            SettingsOnClick(sender, e);
          }

          break;
        case (Key.F1, ModifierKeys.None):
          e.Handled = true;
          AboutOnClick(sender, e);
          break;
      }
    }

    private async void NewProjectOnClick(
      object sender,
      RoutedEventArgs e)
    {
      await NewProjectAsync();
    }

    private async Task NewProjectAsync()
    {
      if (!_viewModel.IsEditorEnabled ||
          !await ConfirmUnsavedChangesAsync())
      {
        return;
      }

      _viewModel.NewProject();
    }

    private async void OpenProjectOnClick(
      object sender,
      RoutedEventArgs e)
    {
      await OpenProjectAsync();
    }

    private async Task OpenProjectAsync()
    {
      if (!_viewModel.IsEditorEnabled ||
          !await ConfirmUnsavedChangesAsync())
      {
        return;
      }

      var dialog = new OpenFileDialog
      {
        AddExtension = true,
        CheckFileExists = true,
        DefaultExt = ".wosecon",
        Filter = AppStrings.Get("ProjectFilesFilter"),
        InitialDirectory = GetInitialDirectory(),
        Multiselect = false,
        Title = AppStrings.Get("OpenProjectTitle")
      };

      if (dialog.ShowDialog(this) != true)
      {
        return;
      }

      try
      {
        _viewModel.ReportExportStarted(AppStrings.Get("OpeningProject"));
        var project = await _projectSerializer.LoadAsync(
          dialog.FileName);
        _viewModel.LoadProject(project, dialog.FileName);
        _lastProjectDirectory = Path.GetDirectoryName(dialog.FileName);
        _viewModel.ReportExportCompleted(AppStrings.Get("ProjectLoaded"));
      }
      catch (Exception exception)
      {
        _viewModel.ReportExportFailed(AppStrings.Get("OpenFailed"));
        MessageBox.Show(
          this,
          exception.Message,
          AppStrings.Get("CouldNotOpenProject"),
          MessageBoxButton.OK,
          MessageBoxImage.Error);
      }
    }

    private async void SaveProjectAsOnClick(
      object sender,
      RoutedEventArgs e)
    {
      await SaveProjectAsync(true);
    }

    private async void SaveProjectOnClick(
      object sender,
      RoutedEventArgs e)
    {
      await SaveProjectAsync(false);
    }

    private async Task<bool> SaveProjectAsync(bool forceSaveAs)
    {
      if (!_viewModel.IsEditorEnabled)
      {
        return false;
      }

      if (!_viewModel.TryCreateProjectSnapshot(
            out var definition,
            out var generatedResult) ||
          definition == null)
      {
        MessageBox.Show(
          this,
          AppStrings.Get("CorrectSettingsBeforeSaving"),
          AppStrings.Get("PuzzleCannotBeSaved"),
          MessageBoxButton.OK,
          MessageBoxImage.Warning);
        return false;
      }

      var path = forceSaveAs ? null : _viewModel.ProjectFilePath;

      if (path == null)
      {
        var dialog = new SaveFileDialog
        {
          AddExtension = true,
          DefaultExt = ".wosecon",
          FileName = GetSuggestedProjectName(definition.PuzzleHeading),
          Filter = AppStrings.Get("ProjectSaveFilter"),
          InitialDirectory = GetInitialDirectory(),
          OverwritePrompt = true,
          Title = AppStrings.Get("SaveProjectTitle")
        };

        if (dialog.ShowDialog(this) != true)
        {
          return false;
        }

        path = dialog.FileName;
      }

      try
      {
        _viewModel.ReportExportStarted(AppStrings.Get("SavingProject"));
        await _projectSerializer.SaveAsync(
          path,
          definition,
          generatedResult);
        _viewModel.MarkProjectSaved(path);
        _lastProjectDirectory = Path.GetDirectoryName(path);
        _viewModel.ReportExportCompleted(AppStrings.Get("ProjectSaved"));
        return true;
      }
      catch (Exception exception)
      {
        _viewModel.ReportExportFailed(AppStrings.Get("SaveFailed"));
        MessageBox.Show(
          this,
          exception.Message,
          AppStrings.Get("CouldNotSaveProject"),
          MessageBoxButton.OK,
          MessageBoxImage.Error);
        return false;
      }
    }

    #endregion
  }
}