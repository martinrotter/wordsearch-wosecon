using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using WordSearchGenerator.Desktop.Localization;
using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Models.Rendering;
using WordSearchGenerator.Desktop.Services.Rendering;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace WordSearchGenerator.Desktop.Services.Exporting
{
  public sealed class DocxPuzzleExporter : IDocxPuzzleExporter
  {
    #region Static Fields

    private const uint PageWidthTwips = 11906;
    private const uint PageHeightTwips = 16838;
    private const uint PageMarginTwips = 1021;
    private const long EmusPerTwip = 635;
    private const int MaximumMessageColumns = 18;
    private const int QuizNumberingId = 1;
    private const string BodyStyleId = "PuzzleBody";
    private const string HeadingStyleId = "PuzzleHeading";
    private const string TitleStyleId = "PuzzleTitle";

    #endregion

    #region Interface Implementations

    public async Task ExportAsync(
      string path,
      BoardRenderModel model,
      BoardPreviewMode previewMode,
      byte[] boardPng,
      CancellationToken cancellationToken = default)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(path);
      ArgumentNullException.ThrowIfNull(model);
      ArgumentNullException.ThrowIfNull(boardPng);
      cancellationToken.ThrowIfCancellationRequested();

      if (boardPng.Length == 0)
      {
        throw new ArgumentException(
          "The board PNG must not be empty.",
          nameof(boardPng));
      }

      var fullPath = Path.GetFullPath(path);
      var directory = Path.GetDirectoryName(fullPath) ??
                      throw new InvalidOperationException(
                        AppStrings.Get("ProjectPathNoParent"));

      if (!Directory.Exists(directory))
      {
        throw new DirectoryNotFoundException(AppStrings.Format(
          "DirectoryDoesNotExist",
          directory));
      }

      var temporaryPath = Path.Combine(
        directory,
        $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

      try
      {
        await Task.Run(
          () =>
          {
            cancellationToken.ThrowIfCancellationRequested();
            WriteDocument(
              temporaryPath,
              model,
              previewMode,
              boardPng,
              cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, fullPath, true);
          },
          cancellationToken);
      }
      finally
      {
        if (File.Exists(temporaryPath))
        {
          File.Delete(temporaryPath);
        }
      }
    }

    #endregion

    #region Other Stuff

    private static void AppendEntries(
      W.Body body,
      BoardRenderModel model,
      BoardPreviewMode previewMode)
    {
      if (!string.IsNullOrWhiteSpace(model.EntryListHeading))
      {
        body.Append(CreateHeading(model.EntryListHeading));
      }

      var entries = PuzzleDocumentPresentation
        .EnumerateEntries(model)
        .ToArray();

      if (model.Mode == PuzzleMode.Normal)
      {
        body.Append(CreateWordTable(entries));
        return;
      }

      var includeAnswers =
        PuzzleDocumentPresentation.ShouldIncludeQuizAnswers(
          model,
          previewMode);

      foreach (var entry in entries)
      {
        var paragraph = CreateNumberedParagraph(
          entry.Question ?? string.Empty);

        if (includeAnswers)
        {
          paragraph.Append(
            new W.Run(
              new W.Break(),
              new W.RunProperties(new W.Bold()),
              CreateText(AppStrings.Get("HtmlAnswer"))),
            new W.Run(CreateText(entry.Answer)));
        }

        body.Append(paragraph);
      }
    }

    private static void AppendSecretMessage(
      W.Body body,
      BoardRenderModel model,
      BoardPreviewMode previewMode)
    {
      body.Append(CreateHeading(AppStrings.Get("SecretMessage")));
      body.Append(CreateBodyParagraph(
        PuzzleDocumentPresentation.GetSecretMessageInstructions(
          model,
          previewMode)));
      body.Append(CreateMessageTable(
        model.SecretMessage,
        PuzzleDocumentPresentation.IsSolution(previewMode)));
    }

    private static W.Paragraph CreateBodyParagraph(
      string text,
      int spacingAfter = 160)
    {
      return new W.Paragraph(
        new W.ParagraphProperties(
          new W.ParagraphStyleId
          {
            Val = BodyStyleId
          },
          new W.SpacingBetweenLines
          {
            After = spacingAfter.ToString(CultureInfo.InvariantCulture)
          }),
        new W.Run(CreateText(text)));
    }

    private static W.Paragraph CreateHeading(string text)
    {
      return new W.Paragraph(
        new W.ParagraphProperties(
          new W.ParagraphStyleId
          {
            Val = HeadingStyleId
          }),
        new W.Run(CreateText(text)));
    }

    private static W.Paragraph CreateNumberedParagraph(string text)
    {
      return new W.Paragraph(
        new W.ParagraphProperties(
          new W.ParagraphStyleId
          {
            Val = BodyStyleId
          },
          new W.NumberingProperties(
            new W.NumberingLevelReference
            {
              Val = 0
            },
            new W.NumberingId
            {
              Val = QuizNumberingId
            }),
          new W.SpacingBetweenLines
          {
            After = "80"
          }),
        new W.Run(CreateText(text)));
    }

    private static W.Paragraph CreateImageParagraph(
      string relationshipId,
      long width,
      long height)
    {
      var drawing = new W.Drawing(
        new DW.Inline(
          new DW.Extent
          {
            Cx = width,
            Cy = height
          },
          new DW.EffectExtent
          {
            LeftEdge = 0,
            TopEdge = 0,
            RightEdge = 0,
            BottomEdge = 0
          },
          new DW.DocProperties
          {
            Id = 1U,
            Name = "Puzzle board"
          },
          new DW.NonVisualGraphicFrameDrawingProperties(
            new A.GraphicFrameLocks
            {
              NoChangeAspect = true
            }),
          new A.Graphic(
            new A.GraphicData(
              new PIC.Picture(
                new PIC.NonVisualPictureProperties(
                  new PIC.NonVisualDrawingProperties
                  {
                    Id = 0U,
                    Name = "board.png"
                  },
                  new PIC.NonVisualPictureDrawingProperties()),
                new PIC.BlipFill(
                  new A.Blip
                  {
                    Embed = relationshipId,
                    CompressionState = A.BlipCompressionValues.Print
                  },
                  new A.Stretch(new A.FillRectangle())),
                new PIC.ShapeProperties(
                  new A.Transform2D(
                    new A.Offset
                    {
                      X = 0,
                      Y = 0
                    },
                    new A.Extents
                    {
                      Cx = width,
                      Cy = height
                    }),
                  new A.PresetGeometry(new A.AdjustValueList())
                  {
                    Preset = A.ShapeTypeValues.Rectangle
                  })))
            {
              Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
            }))
        {
          DistanceFromTop = 0U,
          DistanceFromBottom = 0U,
          DistanceFromLeft = 0U,
          DistanceFromRight = 0U
        });

      return new W.Paragraph(
        new W.ParagraphProperties(
          new W.ParagraphStyleId
          {
            Val = BodyStyleId
          },
          new W.SpacingBetweenLines
          {
            After = "180"
          },
          new W.Justification
          {
            Val = W.JustificationValues.Center
          }),
        new W.Run(drawing));
    }

    private static W.Table CreateMessageTable(
      string message,
      bool showSolution)
    {
      var columnCount = Math.Min(MaximumMessageColumns, message.Length);
      var availableWidth = PageWidthTwips - PageMarginTwips * 2;
      var cellWidth = availableWidth / MaximumMessageColumns;
      var table = new W.Table(
        new W.TableProperties(
          new W.TableWidth
          {
            Type = W.TableWidthUnitValues.Dxa,
            Width = (cellWidth * (uint)columnCount)
              .ToString(CultureInfo.InvariantCulture)
          },
          new W.TableJustification
          {
            Val = W.TableRowAlignmentValues.Center
          },
          new W.TableLayout
          {
            Type = W.TableLayoutValues.Fixed
          }));
      var grid = new W.TableGrid();

      for (var index = 0; index < columnCount; index++)
      {
        grid.Append(new W.GridColumn
        {
          Width = cellWidth.ToString(CultureInfo.InvariantCulture)
        });
      }

      table.Append(grid);

      for (var offset = 0; offset < message.Length; offset += columnCount)
      {
        var row = new W.TableRow();
        var length = Math.Min(columnCount, message.Length - offset);

        for (var index = 0; index < length; index++)
        {
          var character = showSolution
            ? message[offset + index].ToString()
            : "\u00A0";
          var borders = new W.TableCellBorders(
            new W.TopBorder
            {
              Val = W.BorderValues.Single,
              Size = 6U,
              Color = "7F8C8D"
            },
            new W.LeftBorder
            {
              Val = W.BorderValues.Single,
              Size = 6U,
              Color = "7F8C8D"
            },
            new W.BottomBorder
            {
              Val = W.BorderValues.Single,
              Size = 6U,
              Color = "7F8C8D"
            },
            new W.RightBorder
            {
              Val = W.BorderValues.Single,
              Size = 6U,
              Color = "7F8C8D"
            });
          var cell = new W.TableCell(
            new W.TableCellProperties(
              new W.TableCellWidth
              {
                Type = W.TableWidthUnitValues.Dxa,
                Width = cellWidth.ToString(CultureInfo.InvariantCulture)
              },
              borders,
              new W.TableCellVerticalAlignment
              {
                Val = W.TableVerticalAlignmentValues.Center
              }),
            new W.Paragraph(
              new W.ParagraphProperties(
                new W.ParagraphStyleId
                {
                  Val = BodyStyleId
                },
                new W.SpacingBetweenLines
                {
                  Before = "60",
                  After = "60"
                },
                new W.Justification
                {
                  Val = W.JustificationValues.Center
                }),
              new W.Run(
                new W.RunProperties(
                  new W.FontSize
                  {
                    Val = "24"
                  },
                  new W.FontSizeComplexScript
                  {
                    Val = "24"
                  }),
                CreateText(character))));

          row.Append(cell);
        }

        table.Append(row);
      }

      return table;
    }

    private static W.Paragraph CreateTitle(string text)
    {
      return new W.Paragraph(
        new W.ParagraphProperties(
          new W.ParagraphStyleId
          {
            Val = TitleStyleId
          }),
        new W.Run(CreateText(text)));
    }

    private static W.Table CreateWordTable(
      IReadOnlyCollection<BoardRenderEntry> entries)
    {
      var columnCount = entries.Count >= 12
        ? 3
        : entries.Count >= 6
          ? 2
          : 1;
      var availableWidth = PageWidthTwips - PageMarginTwips * 2;
      var cellWidth = availableWidth / (uint)columnCount;
      var table = new W.Table(
        new W.TableProperties(
          new W.TableWidth
          {
            Type = W.TableWidthUnitValues.Dxa,
            Width = availableWidth.ToString(CultureInfo.InvariantCulture)
          },
          new W.TableLayout
          {
            Type = W.TableLayoutValues.Fixed
          }));
      var entryArray = entries.ToArray();
      var grid = new W.TableGrid();

      for (var index = 0; index < columnCount; index++)
      {
        grid.Append(new W.GridColumn
        {
          Width = cellWidth.ToString(CultureInfo.InvariantCulture)
        });
      }

      table.Append(grid);

      for (var offset = 0; offset < entryArray.Length; offset += columnCount)
      {
        var row = new W.TableRow();

        for (var column = 0; column < columnCount; column++)
        {
          var entryIndex = offset + column;
          var text = entryIndex < entryArray.Length
            ? entryArray[entryIndex].Answer
            : string.Empty;

          row.Append(new W.TableCell(
            new W.TableCellProperties(
              new W.TableCellWidth
              {
                Type = W.TableWidthUnitValues.Dxa,
                Width = cellWidth.ToString(CultureInfo.InvariantCulture)
              }),
            new W.Paragraph(
              new W.ParagraphProperties(
                new W.ParagraphStyleId
                {
                  Val = BodyStyleId
                },
                new W.SpacingBetweenLines
                {
                  After = "40"
                }),
              new W.Run(CreateText(text)))));
        }

        table.Append(row);
      }

      return table;
    }

    private static W.SectionProperties CreateSectionProperties()
    {
      return new W.SectionProperties(
        new W.PageSize
        {
          Width = PageWidthTwips,
          Height = PageHeightTwips,
          Orient = W.PageOrientationValues.Portrait
        },
        new W.PageMargin
        {
          Top = (int)PageMarginTwips,
          Right = PageMarginTwips,
          Bottom = (int)PageMarginTwips,
          Left = PageMarginTwips,
          Header = 454U,
          Footer = 454U,
          Gutter = 0U
        });
    }

    private static W.Numbering CreateNumbering()
    {
      var level = new W.Level(
        new W.StartNumberingValue
        {
          Val = 1
        },
        new W.NumberingFormat
        {
          Val = W.NumberFormatValues.Decimal
        },
        new W.LevelText
        {
          Val = "%1."
        },
        new W.LevelJustification
        {
          Val = W.LevelJustificationValues.Left
        },
        new W.PreviousParagraphProperties(
          new W.Indentation
          {
            Left = "360",
            Hanging = "360"
          }))
      {
        LevelIndex = 0
      };
      var abstractNumber = new W.AbstractNum(
        new W.MultiLevelType
        {
          Val = W.MultiLevelValues.SingleLevel
        },
        level)
      {
        AbstractNumberId = QuizNumberingId
      };
      var numberingInstance = new W.NumberingInstance(
        new W.AbstractNumId
        {
          Val = QuizNumberingId
        })
      {
        NumberID = QuizNumberingId
      };

      return new W.Numbering(abstractNumber, numberingInstance);
    }

    private static W.Styles CreateStyles()
    {
      var defaultRunProperties = new W.RunPropertiesBaseStyle(
        new W.RunFonts
        {
          Ascii = "Aptos",
          HighAnsi = "Aptos",
          EastAsia = "Aptos",
          ComplexScript = "Aptos"
        },
        new W.FontSize
        {
          Val = "22"
        },
        new W.FontSizeComplexScript
        {
          Val = "22"
        });
      var defaults = new W.DocDefaults(
        new W.RunPropertiesDefault(defaultRunProperties),
        new W.ParagraphPropertiesDefault(
          new W.ParagraphPropertiesBaseStyle(
            new W.SpacingBetweenLines
            {
              After = "120",
              Line = "300",
              LineRule = W.LineSpacingRuleValues.Auto
            })));
      var normal = new W.Style(
        new W.StyleName
        {
          Val = "Normal"
        })
      {
        Type = W.StyleValues.Paragraph,
        StyleId = "Normal",
        Default = true
      };
      var body = new W.Style(
        new W.StyleName
        {
          Val = "Puzzle body"
        },
        new W.BasedOn
        {
          Val = "Normal"
        },
        new W.NextParagraphStyle
        {
          Val = BodyStyleId
        })
      {
        Type = W.StyleValues.Paragraph,
        StyleId = BodyStyleId,
        CustomStyle = true
      };
      var heading = new W.Style(
        new W.StyleName
        {
          Val = "Puzzle heading"
        },
        new W.BasedOn
        {
          Val = BodyStyleId
        },
        new W.NextParagraphStyle
        {
          Val = BodyStyleId
        },
        new W.StyleParagraphProperties(
          new W.KeepNext(),
          new W.SpacingBetweenLines
          {
            Before = "220",
            After = "80"
          }),
        new W.StyleRunProperties(
          new W.Bold(),
          new W.FontSize
          {
            Val = "28"
          },
          new W.FontSizeComplexScript
          {
            Val = "28"
          }))
      {
        Type = W.StyleValues.Paragraph,
        StyleId = HeadingStyleId,
        CustomStyle = true
      };
      var title = new W.Style(
        new W.StyleName
        {
          Val = "Puzzle title"
        },
        new W.BasedOn
        {
          Val = BodyStyleId
        },
        new W.NextParagraphStyle
        {
          Val = BodyStyleId
        },
        new W.StyleParagraphProperties(
          new W.KeepNext(),
          new W.SpacingBetweenLines
          {
            After = "220"
          },
          new W.Justification
          {
            Val = W.JustificationValues.Center
          }),
        new W.StyleRunProperties(
          new W.Bold(),
          new W.FontSize
          {
            Val = "36"
          },
          new W.FontSizeComplexScript
          {
            Val = "36"
          }))
      {
        Type = W.StyleValues.Paragraph,
        StyleId = TitleStyleId,
        CustomStyle = true
      };

      return new W.Styles(defaults, normal, body, heading, title);
    }

    private static W.Text CreateText(string text)
    {
      return new W.Text(text)
      {
        Space = SpaceProcessingModeValues.Preserve
      };
    }

    private static (long Width, long Height) GetImageExtent(byte[] png)
    {
      if (png.Length < 24 ||
          png[0] != 0x89 ||
          png[1] != 0x50 ||
          png[2] != 0x4E ||
          png[3] != 0x47)
      {
        throw new InvalidDataException("The board renderer returned invalid PNG data.");
      }

      var pixelWidth = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4));
      var pixelHeight = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4));

      if (pixelWidth <= 0 || pixelHeight <= 0)
      {
        throw new InvalidDataException("The board renderer returned invalid PNG dimensions.");
      }

      var availableWidth =
        (PageWidthTwips - PageMarginTwips * 2) * EmusPerTwip;
      var availableHeight =
        (PageHeightTwips - PageMarginTwips * 2) * EmusPerTwip;
      var width = availableWidth;
      var height = checked(width * pixelHeight / pixelWidth);

      if (height > availableHeight)
      {
        height = availableHeight;
        width = checked(height * pixelWidth / pixelHeight);
      }

      return (width, height);
    }

    private static void WriteDocument(
      string path,
      BoardRenderModel model,
      BoardPreviewMode previewMode,
      byte[] boardPng,
      CancellationToken cancellationToken)
    {
      using var document = WordprocessingDocument.Create(
        path,
        WordprocessingDocumentType.Document);
      var mainPart = document.AddMainDocumentPart();
      mainPart.Document = new W.Document(new W.Body());
      var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
      stylesPart.Styles = CreateStyles();
      var numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
      numberingPart.Numbering = CreateNumbering();
      var settingsPart = mainPart.AddNewPart<DocumentSettingsPart>();
      settingsPart.Settings = new W.Settings(
        new W.DoNotAutoCompressPictures());
      var imagePart = mainPart.AddImagePart(ImagePartType.Png);

      using (var imageStream = new MemoryStream(boardPng, false))
      {
        imagePart.FeedData(imageStream);
      }

      cancellationToken.ThrowIfCancellationRequested();

      var body = mainPart.Document.Body ??
                 throw new InvalidOperationException(
                   "The Word document has no body.");

      if (!string.IsNullOrWhiteSpace(model.PuzzleHeading))
      {
        body.Append(CreateTitle(model.PuzzleHeading));
      }

      var (imageWidth, imageHeight) = GetImageExtent(boardPng);
      body.Append(CreateImageParagraph(
        mainPart.GetIdOfPart(imagePart),
        imageWidth,
        imageHeight));

      if (PuzzleDocumentPresentation.ShouldIncludeTutorial(previewMode))
      {
        body.Append(CreateHeading(AppStrings.Get("HtmlTutorialHeading")));
        body.Append(CreateBodyParagraph(
          PuzzleDocumentPresentation.GetTutorialText(model)));
      }

      if (PuzzleDocumentPresentation.ShouldIncludeSecretMessage(model))
      {
        AppendSecretMessage(body, model, previewMode);
      }

      AppendEntries(body, model, previewMode);
      body.Append(CreateSectionProperties());
      mainPart.Document.Save();
    }

    #endregion
  }
}
