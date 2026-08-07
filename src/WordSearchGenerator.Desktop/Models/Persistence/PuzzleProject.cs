namespace WordSearchGenerator.Desktop.Models.Persistence
{
  public sealed record PuzzleProject(
    PuzzleDefinition Definition,
    GenerationResult? GeneratedResult);
}