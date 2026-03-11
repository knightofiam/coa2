using com.forerunnergames.coa2.ui.messages;

namespace com.forerunnergames.coa2.tools.events.args;

public class GameMessageShownEventArgs (GameMessage message) : EventBusEventArgs
{
  public GameMessage Message { get; } = message;
}
