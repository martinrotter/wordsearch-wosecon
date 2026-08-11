using Wose.Common.WoSeCon.Api;
using Wose.Desktop.Services;

namespace Wose.Desktop.Tests
{
  [TestClass]
  public sealed class MonteCarloProgressTests
  {
    #region Other Stuff

    [TestMethod]
    public void CandidateRejectionsAreCountedImmediatelyInLayoutUnits()
    {
      var aggregator = new MonteCarloPuzzleGenerator.ProgressAggregator(
        2,
        24,
        null);
      aggregator.Update(
        0,
        new ConstructionProgress(24, 24, 24, 24, 100, 5, TimeSpan.Zero));

      aggregator.RecordMessageCapacityRejection();
      aggregator.RecordAmbiguousBoardRejection();

      var snapshot = aggregator.Report(true);

      Assert.AreEqual(2, snapshot.CompletedCandidateCount);
      Assert.AreEqual(1, snapshot.MessageCapacityRejectionCount);
      Assert.AreEqual(1, snapshot.AmbiguousBoardRejectionCount);
      Assert.AreEqual(24, snapshot.PlacedWordCount);
      Assert.AreEqual(2, snapshot.ActiveAttemptCount);
    }

    [TestMethod]
    public void CandidateStatisticsSurviveSuccessAndWorkerCancellation()
    {
      var aggregator = new MonteCarloPuzzleGenerator.ProgressAggregator(
        2,
        3,
        null);
      aggregator.RecordMessageCapacityRejection();
      aggregator.RecordAcceptedCandidate();
      aggregator.Complete(
        0,
        MonteCarloPuzzleGenerator.AttemptCompletion.Succeeded,
        20,
        2);
      aggregator.Complete(
        1,
        MonteCarloPuzzleGenerator.AttemptCompletion.Cancelled,
        10,
        1);

      var snapshot = aggregator.Report(true);

      Assert.AreEqual(2, snapshot.CompletedCandidateCount);
      Assert.AreEqual(1, snapshot.MessageCapacityRejectionCount);
      Assert.AreEqual(0, snapshot.AmbiguousBoardRejectionCount);
      Assert.AreEqual(2, snapshot.FinishedAttemptCount);
      Assert.AreEqual(1, snapshot.CancelledAttemptCount);
      Assert.AreEqual(0, snapshot.ActiveAttemptCount);
    }

    [TestMethod]
    public void AttemptOutcomesRemainSeparateFromCandidateRejections()
    {
      var aggregator = new MonteCarloPuzzleGenerator.ProgressAggregator(
        3,
        3,
        null);
      aggregator.RecordMessageCapacityRejection();
      aggregator.RecordAmbiguousBoardRejection();
      aggregator.Complete(
        0,
        MonteCarloPuzzleGenerator.AttemptCompletion.PlacementFailed,
        0,
        0);
      aggregator.Complete(
        1,
        MonteCarloPuzzleGenerator.AttemptCompletion.MessageRejected,
        0,
        0);
      aggregator.Complete(
        2,
        MonteCarloPuzzleGenerator.AttemptCompletion.AmbiguityRejected,
        0,
        0);

      var snapshot = aggregator.Report(true);

      Assert.AreEqual(1, snapshot.PlacementFailedAttemptCount);
      Assert.AreEqual(1, snapshot.MessageCapacityRejectedAttemptCount);
      Assert.AreEqual(1, snapshot.AmbiguityRejectedAttemptCount);
      Assert.AreEqual(1, snapshot.MessageCapacityRejectionCount);
      Assert.AreEqual(1, snapshot.AmbiguousBoardRejectionCount);
    }

    [TestMethod]
    public void RestartKeepsWorkerActiveAndAccumulatesItsWork()
    {
      var aggregator = new MonteCarloPuzzleGenerator.ProgressAggregator(
        1,
        4,
        null);
      aggregator.Update(
        0,
        new ConstructionProgress(3, 3, 4, 4, 100, 5, TimeSpan.Zero));
      aggregator.Restart(0, 100, 5);
      aggregator.Update(
        0,
        new ConstructionProgress(1, 1, 4, 2, 10, 1, TimeSpan.Zero));

      var snapshot = aggregator.Report(true);

      Assert.AreEqual(1, snapshot.ActiveAttemptCount);
      Assert.AreEqual(0, snapshot.FinishedAttemptCount);
      Assert.AreEqual(0, snapshot.CancelledAttemptCount);
      Assert.AreEqual(1, snapshot.PlacedWordCount);
      Assert.AreEqual(3, snapshot.FurthestPlacedWordCount);
      Assert.AreEqual(110, snapshot.TestedPositions);
      Assert.AreEqual(6, snapshot.Backtrackings);
    }

    #endregion
  }
}
