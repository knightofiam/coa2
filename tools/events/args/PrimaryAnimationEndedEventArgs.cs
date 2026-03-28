namespace com.forerunnergames.coa2.tools.events.args;

// Emitted only for non-looping animations
public class PrimaryAnimationEndedEventArgs (string animationName, bool wasLooping) : EventBusEventArgs
{
  public string AnimationName { get; } = animationName;
  public bool WasLooping { get; } = wasLooping;
}
