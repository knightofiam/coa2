using Godot;

namespace com.forerunnergames.coa.player;

public partial class PlayerBodyAnchor : RigidBody2D
{
  [Export] public CharacterBody2D Character = null!;
  [Export] public PlayerHand LeftHand = null!;
  [Export] public PlayerHand RightHand = null!;
  [Export] public PlayerAnimator Animator = null!;
  public bool IsFollowing = true; // True: Follows CharacterBody2D every frame, otherwise acts as normal dynamic rigid body the joints can pull.
  private const float HandHeightDeadZone = 3.0f; // Pixels; avoid flicker
  private PinJoint2D _leftHandPinJoint = null!;
  private PinJoint2D _rightHandPinJoint = null!;
  private Vector2 _previousLinearVelocity = Vector2.Zero;

  public override void _Ready()
  {
    _leftHandPinJoint = GetNode <PinJoint2D> ("LeftHandPinJoint");
    _rightHandPinJoint = GetNode <PinJoint2D> ("RightHandPinJoint");
    _leftHandPinJoint.NodeB = LeftHand.GetPath();
    _rightHandPinJoint.NodeB = RightHand.GetPath();
    TopLevel = true; // Never inherit any transforms.
    FreezeMode = FreezeModeEnum.Kinematic;
    SetFollowing (IsFollowing);
  }

  public override void _PhysicsProcess (double delta)
  {
    if (!CheckFollowing()) return; // Dynamic mode, do nothing.
    // Kinematic mirror of the character (anchor won’t be moved by solver).
    GlobalTransform = Character.GlobalTransform;
    LinearVelocity = Vector2.Zero;
    AngularVelocity = 0.0f;
    MirrorHandsToPinJoints();
    _previousLinearVelocity = LinearVelocity;
  }

  public override void _IntegrateForces (PhysicsDirectBodyState2D state)
  {
    if (!IsFollowing) return; // Dynamic mode, do nothing.
    // Kinematic mirror of the character (anchor won’t be moved by solver).
    state.Transform = Character.GlobalTransform;
    state.LinearVelocity = Vector2.Zero;
    state.AngularVelocity = 0.0f;
  }

  public void SetFollowing (bool isFollowing)
  {
    IsFollowing = isFollowing;
    Freeze = IsFollowing;
    LeftHand.Freeze = IsFollowing;
    RightHand.Freeze = IsFollowing;
    if (IsFollowing) return;
    LeftHand.CanSleep = false;
    LeftHand.Sleeping = false;
    RightHand.CanSleep = false;
    RightHand.Sleeping = false;
  }

  private void MirrorHandsToPinJoints()
  {
    // Pin pivots are the “sockets”.
    LeftHand.GlobalPosition = _leftHandPinJoint.GlobalPosition;
    RightHand.GlobalPosition = _rightHandPinJoint.GlobalPosition;
    // Keep them tame while following.
    LeftHand.LinearVelocity = Vector2.Zero;
    LeftHand.AngularVelocity = 0.0f;
    RightHand.LinearVelocity = Vector2.Zero;
    RightHand.AngularVelocity = 0.0f;
  }

  private bool CheckFollowing()
  {
    if (IsFollowing) return true; // Still kinematic mirror.
    AnimateClimbing(); // We’re in dynamic mode, pick a climb pose based on hand heights.
    return false;
  }

  private void AnimateClimbing()
  {
    var leftHandHeight = LeftHand.GlobalPosition.Y;
    var rightHandHeight = RightHand.GlobalPosition.Y;
    var isLeftHandHigher = leftHandHeight + HandHeightDeadZone < rightHandHeight;
    var isRightHandHigher = rightHandHeight + HandHeightDeadZone < leftHandHeight;
    var frameIndex = isLeftHandHigher ? 2 : isRightHandHigher ? 0 : 1;
    Animator.AlignSpriteToHands (LeftHand.GlobalPosition, RightHand.GlobalPosition);
    Animator.UpdateFromBodyAnchor (frameIndex);
  }
}
