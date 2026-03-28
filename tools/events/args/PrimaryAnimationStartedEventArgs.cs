namespace com.forerunnergames.coa2.tools.events.args;

// Emitted once for all animations, regardless of looping.
public class PrimaryAnimationStartedEventArgs (string animationName, bool isLooping) : EventBusEventArgs
{
  public string AnimationName { get; } = animationName;
  public bool IsLooping { get; } = isLooping;
}
