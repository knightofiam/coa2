using com.forerunnergames.coa2.core.settings;
using com.forerunnergames.coa2.ui.screens;
using Godot;

namespace com.forerunnergames.coa2.ui.effects.screenShake;

public partial class ScreenShakeEffect : Node
{
  [Export] public float Intensity = 30.0f;
  [Export] public float DurationSeconds = 0.3f;
  public void Play (IScreen target) => Play (target.AsControl(), Intensity, DurationSeconds);

  public void Play (Control? target, float intensity, float durationSeconds)
  {
    if (target == null || !Settings.Instance.ScreenShakeEnabled) return;
    var originalPosition = target.Position;
    var tween = CreateTween();

    tween.TweenMethod (Callable.From <float> (t =>
      {
        var offset = new Vector2 ((float)GD.RandRange (-intensity, intensity) * (1 - t), (float)GD.RandRange (-intensity, intensity) * (1 - t));
        target.Position = originalPosition + offset;
      }),
      0.0f,
      1.0f,
      durationSeconds);

    tween.TweenCallback (Callable.From (() => target.Position = originalPosition));
  }
}
