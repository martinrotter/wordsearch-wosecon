using System.Text;
using System.Xml;
using WordSearchGenerator.Common.WoSeCon.Api;

namespace WordSearchGenerator.Common
{
  public class Board
  {
    #region Properties

    public int ColumnCount
    {
      get;
    }

    public bool HtmlOutput
    {
      get;
    }

    public bool QuizMode
    {
      get;
    }

    public Cell[,] Matrix
    {
      get;
      private set;
    }

    public int CharCellCount
    {
      get
      {
        int taken = 0;

        foreach (Cell cell in Matrix)
        {
          if (cell.Type == Cell.CellType.CharFromText)
          {
            taken++;
          }
        }

        return taken;
      }
    }

    public double PercentageOccupied
    {
      get => 100 * (double)CharCellCount / (RowCount * ColumnCount);
    }

    public int IntersectionCount
    {
      get => Matrix.OfType<Cell>().Count(cl => cl.Intersections >= 2);
    }

    public string Message
    {
      get;
    }

    public int RowCount
    {
      get;
    }

    public List<WordInfo> Words
    {
      get;
    }

    private Random Rng
    {
      get;
    } = new Random((int)(DateTime.Now - DateTime.Today).TotalMilliseconds);

    public double BlindRate
    {
      get;
    }

    #endregion

    #region Constructors

    public Board(
      List<WordInfo> words,
      int rowCount,
      int columnCount,
      bool htmlOutput,
      bool quizMode,
      double blindRate = default,
      string message = null)
    {
      Words = words;
      RowCount = rowCount;
      ColumnCount = columnCount;
      HtmlOutput = htmlOutput;
      QuizMode = quizMode;
      Message = message;
      BlindRate = blindRate;

      FillBoard();
    }

    #endregion

    #region Other Stuff

    public string Print(bool htmlOutput, bool showSolution)
    {
      if (htmlOutput)
      {
        string html = GetHtmlTemplate(ColumnCount);
        string board = PrintBoardHtml();
        string words = PrintWordsHtml(showSolution);

        html = html
          .Replace("{1}", board)
          .Replace("{2}", words);

#if DEBUG
        File.WriteAllText("html.html", html, Encoding.UTF8);
#endif

        return html;
      }
      else
      {
        return PrintBoardSimple() + PrintWords(showSolution);
      }
    }

    private string PrintWordsHtml(bool showSolution)
    { 
      XmlDocument xml = new XmlDocument();
      XmlElement root = xml.CreateElement("div").AppendClass("wordsearch-words");
      xml.AppendChild(root);

      foreach (WordInfo word in Words.OrderBy(wrd => wrd.PrintableText.ToLower()))
      {
        root.AppendElem("div").AppendClass("wordsearch-word").InnerText = word.ToString(0, true, showSolution);
      }

      return xml.OuterXml;
    }

    private string PrintBoardHtml()
    {
      XmlDocument xml = new XmlDocument();
      XmlElement root = xml.CreateElement("div").AppendClass("wordsearch");
      xml.AppendChild(root);

      /*
      bldr.Append("   ❘ ");

      for (int j = 0; j < ColumnCount; j++)
      {
        bldr.Append(j.ToString("00"));
      }

      bldr.AppendLine();
      bldr.Append(new string('-', bldr.Length - 1));
      bldr.AppendLine();
      */

      for (int i = 0; i < RowCount; i++)
      {
        for (int j = 0; j < ColumnCount; j++)
        {
          Cell cell = Matrix[i, j];
          XmlElement xmlCell = root.AppendElem("div").AppendClass("wordsearch-cell");

          switch (cell.Type)
          {
            case Cell.CellType.Empty:
              xmlCell.InnerText = "·";
              xmlCell.AppendClass("wordsearch-cell-empty");
              break;

            case Cell.CellType.QuizWordPlaceholder:
              xmlCell.InnerText = $"{cell.QuizWordNumber} {DirectedLocation.GetArrowForDirection(cell.QuizWordDirection)}";
              xmlCell.AppendClass("wordsearch-cell-quiz");
              break;

            default:
              xmlCell.InnerText = cell.Char.ToString();
              break;
          }
        }
      }

      return xml.OuterXml;
    }

    private string GetHtmlTemplate(int columnCount)
    {
      return Properties.Resources.wordsearch_html.Replace("{0}", columnCount.ToString());
    }

