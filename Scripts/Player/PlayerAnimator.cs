using Godot;
using NLog;

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
  [Export] public Godot.Collections.Array <Vector2> ClimbLeftSocketByFrame { get; set; } = [];
  [Export] public Godot.Collections.Array <Vector2> ClimbRightSocketByFrame { get; set; } = [];
  public string CurrentAnimation => Sprite.Animation;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private Node2D _followTarget = null!;
  private Marker2D _leftHandSocket = null!;
  private Marker2D _rightHandSocket = null!;
  private Vector2 _visualOffset = Vector2.Zero;
  private bool _wasOnFloor;
  public void SetFollowTarget (Node2D node) => _followTarget = node;
  public void ResetVisualOffset() => _visualOffset = Vector2.Zero;
  private void OnAnimationLooped() => Log.Trace ("Animation looped: {animationName}", Sprite.Animation);
  private void OnAnimationFinished() => Log.Info ("Animation ended: {animationName}", Sprite.Animation);

  public override void _Ready()
  {
    _followTarget = GetNode <Node2D> (DefaultFollowTargetPath);
    _leftHandSocket = GetNode <Marker2D> ("LeftHandSocket");
    _rightHandSocket = GetNode <Marker2D> ("RightHandSocket");
    Sprite.AnimationLooped += OnAnimationLooped;
    Sprite.AnimationFinished += OnAnimationFinished;
    Sprite.Play ("idle");
  }

  public override void _PhysicsProcess (double delta)
  {
    GlobalRotation = _followTarget.GlobalRotation;
    GlobalPosition = _followTarget.GlobalPosition + _visualOffset;
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
    Scale = NormalSpriteScale * new Vector2 (facingLeft ? -1 : 1, 1.0f);
    if (Sprite.Animation == animationName && Sprite.IsPlaying()) return;
    if (animationName == "idle" && Sprite.IsPlaying() && (Sprite.Animation == "land" || Sprite.Animation == "jump")) return;
    Sprite.Play (animationName, speedScale);
    Log.Info ("Playing animation {animationName} at {speedScale:F1} speed", animationName, speedScale);
  }

  public void UpdateFromBodyAnchor (int climbingFrameIndex)
  {
    Sprite.Animation = "climb";
    Scale = ClimbingSpriteScale; // No flipping while climbing.
    if (Sprite.IsPlaying()) Sprite.Pause();
    var max = Sprite.SpriteFrames.GetFrameCount ("climb") - 1;
    var frame = Mathf.Clamp (climbingFrameIndex, 0, max);
    Sprite.Frame = frame;
    if (ClimbLeftSocketByFrame.Count > frame) _leftHandSocket.Position = ClimbLeftSocketByFrame[frame];
    if (ClimbRightSocketByFrame.Count > frame) _rightHandSocket.Position = ClimbRightSocketByFrame[frame];
  }

  // Keep sprite hands on the real hands while climbing.
  public void AlignSpriteToHands (Vector2 leftHandWorld, Vector2 rightHandWorld, float clamp = 6.0f, float lerp = 0.35f)
  {
    var leftErr = leftHandWorld - _leftHandSocket.GlobalPosition;
    var rightErr = rightHandWorld - _rightHandSocket.GlobalPosition;
    var correction = (leftErr + rightErr) * 0.5f;
    if (correction.Length() > clamp) correction = correction.Normalized() * clamp; // Prevent big pops # TODO Testing
    var target = _visualOffset + correction;
    _visualOffset = _visualOffset.Lerp (target, lerp); // Smooth
  }
}
