namespace WordSearchGenerator.Common
{
  public static class BoardOccurrenceValidator
  {
    #region Static Fields

    private static readonly (int RowStep, int ColumnStep)[] SearchDirections =
    [
      (0, 1),
      (0, -1),
      (1, 0),
      (-1, 0),
      (1, 1),
      (-1, -1),
      (-1, 1),
      (1, -1)
    ];

    #endregion

    #region Other Stuff

    /// <summary>
    ///   Returns whether every configured word occurs on exactly its intended
    ///   physical path and nowhere else on a normal-mode board.
    /// </summary>
    public static bool HasUniqueWordOccurrences(this Board board)
    {
      ArgumentNullException.ThrowIfNull(board);

      if (board.QuizMode)
      {
        throw new InvalidOperationException(
          "Word occurrence uniqueness is only defined for normal-mode boards.");
      }

      foreach (var word in board.Words)
      {
        if (word.Placement == null)
        {
          return false;
        }

        var intendedLocations = word.GetAllPlacementLocations(false);

        if (intendedLocations.Count == 0)
        {
          return false;
        }

        var intendedPath = CreatePath(
          board.Columns,
          intendedLocations[0].Row,
          intendedLocations[0].Column,
          intendedLocations[^1].Row,
          intendedLocations[^1].Column);
        if (!HasOnlyOccurrenceOnPath(board, word.Text, intendedPath))
        {
          return false;
        }
      }

      return true;
    }

    private static (int FirstCell, int LastCell) CreatePath(
      int columnCount,
      int startRow,
      int startColumn,
      int endRow,
      int endColumn)
    {
      var start = startRow * columnCount + startColumn;
      var end = endRow * columnCount + endColumn;

      return start <= end ? (start, end) : (end, start);
    }

    private static bool HasOnlyOccurrenceOnPath(
      Board board,
      string word,
      (int FirstCell, int LastCell) intendedPath)
    {
      (int FirstCell, int LastCell)? occurrence = null;

      for (var row = 0; row < board.Rows; row++)
      for (var column = 0; column < board.Columns; column++)
      {
        foreach (var (rowStep, columnStep) in SearchDirections)
        {
          var endRow = row + rowStep * (word.Length - 1);
          var endColumn = column + columnStep * (word.Length - 1);

          if (endRow < 0 ||
              endRow >= board.Rows ||
              endColumn < 0 ||
              endColumn >= board.Columns)
          {
            continue;
          }

          var matches = true;

          for (var characterIndex = 0;
               characterIndex < word.Length;
               characterIndex++)
          {
            var cell = board.Matrix[
              row + rowStep * characterIndex,
              column + columnStep * characterIndex];

            if ((cell.Type != Board.Cell.CellType.CharFromText &&
                 cell.Type != Board.Cell.CellType.CharFromMessage) ||
                cell.Char != word[characterIndex])
            {
              matches = false;
              break;
            }
          }

          if (!matches)
          {
            continue;
          }

          var path = CreatePath(
            board.Columns,
            row,
            column,
            endRow,
            endColumn);

          if (occurrence != null && occurrence.Value != path)
          {
            return false;
          }

          occurrence = path;
        }
      }

      return occurrence == intendedPath;
    }

    #endregion
  }
}
