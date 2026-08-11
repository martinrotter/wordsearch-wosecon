namespace Wose.Common
{
  public sealed class MessageCannotBePlacedException : InvalidOperationException
  {
    #region Constructors

    public MessageCannotBePlacedException(string message) : base(message)
    {
    }

    #endregion
  }
}
