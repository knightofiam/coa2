using com.forerunnergames.coa.game;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa.player;

public partial class PlayerHand : RigidBody2D
{
  [Export] public float HorizontalClimbSpeed = 30.0f;
  [Export] public float VerticalClimbAcceleration = 1200.0f;
  [Export] public float VerticalClimbMaxSpeed = 50.0f;
  [Export] public float Acceleration = 2000.0f;
  [Export] public float JumpVelocity = -400.0f;
  public bool IsGrabbing { get; private set; }
  public bool IsBodyOnFloor { get; set; }
  public bool IsIceTimerStopped { get; set; }
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private Game _game = null!;
  private StaticBody2D? _worldGrabAnchor;
  private PinJoint2D? _worldGrabJoint;
  private int _iceCollisions;
  public override void _Ready() => _game = GetNode <Game> ("/root/Game");

  public void GrabAt (Vector2 worldPoint)
  {
    ReleaseGrab();
    GlobalPosition = worldPoint;
    LinearVelocity = Vector2.Zero;
    AngularVelocity = 0.0f;
    _worldGrabAnchor = new StaticBody2D { TopLevel = true };
    GetTree().CurrentScene.AddChild (_worldGrabAnchor);
    _worldGrabAnchor.GlobalPosition = worldPoint;
    _worldGrabJoint = new PinJoint2D { TopLevel = true };
    GetTree().CurrentScene.AddChild (_worldGrabJoint);
    _worldGrabJoint.NodeA = GetPath(); // Pin this hand to the static world anchor.
    _worldGrabJoint.NodeB = _worldGrabAnchor.GetPath(); // The static world anchor (similar to a cliff handhold).
    _worldGrabJoint.DisableCollision = true;
    _worldGrabJoint.GlobalPosition = worldPoint;
    IsGrabbing = true;
    CanSleep = false;
    Log.Trace ("Grabbed [{worldPoint}]", worldPoint);
  }

  public void ReleaseGrab()
  {
    if (_worldGrabJoint != null && IsInstanceValid (_worldGrabJoint)) _worldGrabJoint.QueueFree();
    if (_worldGrabAnchor != null && IsInstanceValid (_worldGrabAnchor)) _worldGrabAnchor.QueueFree();
    _worldGrabJoint = null;
    _worldGrabAnchor = null;
    IsGrabbing = false;
    CanSleep = true;
    Log.Trace ("Released grab at [{globalPosition}]", GlobalPosition);
  }
}
