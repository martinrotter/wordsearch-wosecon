using System.Collections.ObjectModel;
using WordSearchGenerator.Common.WoSeCon.Api;

namespace WordSearchGenerator.Desktop.Models
{
  public sealed class PuzzleDefinition
  {
    #region Properties

    public int Columns
    {
      get;
    }

    public IReadOnlyList<PuzzleEntry> Entries
    {
      get;
    }

    public GenerationOptions Generation
    {
      get;
    }

    public PuzzleMode Mode
    {
      get;
    }

    public int Rows
    {
      get;
    }

    public string SecretMessage
    {
      get;
    }

    public bool QuizMode => Mode == PuzzleMode.Quiz;

    #endregion

    #region Constructors

    public PuzzleDefinition(
      PuzzleMode mode,
      int rows,
      int columns,
      IEnumerable<PuzzleEntry> entries,
      string secretMessage,
      GenerationOptions generation)
    {
      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
      ArgumentNullException.ThrowIfNull(entries);
      ArgumentNullException.ThrowIfNull(secretMessage);
      ArgumentNullException.ThrowIfNull(generation);

      var entryArray = entries.ToArray();

      if (entryArray.Length == 0)
      {
        throw new ArgumentException(
          "At least one puzzle entry is required.",
          nameof(entries));
      }

      if (entryArray.Any(entry => entry == null))
      {
        throw new ArgumentException(
          "Puzzle entries cannot contain null values.",
          nameof(entries));
      }

      Mode = mode;
      Rows = rows;
      Columns = columns;
      Entries = new ReadOnlyCollection<PuzzleEntry>(entryArray);
      SecretMessage = secretMessage;
      Generation = generation;
    }

    #endregion

    #region Other Stuff

    public List<WordInfo> CreateWordInfos()
    {
      return Entries
        .Select((entry, index) => new WordInfo
        {
          Text = entry.Answer,
          QuizQuestion = entry.Question,
          WordNumber = index + 1
        })
        .ToList();
    }

    #endregion
  }
}
