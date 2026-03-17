using System.Collections.Generic;
using System.Linq;
using com.forerunnergames.coa2.core.data;
using com.forerunnergames.coa2.ui;
using com.forerunnergames.coa2.ui.player;
using com.forerunnergames.coa2.utilities;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.core.player;

public partial class Player : CharacterBody2D
{
  // Begin GameData dependencies
  private static float Gravity => GameData.Gravity;
  // End GameData dependencies

  [Export] public float WalkSpeed = 120.0f;
  [Export] public float RunSpeed = 350.0f;
  [Export] public float Acceleration = 2000.0f;
  [Export] public float JumpVelocity = -850.0f;
  public PlayerView View { get; set; } = null!;
  public Vector2 SpawnPosition { get; set; }
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private UI _ui = null!;
  private Timer _iceTimer = null!; // Forces a short fall after slipping on ice, before being allowed to climb again.
  private CollisionShape2D _collider = null!;
  private readonly List <RayCast2D> _rays = [];
  private Vector2 _previousVelocity = Vector2.Zero;
  private int _iceCollisions;
  private bool _wasOnFloor;
  private bool _justSlippedOnIce;
  public void SetBodyCollisionEnabled (bool enabled) => _collider.Disabled = !enabled;

  public override void _Ready()
  {
    _ui = GetNode <UI> ("/root/UI");
    _collider = GetNode <CollisionShape2D> ("%CollisionShape2D");
    _iceTimer = GetNode <Timer> ("%IceTimer");
    for (var i = 1; i <= 4; ++i) _rays.Add (GetNode <RayCast2D> ("%RayCast2D" + i));
  }

  public override void _PhysicsProcess (double delta)
  {
    var velocity = Velocity;
    var inputDirection = Input.GetVector ("move_left", "move_right", "move_up", "move_down");
    var jumpInput = Input.IsActionJustPressed ("jump");
    var speedBoostInput = Input.IsActionPressed ("speed_boost");
    var isOnFloor = IsOnFloor();
    var landed = !_wasOnFloor && isOnFloor;
    var startJumping = jumpInput && isOnFloor;
    var fallVelocity = isOnFloor ? 0.0f : Gravity * (float)delta;
    var horizontalSpeed = inputDirection.X * (speedBoostInput ? RunSpeed : WalkSpeed);
    var horizontalVelocity = Mathf.MoveToward (velocity.X, horizontalSpeed, Acceleration * (float)delta);
    velocity.X = horizontalVelocity;
    velocity.Y += fallVelocity;
    velocity.Y = startJumping ? JumpVelocity : velocity.Y;
    Velocity = velocity;
    View.Update (Velocity, inputDirection.X, speedBoostInput, isOnFloor, landed, startJumping);

    // TODO FIXME
    // GetParent().GetNode <Label> ("%DebugLabel").SetText ($"Velocity: ({Velocity.X:F1}, {Velocity.Y:F1})\nIsOnFloor: {isOnFloor}\nAnimation: {Animator.CurrentAnimation}");

    _wasOnFloor = isOnFloor;
    _previousVelocity = Velocity;
    MoveAndSlide();
    View.SyncToFollowTarget();
    CheckSlippedOnIce(); // TODO Testing
    HandleKinematicCollisions();
    HandleIceCollisions();
  }

  public bool HandleInput (InputEvent @event)
  {
    if (!Input.IsActionJustPressed ("respawn")) return false;
    Respawn();
    return true;
  }

  private void Respawn()
  {
    Velocity = Vector2.Zero;
    GlobalPosition = SpawnPosition;
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
    // _ui.SetDebugText ($"Collider: {terrain} {mapCoords}, Angle: {angleDegrees}");
  }

  // We handle these separately using ray casts because the player doesn't actually collide with ice tiles (physics layer is masked out).
  // Only the ray casts collide with the ice tiles. This is a workaround for not being able to add Area2D's to individual tiles.
  private void HandleIceCollisions()
  {
    _iceCollisions = _rays.Count (r => Tools.GetTerrain (r) == "Icy Cliff");
    if (_iceCollisions == 0 || !_iceTimer.IsStopped()) return;
    _iceTimer.Start();
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

  private void CheckSlippedOnIce()
  {
    if (!CheckIsTouchingIce()) return;
    if (_justSlippedOnIce) return;
    SlipOnIce();
  }

  private bool CheckIsTouchingIce()
  {
    var isTouchingIce = IsTouchingIce();
    if (isTouchingIce) return true;
    _justSlippedOnIce = false;
    return false;
  }

  private void SlipOnIce()
  {
    _justSlippedOnIce = true;
    StartIceSlipCooldown();
    Velocity = new Vector2 (Velocity.X, 0.0f); // Remove any upward velocity so we appear to start falling downward immediately.
  }
}
