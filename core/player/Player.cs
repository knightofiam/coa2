using System.Collections.Generic;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.core.player;

public partial class Player : Node2D
{
  [Export] public Vector2 SpawnPosition = new(0.0f, -2000.0f);
  [Export] public Vector2 GrabUpOffset = new(0.0f, -50.0f);
  [Export] public Vector2 GrabDownOffset = new(0.0f, 20.0f);
  private const uint ClimbableMask = 6; // Layer 2 (Ground) | Layer 3 (Cliffs)
  private const float HandIceDetectionRadius = 4;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private PlayerBody _characterBody = null!;
  private PlayerAnimator _animator = null!;
  private CircleShape2D _handIceDetector = null!;
  private bool _justSlippedOnIce;
  private Dictionary <GrabDirection, Vector2> _grabDirectionsToOffsets = null!;
  public override void _PhysicsProcess (double delta) => CheckSlippedOnIce();

  private enum GrabDirection
  {
    Up,
    Down
  }

  private enum HandDirection
  {
    Left,
    Right
  }

  public override void _Ready()
  {
    _characterBody = GetNode <PlayerBody> ("PlayerBody");
    _animator = GetNode <PlayerAnimator> ("PlayerAnimator");
    _handIceDetector = new CircleShape2D { Radius = HandIceDetectionRadius }; // TODO Create in editor.

    _grabDirectionsToOffsets = new Dictionary <GrabDirection, Vector2>
    {
      { GrabDirection.Up, GrabUpOffset },
      { GrabDirection.Down, GrabDownOffset }
    };

    // Mark player for butterfly detection
    SetMeta("is_player", true);
  }

  public override void _Input (InputEvent @event)
  {
    if (Input.IsActionJustPressed ("respawn")) Respawn();
    if (Input.IsActionJustPressed ("grab_up_left")) StartGrab (HandDirection.Left, GrabDirection.Up);
    if (Input.IsActionJustPressed ("grab_up_right")) StartGrab (HandDirection.Right, GrabDirection.Up);
    if (Input.IsActionJustPressed ("grab_down_left")) StartGrab (HandDirection.Left, GrabDirection.Down);
    if (Input.IsActionJustPressed ("grab_down_right")) StartGrab (HandDirection.Right, GrabDirection.Down);
    if (Input.IsActionJustReleased ("grab_up_left")) StopGrab (HandDirection.Left);
    if (Input.IsActionJustReleased ("grab_up_right")) StopGrab (HandDirection.Right);
    if (Input.IsActionJustReleased ("grab_down_left")) StopGrab (HandDirection.Left);
    if (Input.IsActionJustReleased ("grab_down_right")) StopGrab (HandDirection.Right);
  }

  private void StartGrab (HandDirection handDirection, GrabDirection grabDirection) { }
  private void StopGrab (HandDirection direction) { }

  private void Respawn()
  {
    _characterBody.Velocity = Vector2.Zero;
    _characterBody.GlobalPosition = SpawnPosition;
  }

  private Vector2 CalculateGrabLocation (Vector2 fromWorld, GrabDirection direction)
  {
    var toWorld = fromWorld + _grabDirectionsToOffsets[direction];
    var space = GetWorld2D().DirectSpaceState; // Try a short ray so we stick to real geometry if present
    var query = PhysicsRayQueryParameters2D.Create (fromWorld, toWorld, collisionMask: ClimbableMask);
    var hit = space.IntersectRay (query);
    if (hit.Count > 0 && hit.TryGetValue ("position", out var p)) return (Vector2)p;
    return toWorld; // No hit? Still reach in air. # TODO Only climb up when actually grabbing something.
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
