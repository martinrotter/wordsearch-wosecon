using System.Collections.ObjectModel;
using WordSearchGenerator.Common.WoSeCon.Api;
using WordSearchGenerator.Desktop.Localization;

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

    public string EntryListHeading
    {
      get;
    }

    public PuzzleMode Mode
    {
      get;
    }

    public string PuzzleHeading
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
      string puzzleHeading,
      string entryListHeading,
      GenerationOptions generation)
    {
      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
      ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
      ArgumentNullException.ThrowIfNull(entries);
      ArgumentNullException.ThrowIfNull(secretMessage);
      ArgumentNullException.ThrowIfNull(puzzleHeading);
      ArgumentNullException.ThrowIfNull(entryListHeading);
      ArgumentNullException.ThrowIfNull(generation);

      var entryArray = entries.ToArray();

      if (entryArray.Length == 0)
      {
        throw new ArgumentException(
          AppStrings.Get("PuzzleEntryRequired"),
          nameof(entries));
      }

      if (entryArray.Any(entry => entry == null))
      {
        throw new ArgumentException(
          AppStrings.Get("PuzzleEntriesNoNull"),
          nameof(entries));
      }

      Mode = mode;
      Rows = rows;
      Columns = columns;
      Entries = new ReadOnlyCollection<PuzzleEntry>(entryArray);
      SecretMessage = secretMessage;
      PuzzleHeading = puzzleHeading.Trim();
      EntryListHeading = entryListHeading.Trim();
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