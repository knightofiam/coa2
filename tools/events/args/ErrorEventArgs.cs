namespace com.forerunnergames.coa2.tools.events.args;

public class ErrorEventArgs (string message) : EventBusEventArgs
{
  public string Message { get; set; } = message;
}
