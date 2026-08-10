using System.Text;
using Wose.Common.WoSeCon.Api;

namespace Wose.Common
{
  public class Board : PuzzleGrid
  {
    #region Properties

    public Cell[,] Matrix
    {
      get;
      private set;
    }

    public int CharCellCount => Matrix.OfType<Cell>().Count(cell => cell.Words.Count > 0);

    public double PercentageOccupied => 100 * (double)CharCellCount / (Rows * Columns);

    public int IntersectionCount => Matrix.OfType<Cell>().Count(cl => cl.Intersections >= 2);

    public string Message
    {
      get;
    }

    public List<WordInfo> Words
    {
      get;
    }

    #endregion

    #region Constructors

    public Board(
      List<WordInfo> words,
      int rows,
      int columns,
      PuzzleMode mode,
      string message = null) : base(mode, rows, columns)
    {
      Words = words;
      Message = message ?? string.Empty;

      FillBoard();
    }

    public Board(
      List<WordInfo> words,
      PuzzleGrid grid,
      string message = null) : this(
      words,
      (grid ?? throw new ArgumentNullException(nameof(grid))).Rows,
      grid.Columns,
      grid.Mode,
      message)
    {
    }

    #endregion

    #region Other Stuff

    public string PrintBoard()
    {
      var bldr = new StringBuilder();

      bldr.Append("    ");

      for (var column = 0; column < Columns; column++)
      {
        bldr.Append($"{column,3}");
      }

      bldr.AppendLine();
      bldr.Append("   +");
      bldr.AppendLine(new string('-', Columns * 3));

      for (var row = 0; row < Rows; row++)
      {
        bldr.Append($"{row,2} | ");

        for (var column = 0; column < Columns; column++)
        {
          var cell = Matrix[row, column];

          bldr.Append(cell.Type switch
          {
            Cell.CellType.Empty => " . ",
            Cell.CellType.QuizQuestion =>
              $" {DirectedLocation.GetArrowForDirection(cell.QuizWordDirection)} ",
            _ => $" {cell.Char} "
          });
        }

        bldr.AppendLine();
      }

      return bldr.ToString();
    }

    public string PrintDiagnostics()
    {
      var bldr = new StringBuilder();

      bldr.AppendLine($"Board: {Rows}x{Columns}");
      bldr.Append(PrintBoard());
      bldr.AppendLine($"Words ({Words.Count}):");
      bldr.Append(PrintWords(true));
      bldr.AppendLine($"Intersections: {IntersectionCount}");
      bldr.Append(PrintIntersections());
      bldr.AppendLine(
        $"Occupied: {CharCellCount}/{Rows * Columns} ({PercentageOccupied:F2}%)");

      return bldr.ToString();
    }

    public string PrintIntersections()
    {
      var bldr = new StringBuilder();

      for (var i = 0; i < Rows; i++)
      {
        for (var j = 0; j < Columns; j++)
        {
          if (Matrix[i, j].Intersections >= 2)
          {
            var cell = Matrix[i, j];
            var words = string.Join(", ", cell.Words.Select(word => word.Text));

            bldr.AppendLine(
              $"  ({i},{j}) '{cell.Char}': {cell.Intersections} words [{words}]");
          }
        }
      }

      if (bldr.Length == 0)
      {
        bldr.AppendLine("  none");
      }

      return bldr.ToString();
    }

    public string PrintWords(bool showSolution)
    {
      var bldr = new StringBuilder();
      var fallbackNumber = 1;

      foreach (var word in Words)
      {
        var wordNumber = word.WordNumber > 0 ? word.WordNumber : fallbackNumber;
        var printableText = word.Text;

        bldr.Append($"  {wordNumber,2}. {printableText}");

        if (showSolution)
        {
          if (word.Placement == null)
          {
            bldr.Append(" [unplaced]");
          }
          else
          {
            bldr.Append(
              $" @ ({word.Placement.Row},{word.Placement.Column}) " +
              $"{DirectedLocation.GetArrowForDirection(word.Placement.Direction)} " +
              word.Placement.Direction);
          }
        }

        bldr.AppendLine();
        fallbackNumber++;
      }

      return bldr.ToString();
    }

