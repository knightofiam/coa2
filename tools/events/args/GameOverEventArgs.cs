namespace com.forerunnergames.coa2.tools.events.args;

public class GameOverEventArgs (int? winnerId) : EventBusEventArgs
{
  public int? WinnerId { get; } = winnerId;
}
