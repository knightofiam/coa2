using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.player;

public partial class PlayerView : Node2D
{
  [Export] public float WalkSpeed = 120.0f;
  [Export] public float RunSpeed = 350.0f;
  [Export] public float WalkAnimationSpeed = 120.0f;
  [Export] public float RunAnimationSpeed = 350.0f;
  [Export] public string IdleLeftAnimation = "player_idle_left";
  [Export] public string IdleBackAnimation = "player_idle_back";
  [Export] public string ClimbingPrepAnimation = "player_idle_back";
  [Export] public string FreeFallingAnimation = "player_free_falling";
  [Export] public string ClimbingUpAnimation = "player_climbing_up";
  [Export] public string CliffHangingAnimation = "player_cliff_hanging";
  [Export] public string TraversingAnimation = "player_cliff_hanging";
  [Export] public string CliffArrestingAnimation = "player_cliff_arresting";
  [Export] public string WalkLeftAnimation = "player_walking_left";
  [Export] public string RunLeftAnimation = "player_running_left";
  public Node2D FollowTarget { get; set; } = null!;
  public StringName CurrentAnimation => _primaryPlayer.CurrentAnimation;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private UI _ui = null!;
  private AnimationPlayer _primaryPlayer = null!;
  private AnimationPlayer _secondaryPlayer = null!;
  private Vector2 _visualOffset = Vector2.Zero;
  private bool _wasOnFloor;
  public void ResetVisualOffset() => _visualOffset = Vector2.Zero;
  private void OnAnimationFinished (StringName animationName) => Log.Info ("Animation ended: {animationName}", animationName);

  public override void _Ready()
  {
    _ui = GetNode <UI> ("/root/UI");
    _primaryPlayer = GetNode <AnimationPlayer> ("%Primary");
    _secondaryPlayer = GetNode <AnimationPlayer> ("%Secondary");
    _primaryPlayer.AnimationFinished += OnAnimationFinished;
    _primaryPlayer.Play (IdleLeftAnimation);
  }

  // TODO Implement clicking on player to equip/unequip
  public void HandleInput (InputEvent @event)
  {
    if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left } click) return;
    _ui.SetDebugText ($"Clicked player at global position: {click.GlobalPosition}, local position: {ToLocal (click.Position)}");
  }

  public void SyncToFollowTarget()
  {
    Rotation = FollowTarget.Rotation;
    Position = FollowTarget.Position + _visualOffset;
  }

  public void Update (Vector2 currentVelocity, float hDirection = 0.0f, bool isSpeedBoosting = false, bool isOnFloor = false, bool landed = false, bool jumped = false)
  {
    var isIdle = isOnFloor && !jumped && currentVelocity.Length() < 1.0f;
    var isWalking = isOnFloor && !isIdle && !jumped && !isSpeedBoosting;
    var isRunning = isOnFloor && !isIdle && !jumped && isSpeedBoosting;
    var animationName = isWalking ? WalkLeftAnimation : isRunning ? RunLeftAnimation : IdleLeftAnimation;
    var facingRight = Mathf.Sign (hDirection) > 0;
    var animationSpeed = isWalking ? WalkAnimationSpeed : isRunning ? RunAnimationSpeed : 1.0f;
    var movementSpeed = isWalking ? WalkSpeed : isRunning ? RunSpeed : 1.0f;
    var speedScale = movementSpeed / animationSpeed;
    Scale = new Vector2 (facingRight ? -8.0f : 8.0f, 8.0f);
    if (_primaryPlayer.CurrentAnimation == animationName && _primaryPlayer.IsPlaying()) return;
    _primaryPlayer.Play (animationName, customSpeed: speedScale);
    Log.Info ("Playing animation {animationName} at {speedScale:F1} speed", animationName, speedScale);
  }
}