    private void FillBoard()
    {
      Matrix = new Cell[Rows, Columns];

      for (var i = 0; i < Rows; i++)
        for (var j = 0; j < Columns; j++)
        {
          Matrix[i, j] = new Cell();
        }

      foreach (var word in Words)
      {
        if (word.Placement == null)
        {
          continue;
        }

        var locations = word.GetAllPlacementLocations(QuizMode);
        var wordText = word.Text;

        for (var j = 0; j < locations.Count; j++)
        {
          var letterLocation = locations[j];
          var r = letterLocation.Row;
          var c = letterLocation.Column;

          var cell = Matrix[r, c];

          cell.Type = Cell.CellType.CharFromText;
          cell.Words.Add(word);

          if (j == 0 && QuizMode)
          {
            cell.Type = Cell.CellType.QuizQuestion;
            cell.QuizWordNumber = word.WordNumber;
            cell.QuizWordDirection = word.Placement.Direction;
          }
          else if (cell.Char == default)
          {
            var textIndex = j - (QuizMode ? 1 : 0);
            cell.Char = wordText[textIndex];
          }
        }
      }

      if (QuizMode)
      {
        AssignQuizMessageCells();
        return;
      }

      FillNormalMessageCells();
    }

    private void AssignQuizMessageCells()
    {
      var availableCells = new Dictionary<char, List<(Cell Cell, int Position)>>();

      for (var row = 0; row < Rows; row++)
        for (var column = 0; column < Columns; column++)
        {
          var cell = Matrix[row, column];

          if (cell.Type != Cell.CellType.CharFromText)
          {
            continue;
          }

          if (!availableCells.TryGetValue(cell.Char, out var cellsForCharacter))
          {
            cellsForCharacter = [];
            availableCells.Add(cell.Char, cellsForCharacter);
          }

          cellsForCharacter.Add((cell, row * Columns + column));
        }

      for (var messageIndex = 0; messageIndex < Message.Length; messageIndex++)
      {
        var messageCharacter = Message[messageIndex];

        if (!availableCells.TryGetValue(messageCharacter, out var matchingCells) ||
            matchingCells.Count == 0)
        {
          throw new MessageCannotBePlacedException(
            $"message character at index {messageIndex} cannot be assigned to a distinct answer cell");
        }

        var targetPosition = Message.Length == 1
          ? (Rows * Columns - 1) / 2.0
          : (double)messageIndex *
            (Rows * Columns - 1) /
            (Message.Length - 1);
        var nearestCellIndex = 0;
        var nearestDistance = Math.Abs(
          matchingCells[0].Position - targetPosition);

        for (var candidateIndex = 1;
             candidateIndex < matchingCells.Count;
             candidateIndex++)
        {
          var distance = Math.Abs(
            matchingCells[candidateIndex].Position - targetPosition);

          if (distance < nearestDistance)
          {
            nearestCellIndex = candidateIndex;
            nearestDistance = distance;
          }
        }

        matchingCells[nearestCellIndex].Cell.MessageIndex = messageIndex + 1;
        matchingCells.RemoveAt(nearestCellIndex);
      }
    }

    private void FillNormalMessageCells()
    {
      var availableCells = new List<Cell>();

      for (var row = 0; row < Rows; row++)
        for (var column = 0; column < Columns; column++)
        {
          if (Matrix[row, column].Type == Cell.CellType.Empty)
          {
            availableCells.Add(Matrix[row, column]);
          }
        }

      if (Message.Length > availableCells.Count)
      {
        throw new MessageCannotBePlacedException(
          $"message is too long, {Message.Length - availableCells.Count} characters remain to be placed");
      }

      for (var messageIndex = 0; messageIndex < Message.Length; messageIndex++)
      {
        var availableCellIndex = Message.Length == 1
          ? availableCells.Count / 2
          : (int)Math.Round(
            (double)messageIndex *
            (availableCells.Count - 1) /
            (Message.Length - 1),
            MidpointRounding.AwayFromZero);
        var cell = availableCells[availableCellIndex];

        cell.Type = Cell.CellType.CharFromMessage;
        cell.Char = Message[messageIndex];
      }
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

        // Question cell placed in front of the word.
        // The cell will hold number and direction of the guessed word.
        QuizQuestion
      }

      #endregion

      #region Properties

      public char Char
      {
        get;
        set;
      }

      public int Intersections => Words.Count;

      public List<WordInfo> Words
      {
        get;
      } = [];

      /// <summary>
      ///   One-based position of this answer cell in a quiz-mode secret
      ///   message, or when it is not an extraction
      ///   cell.
      /// </summary>
      public int? MessageIndex
      {
        get;
        set;
      }

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
