using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Wose.Desktop.Models;

namespace Wose.Desktop.Views.Controls
{
  public sealed class SharedLetterTextEditor : TextEditor
  {
    public static readonly DependencyProperty BoundTextProperty =
      DependencyProperty.Register(
        nameof(BoundText),
        typeof(string),
        typeof(SharedLetterTextEditor),
        new FrameworkPropertyMetadata(
          string.Empty,
          FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
          OnBoundTextChanged));

    public static readonly DependencyProperty SharedLetterBrushProperty =
      DependencyProperty.Register(
        nameof(SharedLetterBrush),
        typeof(Brush),
        typeof(SharedLetterTextEditor),
        new PropertyMetadata(Brushes.DodgerBlue, OnSharedLetterBrushChanged));

    private readonly SharedLetterColorizer _colorizer;
    private bool _isSynchronizingText;

    public string BoundText
    {
      get => (string)GetValue(BoundTextProperty);
      set => SetValue(BoundTextProperty, value);
    }

    public Brush SharedLetterBrush
    {
      get => (Brush)GetValue(SharedLetterBrushProperty);
      set => SetValue(SharedLetterBrushProperty, value);
    }

    public SharedLetterTextEditor()
    {
      _colorizer = new SharedLetterColorizer(SharedLetterBrush);
      TextArea.TextView.LineTransformers.Add(_colorizer);
      TextChanged += OnEditorTextChanged;
    }

    private static void OnBoundTextChanged(
      DependencyObject dependencyObject,
      DependencyPropertyChangedEventArgs e)
    {
      var editor = (SharedLetterTextEditor)dependencyObject;
      var newText = (string?)e.NewValue ?? string.Empty;

      if (editor._isSynchronizingText || editor.Text == newText)
      {
        return;
      }

      var caretOffset = editor.CaretOffset;
      editor._isSynchronizingText = true;
      editor.Text = newText;
      editor.CaretOffset = Math.Min(caretOffset, newText.Length);
      editor._isSynchronizingText = false;
      editor.UpdateSharedLetters();
    }

    private static void OnSharedLetterBrushChanged(
      DependencyObject dependencyObject,
      DependencyPropertyChangedEventArgs e)
    {
      var editor = (SharedLetterTextEditor)dependencyObject;
      editor._colorizer.Brush = (Brush)e.NewValue;
      editor.TextArea.TextView.Redraw();
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
      UpdateSharedLetters();

      if (_isSynchronizingText)
      {
        return;
      }

      _isSynchronizingText = true;
      SetCurrentValue(BoundTextProperty, Text);
      _isSynchronizingText = false;
    }

    private void UpdateSharedLetters()
    {
      _colorizer.SharedLetters = SharedLetterAnalyzer.FindSharedLetters(Text);
      TextArea.TextView.Redraw();
    }

    private sealed class SharedLetterColorizer(Brush brush)
      : DocumentColorizingTransformer
    {
      public Brush Brush
      {
        get;
        set;
      } = brush;

      public IReadOnlySet<char> SharedLetters
      {
        get;
        set;
      } = new HashSet<char>();

      protected override void ColorizeLine(DocumentLine line)
      {
        var lineText = CurrentContext.Document.GetText(line);

        for (var index = 0; index < lineText.Length; index++)
        {
          if (!SharedLetters.Contains(lineText[index]))
          {
            continue;
          }

          var offset = line.Offset + index;
          ChangeLinePart(
            offset,
            offset + 1,
            element =>
            {
              element.TextRunProperties.SetForegroundBrush(Brush);
              element.TextRunProperties.SetTypeface(
                new Typeface(
                  element.TextRunProperties.Typeface.FontFamily,
                  element.TextRunProperties.Typeface.Style,
                  FontWeights.SemiBold,
                  element.TextRunProperties.Typeface.Stretch));
            });
        }
      }
    }
  }
}
