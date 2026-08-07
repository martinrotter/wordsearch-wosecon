using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Models.Persistence;
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
        "The current puzzle has unsaved changes. Do you want to save them?",
        "Unsaved changes",
        MessageBoxButton.YesNoCancel,
        MessageBoxImage.Warning);

      return result switch
      {
        MessageBoxResult.Yes => await SaveProjectAsync(forceSaveAs: false),
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
          ? "Text files (*.txt)|*.txt|All files (*.*)|*.*"
          : "Tab-separated text (*.txt;*.tsv)|*.txt;*.tsv|All files (*.*)|*.*",
        InitialDirectory = GetInitialDirectory(),
        Multiselect = false,
        Title = _viewModel.Mode == PuzzleMode.Normal
          ? "Import words"
          : "Import quiz answers and questions"
      };

      if (dialog.ShowDialog(this) != true)
      {
        return;
      }

      try
      {
        _viewModel.ReportExportStarted("Importing entries");
        var utf8 = new UTF8Encoding(
          encoderShouldEmitUTF8Identifier: false,
          throwOnInvalidBytes: true);
        var source = await File.ReadAllTextAsync(dialog.FileName, utf8);
        IReadOnlyList<PuzzleEntry> entries =
          _viewModel.Mode == PuzzleMode.Normal
            ? PuzzleInputFileParser.ParseWords(source)
            : PuzzleInputFileParser.ParseQuizEntries(source);

        _viewModel.ApplyImportedEntries(entries);
        _lastProjectDirectory = Path.GetDirectoryName(dialog.FileName);
        _viewModel.ReportExportCompleted(
          $"Imported {entries.Count} " +
          (_viewModel.Mode == PuzzleMode.Normal ? "words" : "quiz entries"));
      }
      catch (Exception exception)
      {
        _viewModel.ReportExportFailed("Import failed");
        MessageBox.Show(
          this,
          exception.Message,
          "Could not import entries",
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
        return "Untitled.wosecon";
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
      if (_closingAfterConfirmation || !_viewModel.IsDirty)
      {
        return;
      }

      e.Cancel = true;

      if (await ConfirmUnsavedChangesAsync())
      {
        _closingAfterConfirmation = true;
        _ = Dispatcher.BeginInvoke(
          DispatcherPriority.ApplicationIdle,
          new Action(Close));
      }
    }

    private async void MainWindowOnPreviewKeyDown(
      object sender,
      KeyEventArgs e)
    {
      if ((Keyboard.Modifiers & ModifierKeys.Control) == 0 ||
          !_viewModel.IsEditorEnabled)
      {
        return;
      }

      switch (e.Key)
      {
        case Key.N:
          e.Handled = true;
          await NewProjectAsync();
          break;
        case Key.O:
          e.Handled = true;
          await OpenProjectAsync();
          break;
        case Key.S:
          e.Handled = true;
          await SaveProjectAsync(
            forceSaveAs:
            (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
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
        Filter = "WoSeCon projects (*.wosecon)|*.wosecon|All files (*.*)|*.*",
        InitialDirectory = GetInitialDirectory(),
        Multiselect = false,
        Title = "Open WoSeCon project"
      };

      if (dialog.ShowDialog(this) != true)
      {
        return;
      }

      try
      {
        _viewModel.ReportExportStarted("Opening project");
        PuzzleProject project = await _projectSerializer.LoadAsync(
          dialog.FileName);
        _viewModel.LoadProject(project, dialog.FileName);
        _lastProjectDirectory = Path.GetDirectoryName(dialog.FileName);
        _viewModel.ReportExportCompleted("Project loaded");
      }
      catch (Exception exception)
      {
        _viewModel.ReportExportFailed("Open failed");
        MessageBox.Show(
          this,
          exception.Message,
          "Could not open project",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
      }
    }

    private async void SaveProjectAsOnClick(
      object sender,
      RoutedEventArgs e)
    {
      await SaveProjectAsync(forceSaveAs: true);
    }

    private async void SaveProjectOnClick(
      object sender,
      RoutedEventArgs e)
    {
      await SaveProjectAsync(forceSaveAs: false);
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
          "Correct the highlighted puzzle settings before saving.",
          "Puzzle cannot be saved",
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
          Filter = "WoSeCon projects (*.wosecon)|*.wosecon",
          InitialDirectory = GetInitialDirectory(),
          OverwritePrompt = true,
          Title = "Save WoSeCon project"
        };

        if (dialog.ShowDialog(this) != true)
        {
          return false;
        }

        path = dialog.FileName;
      }

      try
      {
        _viewModel.ReportExportStarted("Saving project");
        await _projectSerializer.SaveAsync(
          path,
          definition,
          generatedResult);
        _viewModel.MarkProjectSaved(path);
        _lastProjectDirectory = Path.GetDirectoryName(path);
        _viewModel.ReportExportCompleted("Project saved");
        return true;
      }
      catch (Exception exception)
      {
        _viewModel.ReportExportFailed("Save failed");
        MessageBox.Show(
          this,
          exception.Message,
          "Could not save project",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
        return false;
      }
    }

    #endregion
  }
}
