using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Wose.Desktop.Models;

namespace Wose.Desktop.Views.Controls
{
  public sealed class SecretMessageSentenceEditor : TextEditor
  {
    public static readonly DependencyProperty BoundTextProperty =
      DependencyProperty.Register(
        nameof(BoundText),
        typeof(string),
        typeof(SecretMessageSentenceEditor),
        new FrameworkPropertyMetadata(
          string.Empty,
          FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
          OnBoundTextChanged));

    public static readonly DependencyProperty MarkerBrushProperty =
      DependencyProperty.Register(
        nameof(MarkerBrush),
        typeof(Brush),
        typeof(SecretMessageSentenceEditor),
        new PropertyMetadata(Brushes.DodgerBlue, OnMarkerBrushChanged));

    private readonly MarkerColorizer _colorizer;
    private bool _isSynchronizingText;

    public string BoundText
    {
      get => (string)GetValue(BoundTextProperty);
      set => SetValue(BoundTextProperty, value);
    }

    public Brush MarkerBrush
    {
      get => (Brush)GetValue(MarkerBrushProperty);
      set => SetValue(MarkerBrushProperty, value);
    }

    public SecretMessageSentenceEditor()
    {
      _colorizer = new MarkerColorizer(MarkerBrush);
      TextArea.TextView.LineTransformers.Add(_colorizer);
      TextChanged += OnEditorTextChanged;
    }

    private static void OnBoundTextChanged(
      DependencyObject dependencyObject,
      DependencyPropertyChangedEventArgs e)
    {
      var editor = (SecretMessageSentenceEditor)dependencyObject;
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
      editor.TextArea.TextView.Redraw();
    }

    private static void OnMarkerBrushChanged(
      DependencyObject dependencyObject,
      DependencyPropertyChangedEventArgs e)
    {
      var editor = (SecretMessageSentenceEditor)dependencyObject;
      editor._colorizer.Brush = (Brush)e.NewValue;
      editor.TextArea.TextView.Redraw();
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
      TextArea.TextView.Redraw();

      if (_isSynchronizingText)
      {
        return;
      }

      _isSynchronizingText = true;
      SetCurrentValue(BoundTextProperty, Text);
      _isSynchronizingText = false;
    }

    private sealed class MarkerColorizer(Brush brush)
      : DocumentColorizingTransformer
    {
      public Brush Brush
      {
        get;
        set;
      } = brush;

      protected override void ColorizeLine(DocumentLine line)
      {
        var lineText = CurrentContext.Document.GetText(line);
        var searchStart = 0;

        while (searchStart < lineText.Length)
        {
          var index = lineText.IndexOf(
            SecretMessageTemplate.Marker,
            searchStart,
            StringComparison.OrdinalIgnoreCase);

          if (index < 0)
          {
            break;
          }

          ChangeLinePart(
            line.Offset + index,
            line.Offset + index + SecretMessageTemplate.Marker.Length,
            element =>
            {
              element.TextRunProperties.SetForegroundBrush(Brush);
              element.TextRunProperties.SetTypeface(new Typeface(
                element.TextRunProperties.Typeface.FontFamily,
                element.TextRunProperties.Typeface.Style,
                FontWeights.Bold,
                element.TextRunProperties.Typeface.Stretch));
            });

          searchStart = index + SecretMessageTemplate.Marker.Length;
        }
      }
    }
  }
}
