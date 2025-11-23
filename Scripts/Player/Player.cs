using System.Collections.Generic;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa.player;

public partial class Player : Node2D
{
  [Export] public Vector2 SpawnPosition = new(0.0f, -2000.0f);
  [Export] public Vector2 GrabUpOffset = new(0.0f, -50.0f);
  [Export] public Vector2 GrabDownOffset = new(0.0f, 20.0f);
  private const uint IceMask = 8; // Layer 4 (Ice Cliffs)
  private const uint ClimbableMask = 6; // Layer 2 (Ground) | Layer 3 (Cliffs)
  private const float HandIceDetectionRadius = 4;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private PlayerBody _characterBody = null!;
  private PlayerHand _leftHand = null!;
  private PlayerHand _rightHand = null!;
  private PlayerBodyAnchor _bodyAnchor = null!;
  private PlayerAnimator _animator = null!;
  private CircleShape2D _handIceDetector = null!;
  private bool _justSlippedOnIce;
  private Dictionary <GrabDirection, Vector2> _grabDirectionsToOffsets = null!;
  public override void _PhysicsProcess (double delta) => CheckSlippedOnIce();
  private PlayerHand GetHand (HandDirection direction) => direction is HandDirection.Left ? _leftHand : _rightHand;
  private PlayerHand GetHandOppositeOf (HandDirection direction) => direction is HandDirection.Left ? _rightHand : _leftHand;
  private Vector2 GetHandPosition (HandDirection direction) => GetHand (direction).GlobalPosition;
  private bool IsGrabbingWithOppositeOf (HandDirection direction) => GetHandOppositeOf (direction).IsGrabbing;
  private bool IsAnyHandGrabbing() => _leftHand.IsGrabbing || _rightHand.IsGrabbing;

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
    _leftHand = GetNode <PlayerHand> ("PlayerLeftHand");
    _rightHand = GetNode <PlayerHand> ("PlayerRightHand");
    _bodyAnchor = GetNode <PlayerBodyAnchor> ("PlayerBodyAnchor");
    _animator = GetNode <PlayerAnimator> ("PlayerAnimator");
    _characterBody.AnchorPath = _bodyAnchor.GetPath(); // Let body know where the anchor is for follow mode.
    _handIceDetector = new CircleShape2D { Radius = HandIceDetectionRadius }; // TODO Create in editor.

    _grabDirectionsToOffsets = new Dictionary <GrabDirection, Vector2>
    {
      { GrabDirection.Up, GrabUpOffset },
      { GrabDirection.Down, GrabDownOffset }
    };
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

  private void StartGrab (HandDirection handDirection, GrabDirection grabDirection)
  {
    CheckStartFollowing();
    var grabHand = GetHand (handDirection);
    var grabLocation = IsGrabbingWithOppositeOf (handDirection) ? CalculateGrabLocation (grabHand.GlobalPosition, grabDirection) : grabHand.GlobalPosition;
    grabHand.GrabAt (grabLocation);
  }

  private void StopGrab (HandDirection direction)
  {
    GetHand (direction).ReleaseGrab();
    CheckStopFollowing();
  }

  private void CheckStartFollowing()
  {
    if (!_bodyAnchor.IsFollowing) return;
    StartFollowing();
  }

  private void CheckStopFollowing()
  {
    if (IsAnyHandGrabbing()) return;
    StopFollowing();
  }

  private void StartFollowing()
  {
    _bodyAnchor.SetFollowing (false); // Dynamic, joints can pull anchor.
    _characterBody.IsFollowing = true; // Character follows anchor.
    _characterBody.SetBodyCollisionEnabled (false); // avoid double-collisions with world
    _animator.SetFollowTarget (_bodyAnchor);
  }

  // Reset character rotation & velocity, & set the CharacterBody2D to lead while the anchor body follows.
  private void StopFollowing()
  {
    _characterBody.Rotation = 0.0f;
    _bodyAnchor.Rotation = 0.0f;
    _bodyAnchor.AngularVelocity = 0.0f;
    _bodyAnchor.SetFollowing (true);
    _characterBody.IsFollowing = false;
    _characterBody.SetBodyCollisionEnabled (true);
    _animator.SetFollowTarget (_characterBody);
  }

  private void ReleaseAllGrabs()
  {
    _leftHand.ReleaseGrab();
    _rightHand.ReleaseGrab();
  }

  private void Respawn()
  {
    ReleaseAllGrabs();
    StopFollowing();
    _characterBody.Velocity = Vector2.Zero;
    _characterBody.GlobalPosition = SpawnPosition;
    _bodyAnchor.LinearVelocity = Vector2.Zero;
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
    if (!_characterBody.IsFollowing) return; // If the character body is leading, it will detect ice slips. This would mean the hands are grabbing
    if (!CheckIsTouchingIce()) return;
    if (_justSlippedOnIce) return;
    SlipOnIce();
  }

  private bool CheckIsTouchingIce()
  {
    var isTouchingIce = _characterBody.IsTouchingIce() || IsHandTouchingIce (HandDirection.Left) || IsHandTouchingIce (HandDirection.Right);
    if (isTouchingIce) return true;
    _justSlippedOnIce = false;
    return false;
  }

  private bool IsHandTouchingIce (HandDirection handDirection)
  {
    var shapeParams = new PhysicsShapeQueryParameters2D
    {
      ShapeRid = _handIceDetector.GetRid(),
      Transform = new Transform2D (0, GetHandPosition (handDirection)), // Center detector on hand position.
      CollisionMask = IceMask,
      CollideWithBodies = true,
      CollideWithAreas = true,
    };

    return GetWorld2D().DirectSpaceState.IntersectShape (shapeParams, maxResults: 1).Count > 0;
  }

  private void SlipOnIce()
  {
    _justSlippedOnIce = true;
    ReleaseAllGrabs();
    _characterBody.StartIceSlipCooldown();
    StopFollowing(); // Stop following so the kinematic body leads & falls under gravity.
    _characterBody.Velocity = new Vector2 (_characterBody.Velocity.X, 0.0f); // Remove any upward velocity so we appear to start falling downward immediately.
  }
}
