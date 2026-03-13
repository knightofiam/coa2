using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.core.player;

public partial class Player : Node2D
{
  [Export] public Vector2 SpawnPosition = new(0.0f, -250.0f);
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private PlayerBody _body = null!;
  private PlayerAnimator _animator = null!;
  private Label _debugLabel = null!;
  private bool _justSlippedOnIce;

  public override void _Ready()
  {
    _body = GetNode <PlayerBody> ("%PlayerBody");
    _animator = GetNode <PlayerAnimator> ("%PlayerAnimator");
    _debugLabel = GetNode <Label> ("%DebugLabel");
    SetMeta ("is_player", true); // Mark player for butterfly detection
  }

  public override void _PhysicsProcess (double delta)
  {
    _debugLabel.GlobalPosition = _body.GlobalPosition - _body.GetSize() * 24;
    CheckSlippedOnIce();
  }

  public override void _Input (InputEvent @event)
  {
    if (Input.IsActionJustPressed ("respawn")) Respawn();
  }

  private void Respawn()
  {
    _body.Velocity = Vector2.Zero;
    _body.GlobalPosition = SpawnPosition;
  }

  private void CheckSlippedOnIce()
  {
    if (!CheckIsTouchingIce()) return;
    if (_justSlippedOnIce) return;
    SlipOnIce();
  }

  private bool CheckIsTouchingIce()
  {
    var isTouchingIce = _body.IsTouchingIce();
    if (isTouchingIce) return true;
    _justSlippedOnIce = false;
    return false;
  }

  private void SlipOnIce()
  {
    _justSlippedOnIce = true;
    _body.StartIceSlipCooldown();
    _body.Velocity = new Vector2 (_body.Velocity.X, 0.0f); // Remove any upward velocity so we appear to start falling downward immediately.
  }
}