    public string PrintBoardSimple()
    {
      StringBuilder bldr = new StringBuilder();

      bldr.Append("   ❘ ");

      for (int j = 0; j < ColumnCount; j++)
      {
        bldr.Append(j.ToString("00"));
      }

      bldr.AppendLine();
      bldr.Append(new string('-', bldr.Length - 1));
      bldr.AppendLine();

      for (int i = 0; i < RowCount; i++)
      {
        bldr.Append($"{i:00} ❘ ");

        for (int j = 0; j < ColumnCount; j++)
        {
          Cell cell = Matrix[i, j];

          bldr.Append(cell.Type switch
          {
            Cell.CellType.Empty => " -",
            Cell.CellType.QuizWordPlaceholder => $" {DirectedLocation.GetArrowForDirection(cell.QuizWordDirection)}",
            _ => $" {cell.Char}"
          });
        }

        bldr.AppendLine();
      }

      bldr.AppendLine();
      return bldr.ToString();
    }

    public string PrintIntersections()
    {
      StringBuilder bldr = new StringBuilder();

      for (int i = 0; i < RowCount; i++)
      {
        for (int j = 0; j < ColumnCount; j++)
        {
          if (Matrix[i, j].Intersections >= 2)
          {
            bldr.AppendLine($"Crossing: {i}x{j} -> {Matrix[i, j].Intersections}");
          }
        }
      }

      bldr.AppendLine();

      return bldr.ToString();
    }

    public string PrintWords(bool showSolution)
    {
      StringBuilder bldr = new StringBuilder();
      int longestWord = Words.Max(wrd => wrd.PrintableText.Length);

      foreach (WordInfo word in Words.OrderBy(wrd => wrd.PrintableText.ToLower()))
      {
        bldr.Append(word.ToString(longestWord, false, showSolution));
      }

      bldr.AppendLine();

      return bldr.ToString();
    }

    private void FillBoard()
    {
      List<char> messageChars = Message == null ? [] : Message.ToCharArray().ToList();
      Matrix = new Cell[RowCount, ColumnCount];

      for (int i = 0; i < RowCount; i++)
      for (int j = 0; j < ColumnCount; j++)
      {
        Matrix[i, j] = new Cell();
      }

      foreach (WordInfo word in Words)
      {
        List<DirectedLocation> locations = word.GetAllLetterLocations();

        if (!locations.Any())
        {
          throw new Exception("no locations");
        }

        string wordText = word.Text;

        for (int j = 0; j < wordText.Length; j++)
        {
          DirectedLocation letterLocation = locations[j];
          int r = letterLocation.Row;
          int c = letterLocation.Column;

          Cell cell = Matrix[r, c];

          cell.Type = Cell.CellType.CharFromText;
          cell.Words.Add(word);

          if (j == 0 && QuizMode)
          {
            cell.Type = Cell.CellType.QuizWordPlaceholder;
            cell.QuizWordNumber = word.WordNumber;
            cell.QuizWordDirection = word.Placement.Direction;
          }
          else if (cell.Char == default)
          {
            cell.Char = ShouldBeBlind() ? ' ' : wordText[j];
          }
        }
      }

      for (int i = 0; i < RowCount; i++)
      for (int j = 0; j < ColumnCount; j++)
      {
        if (messageChars.Count == 0)
        {
          break;
        }

        if (Matrix[i, j].Type != Cell.CellType.Empty)
        {
          continue;
        }

        Matrix[i, j].Type = Cell.CellType.CharFromMessage;
        Matrix[i, j].Char = messageChars.TakeFirst();
      }

      if (messageChars.Count > 0)
      {
        throw new Exception($"message is too long, {messageChars.Count} characters remain to be placed");
      }
    }

    private bool ShouldBeBlind()
    {
      return BlindRate > 0.0 && Rng.NextDouble() <= BlindRate;
    }

    #endregion

    #region Nested Types

    public class Cell
    {
      #region Enums

      public enum CellType
      {
        // Empty cell -> no character from any word is on it.
        Empty,

        // Char from found word(s).
        CharFromText,

        // Char from message.
        CharFromMessage,

        // Special placeholder placed in front of the word.
        // The cell will hold number and direction of the guessed word.
        QuizWordPlaceholder
      }

      #endregion

      #region Properties

      public char Char
      {
        get;
        set;
      }

      public int Intersections
      {
        get => Words.Count;
      }

      public List<WordInfo> Words
      {
        get;
      } = new List<WordInfo>();

      public DirectedLocation.LocationDirection QuizWordDirection
      {
        get;
        set;
      }

      public CellType Type
      {
        get;
        set;
      } = CellType.Empty;

      public int QuizWordNumber
      {
        get;
        set;
      }

      #endregion
    }

    #endregion
  }
}