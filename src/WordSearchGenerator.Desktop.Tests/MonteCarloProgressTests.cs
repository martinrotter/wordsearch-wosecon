using WordSearchGenerator.Common.WoSeCon.Api;
using WordSearchGenerator.Desktop.Models;
using WordSearchGenerator.Desktop.Services;

namespace WordSearchGenerator.Desktop.Tests
{
  [TestClass]
  public sealed class MonteCarloProgressTests
  {
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
  }
}
