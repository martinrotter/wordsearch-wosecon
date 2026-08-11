using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Wose.Desktop.Views.Controls
{
  public partial class BoardDimensionsEditor : UserControl
  {
    #region Static Fields

    private static readonly DependencyPropertyKey CellCountToolTipPropertyKey =
      DependencyProperty.RegisterReadOnly(
        nameof(CellCountToolTip),
        typeof(string),
        typeof(BoardDimensionsEditor),
        new PropertyMetadata(null));

    public static readonly DependencyProperty CellCountLabelProperty =
      DependencyProperty.Register(
        nameof(CellCountLabel),
        typeof(string),
        typeof(BoardDimensionsEditor),
        new PropertyMetadata(string.Empty, OnValueAffectingCellCountChanged));

    public static readonly DependencyProperty CellCountToolTipProperty =
      CellCountToolTipPropertyKey.DependencyProperty;

    public static readonly DependencyProperty ColumnsTextProperty =
      DependencyProperty.Register(
        nameof(ColumnsText),
        typeof(string),
        typeof(BoardDimensionsEditor),
        new FrameworkPropertyMetadata(
          string.Empty,
          FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
          OnValueAffectingCellCountChanged));

    public static readonly DependencyProperty RowsTextProperty =
      DependencyProperty.Register(
        nameof(RowsText),
        typeof(string),
        typeof(BoardDimensionsEditor),
        new FrameworkPropertyMetadata(
          string.Empty,
          FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
          OnValueAffectingCellCountChanged));

    #endregion

    #region Properties

    public string CellCountLabel
    {
      get => (string)GetValue(CellCountLabelProperty);
      set => SetValue(CellCountLabelProperty, value);
    }

    public string? CellCountToolTip =>
      (string?)GetValue(CellCountToolTipProperty);

    public string ColumnsText
    {
      get => (string)GetValue(ColumnsTextProperty);
      set => SetValue(ColumnsTextProperty, value);
    }

    public string RowsText
    {
      get => (string)GetValue(RowsTextProperty);
      set => SetValue(RowsTextProperty, value);
    }

    #endregion

    #region Constructors

    public BoardDimensionsEditor()
    {
      InitializeComponent();
    }

    #endregion

    #region Other Stuff

    private static bool ContainsOnlyDigits(string text)
    {
      return text.All(character => character is >= '0' and <= '9');
    }

    private static void OnValueAffectingCellCountChanged(
      DependencyObject dependencyObject,
      DependencyPropertyChangedEventArgs e)
    {
      ((BoardDimensionsEditor)dependencyObject).UpdateCellCountToolTip();
    }

    private void NumericTextBoxOnPasting(
      object sender,
      DataObjectPastingEventArgs e)
    {
      if (!e.DataObject.GetDataPresent(DataFormats.UnicodeText) ||
          e.DataObject.GetData(DataFormats.UnicodeText) is not string text ||
          !ContainsOnlyDigits(text))
      {
        e.CancelCommand();
      }
    }

    private void NumericTextBoxOnPreviewTextInput(
      object sender,
      TextCompositionEventArgs e)
    {
      e.Handled = !ContainsOnlyDigits(e.Text);
    }

    private void UpdateCellCountToolTip()
    {
      if (!int.TryParse(RowsText, out var rows) ||
          rows <= 0 ||
          !int.TryParse(ColumnsText, out var columns) ||
          columns <= 0)
      {
        SetValue(CellCountToolTipPropertyKey, null);
        return;
      }

      var totalCellCount = (long)rows * columns;
      var label = string.IsNullOrWhiteSpace(CellCountLabel)
        ? string.Empty
        : $"{CellCountLabel}: ";

      SetValue(
        CellCountToolTipPropertyKey,
        $"{label}{totalCellCount:N0}");
    }

    #endregion
  }
}
