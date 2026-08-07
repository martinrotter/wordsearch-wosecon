using System.Diagnostics;
using WordSearchGenerator.Common;
using WordSearchGenerator.Common.WoSeCon;
using WordSearchGenerator.Common.WoSeCon.Api;
using WordSearchGenerator.Desktop.Models;

namespace WordSearchGenerator.Desktop.Services
{
  public sealed class SinglePuzzleGenerator : ISinglePuzzleGenerator
  {
    #region Interface Implementations

    public Task<GenerationResult> GenerateAsync(
      PuzzleDefinition definition,
      IProgress<ConstructionProgress>? progress,
      CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(definition);

      return Task.Run(
        () => Generate(definition, progress, cancellationToken),
        cancellationToken);
    }

    #endregion

    #region Other Stuff

    private static GenerationResult Generate(
      PuzzleDefinition definition,
      IProgress<ConstructionProgress>? progress,
      CancellationToken cancellationToken)
    {
      var stopwatch = Stopwatch.StartNew();
      var generator = new WoSeCon(
        definition.CreateWordInfos(),
        definition.Rows,
        definition.Columns,
        definition.QuizMode);

      generator.Construct(progress, cancellationToken);
      cancellationToken.ThrowIfCancellationRequested();

      var boardWithoutMessage = new Board(
        generator.Words,
        definition.Rows,
        definition.Columns,
        definition.QuizMode);
      var availableCellCount = boardWithoutMessage.Matrix
        .OfType<Board.Cell>()
        .Count(cell => cell.Type == Board.Cell.CellType.Empty);

      if (definition.SecretMessage.Length > availableCellCount)
      {
        throw new InsufficientMessageCapacityException(
          definition.SecretMessage.Length,
          availableCellCount);
      }

      cancellationToken.ThrowIfCancellationRequested();

      var board = new Board(
        generator.Words,
        definition.Rows,
        definition.Columns,
        definition.QuizMode,
        definition.SecretMessage);

      stopwatch.Stop();

      return new GenerationResult(
        definition,
        board,
        stopwatch.Elapsed,
        generator.TestedPositions,
        generator.Backtrackings);
    }

    #endregion
  }
}
