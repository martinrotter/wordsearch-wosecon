namespace WordSearchGenerator.Desktop.ViewModels
{
  public enum EditorActionState
  {
    Invalid,
    Ready,
    Generating,
    Completed,
    MessageDidNotFit,
    Failed,
    Cancelled
  }
}
