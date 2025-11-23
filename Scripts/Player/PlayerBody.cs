using System.Collections.Generic;
using System.Linq;
using com.forerunnergames.coa.game;
using com.forerunnergames.coa.settings;
using com.forerunnergames.coa.utilities;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa.player;

public partial class PlayerBody : CharacterBody2D
{
  [Export] public PlayerHand LeftHand = null!;
  [Export] public PlayerHand RightHand = null!;
  [Export] public NodePath AnchorPath = null!;
  [Export] public PlayerAnimator Animator = null!;
  [Export] public float WalkSpeed = 100.0f;
  [Export] public float RunSpeed = 300.0f;
  [Export] public float Acceleration = 2000.0f;
  [Export] public float JumpVelocity = -400.0f;
  public bool IsFollowing { get; set; }
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private Game _game = null!;
  private Timer _iceTimer = null!; // Forces a short fall after slipping on ice, before being allowed to climb again.
  private RigidBody2D _anchor = null!;
  private CollisionShape2D _collider = null!;
  private readonly List <RayCast2D> _rays = [];
  private int _iceCollisions;
  private bool _wasOnFloor;
  private Vector2 _previousVelocity = Vector2.Zero;
  public void SetBodyCollisionEnabled (bool enabled) => _collider.Disabled = !enabled;

  public override void _Ready()
  {
    _game = GetNode <Game> ("/root/Game");
    _collider = GetNode <CollisionShape2D> ("CollisionShape2D");
    _iceTimer = GetNode <Timer> ("IceTimer");
    _anchor = GetNode <RigidBody2D> (AnchorPath);
    for (var i = 1; i <= 4; ++i) _rays.Add (GetNode <RayCast2D> ("RayCast2D" + i));
  }

  public override void _PhysicsProcess (double delta)
  {
    if (CheckFollowing()) return; // Allow the anchor to lead with physics.
    var velocity = Velocity;
    var inputDirection = Input.GetVector ("move_left", "move_right", "move_up", "move_down");
    var jumpInput = Input.IsActionJustPressed ("jump");
    var speedBoostInput = Input.IsActionPressed ("speed_boost");
    var isIceTimerStopped = _iceTimer.IsStopped();
    var isOnFloor = IsOnFloor();
    var landed = !_wasOnFloor && isOnFloor;
    var startJumping = jumpInput && isOnFloor;
    var fallVelocity = isOnFloor ? 0.0f : Settings.Gravity * (float)delta;
    var horizontalSpeed = inputDirection.X * (speedBoostInput ? RunSpeed : WalkSpeed);
    var horizontalVelocity = Mathf.MoveToward (velocity.X, horizontalSpeed, Acceleration * (float)delta);
    velocity.X = horizontalVelocity;
    velocity.Y += fallVelocity;
    velocity.Y = startJumping ? JumpVelocity : velocity.Y;
    LeftHand.IsBodyOnFloor = isOnFloor;
    RightHand.IsBodyOnFloor = isOnFloor;
    LeftHand.IsIceTimerStopped = isIceTimerStopped;
    RightHand.IsIceTimerStopped = isIceTimerStopped;
    Velocity = velocity;
    Animator.UpdateFromCharacterBody (Velocity, inputDirection.X, speedBoostInput, isOnFloor, landed, startJumping);
    _game.SetDebugText ($"Velocity: ({Velocity.X:F1}, {Velocity.Y:F1}), IsOnFloor: {isOnFloor}, Animation: {Animator.CurrentAnimation}");
    _wasOnFloor = isOnFloor;
    _previousVelocity = Velocity;
    MoveAndSlide();
    HandleKinematicCollisions();
    HandleIceCollisions();
  }

  private void HandleKinematicCollisions()
  {
    for (var i = 0; i < GetSlideCollisionCount(); ++i) HandleKinematicCollision (GetSlideCollision (i));
  }

  private void HandleKinematicCollision (KinematicCollision2D collision)
  {
    if (collision.GetCollider() is not TileMapLayer tileMapLayer) return;
    var angleDegrees = Mathf.RadToDeg (collision.GetAngle (Vector2.Up));
    var (mapCoords, terrain) = Tools.GetTileAt (collision.GetPosition(), tileMapLayer);
    // Log.Debug ("Last slide: {collisionPosition}", collision.GetPosition());
    // Log.Debug ("{terrain} {mapCoords}", terrain, mapCoords);
    // _game.SetDebugText ($"Collider: {terrain} {mapCoords}, Angle: {angleDegrees}");
  }

  // We handle these separately using ray casts because the player doesn't actually collide with ice tiles (physics layer is masked out).
  // Only the ray casts collide with the ice tiles. This is a workaround for not being able to add Area2D's to individual tiles.
  private void HandleIceCollisions()
  {
    _iceCollisions = _rays.Count (r => Tools.GetTerrain (r) == "Icy Cliff");
    if (_iceCollisions == 0 || !_iceTimer.IsStopped()) return;
    _iceTimer.Start();
  }

  private bool CheckFollowing()
  {
    if (!IsFollowing) return false;
    Follow();
    return true;
  }

  private void Follow()
  {
    GlobalTransform = _anchor.GlobalTransform;
    Velocity = Vector2.Zero;
  }

  public bool IsTouchingIce()
  {
    for (var i = 1; i <= 4; ++i)
    {
      var ray = GetNode <RayCast2D> ($"RayCast2D{i}");
      if (Tools.GetTerrain (ray) == "Icy Cliff") return true;
    }

    return false;
  }

  public void StartIceSlipCooldown()
  {
    if (!_iceTimer.IsStopped()) return;
    _iceTimer.Start();
  }
}
