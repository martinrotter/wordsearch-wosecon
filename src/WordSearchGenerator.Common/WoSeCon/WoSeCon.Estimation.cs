using WordSearchGenerator.Common.WoSeCon.Api;

namespace WordSearchGenerator.Common.WoSeCon
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
    public static EstimatedConstructionTime EstimateDifficulty(
      IEnumerable<WordInfo> words,
      int rowCount,
      int columnCount,
      bool quizMode,
      int parallelism = 1)
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

      var wordList = words.ToList();

      ValidateWords(wordList);

      if (wordList.Count == 0)
      {
        return EstimatedConstructionTime.FastInSeconds;
      }

      var cellCount = (long)rowCount * columnCount;
      var questionCellCount = quizMode ? wordList.Count : 0;
      var answerCharacterCount = wordList.Sum(word => (long)word.Text.Length);
      var requiredIntersections = Math.Max(
        0L,
        answerCharacterCount + questionCellCount - cellCount);
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

      var compatibleWordPairs = CountCompatibleWordPairs(wordList);

      // Two straight words can cross at most once. If even the number of
      // compatible word pairs cannot provide the minimum required sharing,
      // the input is at least structurally very unlikely to fit.
      if (requiredIntersections > compatibleWordPairs)
      {
        return EstimatedConstructionTime.LikelyImpossible;
      }

      var maximumPlacementCount = CountLegalPlacements(2, rowCount, columnCount);
      var minimumPlacementRatio = legalPlacementCounts.Min() /
                                  (double)maximumPlacementCount;
      var packingRatio = (answerCharacterCount + questionCellCount) /
                         (double)cellCount;
      var crossingPressure = requiredIntersections == 0
        ? 0
        : requiredIntersections / (double)Math.Max(1L, compatibleWordPairs);

      // These weights intentionally remain simple and visible. They should
      // be recalibrated later from real construction telemetry.
      var score = 0.0;
      score += Scale(packingRatio, 0.45, 1.25) * 35;
      score += Scale(crossingPressure, 0, 0.35) * 25;
      score += Scale(0.25 - minimumPlacementRatio, 0, 0.25) * 15;
      score += Scale(wordList.Count, 5, 50) * 15;

      if (quizMode)
      {
        score += Scale(questionCellCount / (double)cellCount, 0, 0.20) * 10;
      }

      // Independent randomized attempts reduce time to the first solution,
      // but with diminishing returns as more workers are added.
      score -= Math.Min(20, Math.Log2(parallelism) * 4);
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

    private static long CountCompatibleWordPairs(IReadOnlyList<WordInfo> words)
    {
      var characterSets = words
        .Select(word => word.Text.ToHashSet())
        .ToArray();
      long compatiblePairs = 0;

      for (var first = 0; first < characterSets.Length - 1; first++)
      {
        for (var second = first + 1; second < characterSets.Length; second++)
        {
          if (characterSets[first].Overlaps(characterSets[second]))
          {
            compatiblePairs++;
          }
        }
      }

      return compatiblePairs;
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
  }
}