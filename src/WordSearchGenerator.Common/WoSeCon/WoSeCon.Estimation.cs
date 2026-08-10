using Wose.Common.WoSeCon.Api;

namespace Wose.Common.WoSeCon
{
  public partial class WoSeCon
  {
    #region Enums

    public enum EstimatedConstructionTime
    {
      FastInSeconds,
      FastUnderMinute,
      SlowFewMinutes,
      SlowerManyMinutes,
      CrazySlowHours,
      LikelyImpossible
    }

    #endregion

    #region Other Stuff

    /// <summary>
    ///   Produces an immediate, deterministic estimate without running the
    ///   construction algorithm. The result is a heuristic difficulty band,
    ///   not a guarantee that construction will finish within that time.
    /// </summary>
    /// <param name="requiredVacantCellCount">
    ///   Minimum cells that must remain outside all word placements. Pass the
    ///   normal-mode secret-message length, or zero when no vacant capacity is
    ///   required.
    /// </param>
    public static EstimatedConstructionTime EstimateDifficulty(
      IEnumerable<WordInfo> words,
      int rowCount,
      int columnCount,
      bool quizMode,
      int parallelism = 1,
      int requiredVacantCellCount = 0)
    {
      ArgumentNullException.ThrowIfNull(words);

      if (rowCount <= 0)
      {
        throw new ArgumentOutOfRangeException(
          nameof(rowCount),
          rowCount,
          "Row count must be positive.");
      }

      if (columnCount <= 0)
      {
        throw new ArgumentOutOfRangeException(
          nameof(columnCount),
          columnCount,
          "Column count must be positive.");
      }

      if (parallelism <= 0)
      {
        throw new ArgumentOutOfRangeException(
          nameof(parallelism),
          parallelism,
          "Parallelism must be positive.");
      }

      if (requiredVacantCellCount < 0)
      {
        throw new ArgumentOutOfRangeException(
          nameof(requiredVacantCellCount),
          requiredVacantCellCount,
          "Required vacant-cell count cannot be negative.");
      }

      var wordList = words.ToList();

      ValidateWords(wordList);

      var cellCount = (long)rowCount * columnCount;

      if (requiredVacantCellCount > cellCount)
      {
        return EstimatedConstructionTime.LikelyImpossible;
      }

      if (wordList.Count == 0)
      {
        return EstimatedConstructionTime.FastInSeconds;
      }

      var availablePlacementCellCount = cellCount - requiredVacantCellCount;
      var questionCellCount = quizMode ? wordList.Count : 0;
      var answerCharacterCount = wordList.Sum(word => (long)word.Text.Length);
      var requiredIntersections = Math.Max(
        0L,
        answerCharacterCount +
        questionCellCount -
        availablePlacementCellCount);
      var placementLengths = wordList
        .Select(word => word.Text.Length + (quizMode ? 1 : 0))
        .ToArray();
      var legalPlacementCounts = placementLengths
        .Select(length => CountLegalPlacements(length, rowCount, columnCount))
        .ToArray();

      if (legalPlacementCounts.Any(count => count == 0))
      {
        return EstimatedConstructionTime.LikelyImpossible;
      }

      var crossingStatistics = CalculateCrossingStatistics(wordList);

      // Two straight words can cross at most once. If even the number of
      // compatible word pairs cannot provide the minimum required sharing,
      // the input is at least structurally very unlikely to fit.
      if (requiredIntersections > crossingStatistics.CompatiblePairCount)
      {
        return EstimatedConstructionTime.LikelyImpossible;
      }

      var maximumPlacementCount = CountLegalPlacements(2, rowCount, columnCount);
      var minimumPlacementRatio = legalPlacementCounts.Min() /
                                  (double)maximumPlacementCount;
      var packingRatio = (answerCharacterCount + questionCellCount) /
                         Math.Max(1.0, availablePlacementCellCount);
      var crossingScarcity = requiredIntersections == 0
        ? 0
        : requiredIntersections /
          (double)Math.Max(1L, crossingStatistics.MatchingPositionPairCount);
      var averageWordLength = answerCharacterCount / (double)wordList.Count;
      var maximumCharacterShare = wordList
                                    .SelectMany(word => word.Text)
                                    .GroupBy(character => character)
                                    .Max(group => group.LongCount()) /
                                  (double)answerCharacterCount;

      // Calibrated against the repository's Phase 2 and Phase 3 benchmark
      // corpus. The score deliberately favors conservative warnings because
      // randomized search has a very long seed-dependent tail.
      var score = 0.0;
      score += Scale(packingRatio, 0.55, 1.35) * 22;
      score += Scale(crossingScarcity, 0.035, 0.20) * 50;
      score += Scale(0.30 - minimumPlacementRatio, 0, 0.30) * 10;
      score += Scale(wordList.Count, 7, 30) * 24;
      score += Scale(requiredIntersections, 6, 80) * 16;

      // Many words at high density create a deep search tree even when the
      // words provide plentiful crossing choices.
      score += Scale(wordList.Count, 12, 30) *
               Scale(packingRatio, 0.90, 1.20) *
               18;

      if (quizMode)
      {
        score += Scale(questionCellCount / (double)cellCount, 0, 0.25) * 5;
      }
      else
      {
        // The normal-mode completion validator rejects boards containing
        // additional occurrences. Short, repetitive words and large families
        // built from almost the same alphabet are especially prone to this.
        score += Scale(5 - averageWordLength, 0, 2) *
                 Scale(maximumCharacterShare, 0.20, 0.60) *
                 Scale(packingRatio, 0.75, 1.00) *
                 28;
        score += Scale(
                   crossingStatistics.MeanCharacterSetSimilarity,
                   0.35,
                   0.90) *
                 Scale(wordList.Count, 6, 12) *
                 25;
      }

      // Independent randomized attempts reduce time to the first solution,
      // but only while CPU capacity is available, and with diminishing returns.
      var effectiveParallelism = Math.Min(
        parallelism,
        Math.Max(1, Environment.ProcessorCount));
      score -= Math.Min(10, Math.Log2(effectiveParallelism) * 3);
      score = Math.Max(0, score);

      if (score < 25)
      {
        return EstimatedConstructionTime.FastInSeconds;
      }

      if (score < 40)
      {
        return EstimatedConstructionTime.FastUnderMinute;
      }

      if (score < 58)
      {
        return EstimatedConstructionTime.SlowFewMinutes;
      }

      if (score < 75)
      {
        return EstimatedConstructionTime.SlowerManyMinutes;
      }

      return EstimatedConstructionTime.CrazySlowHours;
    }

