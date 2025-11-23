using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa.player;

public partial class PlayerAnimator : Node2D
{
  [Export] public AnimatedSprite2D Sprite = null!;
  [Export] public NodePath DefaultFollowTargetPath = null!;
  [Export] public Vector2 NormalSpriteScale = new(2.0f, 2.0f);
  [Export] public Vector2 ClimbingSpriteScale = new(2.25f, 2.25f);
  [Export] public float WalkSpeed = 100.0f;
  [Export] public float RunSpeed = 300.0f;
  [Export] public float WalkAnimationSpeed = 40.0f;
  [Export] public float RunAnimationSpeed = 300.0f;
  public string CurrentAnimation => Sprite.Animation;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private Node2D _followTarget = null!;
  private bool _wasOnFloor;
  public void SetFollowTarget (Node2D node) => _followTarget = node;
  private void OnAnimationLooped() => Log.Trace ("Animation looped: {animationName}", Sprite.Animation);
  private void OnAnimationFinished() => Log.Info ("Animation ended: {animationName}", Sprite.Animation);

  public override void _Ready()
  {
    _followTarget = GetNode <Node2D> (DefaultFollowTargetPath);
    Sprite.AnimationLooped += OnAnimationLooped;
    Sprite.AnimationFinished += OnAnimationFinished;
    Sprite.Play ("idle");
  }

  public override void _Process (double delta)
  {
    GlobalRotation = _followTarget.GlobalRotation;
    GlobalPosition = _followTarget.GlobalPosition;
  }

  public void UpdateFromCharacterBody (Vector2 currentVelocity, float hDirection = 0.0f, bool isSpeedBoosting = false, bool isOnFloor = false, bool landed = false, bool jumped = false)
  {
    var isIdle = isOnFloor && !jumped && currentVelocity.Length() < 1.0f;
    var isWalking = isOnFloor && !isIdle && !jumped && !isSpeedBoosting;
    var isRunning = isOnFloor && !isIdle && !jumped && isSpeedBoosting;
    var animationName = isWalking ? "walk" : isRunning ? "run" : jumped ? "jump" : landed ? "land" : "idle";
    var facingLeft = Mathf.Sign (hDirection) < 0;
    var animationSpeed = isWalking ? WalkAnimationSpeed : isRunning ? RunAnimationSpeed : 1.0f;
    var movementSpeed = isWalking ? WalkSpeed : isRunning ? RunSpeed : 1.0f;
    var speedScale = movementSpeed / animationSpeed;
    Sprite.FlipH = facingLeft;
    Sprite.Scale = NormalSpriteScale;
    if (Sprite.Animation == animationName && Sprite.IsPlaying()) return;
    if (animationName == "idle" && Sprite.IsPlaying() && (Sprite.Animation == "land" || Sprite.Animation == "jump")) return;
    Sprite.Play (animationName, speedScale);
    Log.Info ("Playing animation {animationName} at {speedScale:F1} speed", animationName, speedScale);
  }

  public void UpdateFromBodyAnchor (int climbingFrameIndex)
  {
    Sprite.Animation = "climb";
    Sprite.Scale = ClimbingSpriteScale;
    if (Sprite.IsPlaying()) Sprite.Pause();
    Sprite.Frame = Mathf.Clamp (climbingFrameIndex, 0, Sprite.SpriteFrames.GetFrameCount ("climb") - 1);
  }
}
