using System.Collections.ObjectModel;
using Wose.Common;
using Wose.Common.WoSeCon.Api;
using Wose.Desktop.Localization;

namespace Wose.Desktop.Models
{
  public sealed class PuzzleDefinition : PuzzleGrid
  {
    public const int MaximumBlindPercentage = 30;

    #region Properties

    public int BlindPercentage
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

    public string PuzzleHeading
    {
      get;
    }

    public bool RequireExactMessageFit
    {
      get;
    }

    public string SecretMessage
    {
      get;
    }

    public string StyleId
    {
      get;
    }

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
      string styleId,
      GenerationOptions generation,
      bool requireExactMessageFit = false,
      int blindPercentage = 0) : base(mode, rows, columns)
    {
      ArgumentNullException.ThrowIfNull(entries);
      ArgumentNullException.ThrowIfNull(secretMessage);
      ArgumentNullException.ThrowIfNull(puzzleHeading);
      ArgumentNullException.ThrowIfNull(entryListHeading);
      ArgumentException.ThrowIfNullOrWhiteSpace(styleId);
      ArgumentNullException.ThrowIfNull(generation);

      if (blindPercentage is < 0 or > MaximumBlindPercentage)
      {
        throw new ArgumentOutOfRangeException(nameof(blindPercentage));
      }

      if (mode != PuzzleMode.Normal && blindPercentage != 0)
      {
        throw new ArgumentException(
          AppStrings.Get("BlindModeNormalOnly"),
          nameof(blindPercentage));
      }

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

      Entries = new ReadOnlyCollection<PuzzleEntry>(entryArray);
      SecretMessage = secretMessage;
      PuzzleHeading = puzzleHeading.Trim();
      EntryListHeading = entryListHeading.Trim();
      StyleId = styleId;
      Generation = generation;
      RequireExactMessageFit = requireExactMessageFit;
      BlindPercentage = blindPercentage;
    }

    #endregion

    #region Other Stuff

    public List<WordInfo> CreateWordInfos()
    {
      return Entries
        .Select((entry, index) => new WordInfo
        {
          Text = entry.Answer,
          WordNumber = index + 1
        })
        .ToList();
    }

    #endregion
  }
}