    private static CrossingStatistics CalculateCrossingStatistics(
      IReadOnlyList<WordInfo> words)
    {
      var characterCounts = words
        .Select(word => word.Text
          .GroupBy(character => character)
          .ToDictionary(group => group.Key, group => group.LongCount()))
        .ToArray();
      long compatiblePairs = 0;
      long matchingPositionPairs = 0;
      var characterSetSimilaritySum = 0.0;
      var pairCount = 0L;

      for (var first = 0; first < characterCounts.Length - 1; first++)
      {
        for (var second = first + 1;
             second < characterCounts.Length;
             second++)
        {
          var firstCounts = characterCounts[first];
          var secondCounts = characterCounts[second];
          var sharedCharacterCount = 0;

          foreach (var characterCount in firstCounts)
          {
            if (!secondCounts.TryGetValue(
                  characterCount.Key,
                  out var secondCount))
            {
              continue;
            }

            sharedCharacterCount++;
            matchingPositionPairs += characterCount.Value * secondCount;
          }

          if (sharedCharacterCount > 0)
          {
            compatiblePairs++;
          }

          var combinedCharacterCount = firstCounts.Count +
                                       secondCounts.Count -
                                       sharedCharacterCount;
          characterSetSimilaritySum += sharedCharacterCount /
                                       (double)combinedCharacterCount;
          pairCount++;
        }
      }

      return new CrossingStatistics(
        compatiblePairs,
        matchingPositionPairs,
        pairCount == 0 ? 0 : characterSetSimilaritySum / pairCount);
    }

    private static long CountLegalPlacements(
      int length,
      int rowCount,
      int columnCount)
    {
      var horizontalSpan = Math.Max(0L, (long)columnCount - length + 1);
      var verticalSpan = Math.Max(0L, (long)rowCount - length + 1);
      var horizontal = 2L * rowCount * horizontalSpan;
      var vertical = 2L * columnCount * verticalSpan;
      var diagonal = 4L * horizontalSpan * verticalSpan;

      return horizontal + vertical + diagonal;
    }

    private static double Scale(double value, double minimum, double maximum)
    {
      if (value <= minimum)
      {
        return 0;
      }

      if (value >= maximum)
      {
        return 1;
      }

      return (value - minimum) / (maximum - minimum);
    }

    #endregion

    #region Nested Types

    private readonly record struct CrossingStatistics(
      long CompatiblePairCount,
      long MatchingPositionPairCount,
      double MeanCharacterSetSimilarity);

    #endregion
  }
}
