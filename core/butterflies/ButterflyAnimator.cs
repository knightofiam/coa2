using Godot;

namespace com.forerunnergames.coa2.core.butterflies;

public partial class ButterflyAnimator : Node2D
{
  [Export] public float IdleAnimationSpeed { get; set; } = 2.0f;
  [Export] public float FlyingAnimationSpeed { get; set; } = 8.5f;
  [Export] public float EvadingAnimationSpeed { get; set; } = 12.0f;
  [Export] public float PerchingAnimationSpeed { get; set; } = 12.0f;
  private AnimatedSprite2D _sprite = null!;
  public override void _Ready() => _sprite = GetNode <AnimatedSprite2D> ("%AnimatedSprite2D");

  public void PlayIdle()
  {
    if (_sprite.Animation == "idle") return;
    _sprite.Play ("idle");
    _sprite.SpeedScale = IdleAnimationSpeed;
  }

  public void PlayFlying()
  {
    if (_sprite.Animation == "flying") return;
    _sprite.Play ("flying");
    _sprite.SpeedScale = FlyingAnimationSpeed;
  }

  public void PlayEvading()
  {
    if (_sprite.Animation == "evading") return;
    _sprite.Play ("evading");
    _sprite.SpeedScale = EvadingAnimationSpeed;
  }

  public void PlayPerching()
  {
    if (_sprite.Animation == "perching") return;
    _sprite.Play ("perching");
    _sprite.SpeedScale = PerchingAnimationSpeed;
  }
}
