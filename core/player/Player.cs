using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.core.player;

public partial class Player : Node2D
{
  [Export] public Vector2 SpawnPosition = new(0.0f, -2000.0f * 24.0f);
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private PlayerBody _characterBody = null!;
  private PlayerAnimator _animator = null!;
  private bool _justSlippedOnIce;
  public override void _PhysicsProcess (double delta) => CheckSlippedOnIce();

  public override void _Ready()
  {
    _characterBody = GetNode <PlayerBody> ("PlayerBody");
    _animator = GetNode <PlayerAnimator> ("PlayerAnimator");
    // Mark player for butterfly detection
    SetMeta ("is_player", true);
  }

  public override void _Input (InputEvent @event)
  {
    if (Input.IsActionJustPressed ("respawn")) Respawn();
  }

  private void Respawn()
  {
    _characterBody.Velocity = Vector2.Zero;
    _characterBody.GlobalPosition = SpawnPosition;
  }

  private void CheckSlippedOnIce()
  {
    if (!CheckIsTouchingIce()) return;
    if (_justSlippedOnIce) return;
    SlipOnIce();
  }

  private bool CheckIsTouchingIce()
  {
    var isTouchingIce = _characterBody.IsTouchingIce();
    if (isTouchingIce) return true;
    _justSlippedOnIce = false;
    return false;
  }

  private void SlipOnIce()
  {
    _justSlippedOnIce = true;
    _characterBody.StartIceSlipCooldown();
    _characterBody.Velocity = new Vector2 (_characterBody.Velocity.X, 0.0f); // Remove any upward velocity so we appear to start falling downward immediately.
  }
}
