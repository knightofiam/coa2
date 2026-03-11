using Godot;
using System.Collections.Generic;
using System.Linq;

namespace com.forerunnergames.coa2.core.butterflies;

public partial class Butterfly : Node2D
{
  // @formatter:off
  [ExportGroup ("Movement")]
  [Export] public float FlyingSpeed { get; set; } = 300.0f;
  [Export] public float EvadingSpeed { get; set; } = 400.0f;
  [Export] public float PerchingSpeed { get; set; } = 200.0f;
  [ExportGroup ("Flight Pattern")]
  [Export] public float OscillationWidth { get; set; } = 5.0f;
  [Export] public float OscillationFrequency { get; set; } = 3.0f;
  [ExportGroup ("Idle Timing")]
  [Export] public float MinIdleTimeSeconds { get; set; } = 1.0f;
  [Export] public float MaxIdleTimeSeconds { get; set; } = 1.0f;
  [ExportGroup ("Perch Selection")]
  [Export] public float NearestPerchFrequency { get; set; } = 0.8f; // 80% chance to pick nearest
  [Export] public float RoamingRadius { get; set; } = 500.0f; // Max distance to search for perches
  private ButterflyState _currentState = ButterflyState.Idle;
  private ButterflyAnimator _animator = null!;
  private Area2D _perchDetector = null!;
  private Area2D _threatDetector = null!;
  private Timer _idleTimer = null!;
  private Vector2 _targetPosition;
  private Vector2 _flightStartPosition;
  private float _flightProgress;
  private float _oscillationOffset;
  private readonly List <Marker2D> _availablePerches = [];
  private readonly List <Marker2D> _visitedPerches = [];
  private Marker2D? _currentPerch;
  private Vector2 _homePosition;
  private void OnIdleTimerTimeout() => EnterFlyingState(); // Time to find a new perch
  private void OnThreatAreaDetected (Area2D area) { if (area.IsInGroup ("butterfly_threat") && _currentState != ButterflyState.Evading) EnterEvadingState (area.GlobalPosition); } // Evade from any area marked as threat
  private void ProcessEvadingState (double delta) => MoveTowardTargetWithOscillation (EvadingSpeed, delta, EnterFlyingState);
  private void ProcessFlyingState (double delta) => MoveTowardTargetWithOscillation (FlyingSpeed, delta, EnterPerchingState);
  private void UpdateFlightProgress (float speed, double delta) => _flightProgress += speed * (float)delta / _flightStartPosition.DistanceTo (_targetPosition);
  private Marker2D GetNearestPerch() => _availablePerches.OrderBy (p => GlobalPosition.DistanceSquaredTo (p.GlobalPosition)).First();
  private Marker2D GetRandomPerch() => _availablePerches [GD.RandRange (0, _availablePerches.Count - 1)];
  private bool HasReachedTarget (float threshold) => GlobalPosition.DistanceTo (_targetPosition) < threshold;
  private bool ShouldSelectNearestPerch() => GD.Randf() < NearestPerchFrequency;
  private bool IsPerchTooFarFromHome (Marker2D marker) => _homePosition.DistanceTo (marker.GlobalPosition) > RoamingRadius;
  private bool HasAlreadyVisitedPerch (Marker2D marker) => _visitedPerches.Contains (marker);
  private bool IsValidUnvisitedPerch (Marker2D marker) => !IsPerchTooFarFromHome (marker) && !HasAlreadyVisitedPerch (marker);
  private bool HasReachedTargetAndComplete (float threshold, System.Action onReachTarget) { if (!HasReachedTarget (threshold)) return false; onReachTarget(); return true; }
  private bool HasCompletedFlightAndFinish (System.Action onReachTarget) { if (_flightProgress < 1.0f) return false; onReachTarget(); return true; }
  private bool CheckNoPerchesAndReturnToIdle (Marker2D? perch) { if (perch != null) return false; EnterIdleState(); return true; }
  // @formatter:on

  public override void _Ready()
  {
    _animator = GetNode <ButterflyAnimator> ("%ButterflyAnimator");
    _perchDetector = GetNode <Area2D> ("%PerchDetector");
    _threatDetector = GetNode <Area2D> ("%ThreatDetector");
    _idleTimer = GetNode <Timer> ("%IdleTimer");
    _homePosition = GlobalPosition;
    _idleTimer.Timeout += OnIdleTimerTimeout;
    _threatDetector.BodyEntered += OnThreatDetected;
    _threatDetector.AreaEntered += OnThreatAreaDetected;
    EnterIdleState();
  }

  public override void _Process (double delta)
  {
    switch (_currentState)
    {
      case ButterflyState.Idle: break;
      case ButterflyState.Flying: ProcessFlyingState (delta); break;
      case ButterflyState.Evading: ProcessEvadingState (delta); break;
      case ButterflyState.Perching: ProcessPerchingState (delta); break;
    }
  }

  private void OnThreatDetected (Node2D body)
  {
    if (!body.HasMeta ("is_player")) return; // Check if it's the player and if the player is moving
    var velocity = body.Get ("Velocity").AsVector2();
    if (!(velocity.LengthSquared() > 1.0f)) return; // Player is moving
    if (_currentState == ButterflyState.Evading) return;
    EnterEvadingState (body.GlobalPosition);
  }

  private void ProcessPerchingState (double delta)
  {
    if (HasReachedTargetAndComplete (0.5f, SnapToTargetAndEnterIdle)) return;
    UpdateFlightProgress (PerchingSpeed, delta);
    if (HasCompletedFlightAndFinish (SnapToTargetAndEnterIdle)) return;
    GlobalPosition = _flightStartPosition.Lerp (_targetPosition, _flightProgress);
  }

  private void EnterIdleState()
  {
    _currentState = ButterflyState.Idle;
    _animator.PlayIdle();
    _idleTimer.WaitTime = (float)GD.RandRange (MinIdleTimeSeconds, MaxIdleTimeSeconds);
    _idleTimer.Start();
  }

  private void EnterFlyingState()
  {
    _currentState = ButterflyState.Flying;
    _animator.PlayFlying();
    var nextPerch = SelectNextPerch();
    if (CheckNoPerchesAndReturnToIdle (nextPerch)) return;
    _targetPosition = nextPerch!.GlobalPosition;
    _flightStartPosition = GlobalPosition;
    _flightProgress = 0.0f;
    _oscillationOffset = (float)GD.RandRange (0.0f, Mathf.Tau);
  }

  private void EnterEvadingState (Vector2 threatPosition)
  {
    _currentState = ButterflyState.Evading;
    _animator.PlayEvading();
    var fleeDirection = (GlobalPosition - threatPosition).Normalized(); // Flee away from threat
    _targetPosition = GlobalPosition + fleeDirection * 200.0f; // Flee 200 units away
    _flightStartPosition = GlobalPosition;
    _flightProgress = 0.0f;
    _oscillationOffset = (float)GD.RandRange (0.0f, Mathf.Tau);
  }

  private void EnterPerchingState()
  {
    _currentState = ButterflyState.Perching;
    _animator.PlayPerching();
    _flightStartPosition = GlobalPosition; // Slow approach to exact perch position
    _flightProgress = 0.0f;
  }

  private void MoveTowardTargetWithOscillation (float speed, double delta, System.Action onReachTarget)
  {
    if (HasReachedTargetAndComplete (0.1f, onReachTarget)) return;
    UpdateFlightProgress (speed, delta);
    if (HasCompletedFlightAndFinish (onReachTarget)) return;
    GlobalPosition = CalculateOscillatingPosition();
  }

  private Marker2D? SelectNextPerch()
  {
    UpdateAvailablePerches();
    if (!TryResetPerchesIfAllVisited()) return null;
    var selectedPerch = ShouldSelectNearestPerch() ? GetNearestPerch() : GetRandomPerch();
    MarkPerchAsVisited (selectedPerch);
    return selectedPerch;
  }

  private void SnapToTargetAndEnterIdle()
  {
    GlobalPosition = _targetPosition;
    EnterIdleState();
  }

  private void UpdateAvailablePerches()
  {
    _availablePerches.Clear();
    _availablePerches.AddRange (GetTree().GetNodesInGroup ("butterfly_perch").OfType <Marker2D>().Where (IsValidUnvisitedPerch));
  }

  private bool TryResetPerchesIfAllVisited()
  {
    if (_availablePerches.Count > 0) return true;
    _visitedPerches.Clear();
    UpdateAvailablePerches();
    return _availablePerches.Count > 0;
  }

  private void MarkPerchAsVisited (Marker2D perch)
  {
    _visitedPerches.Add (perch);
    _currentPerch = perch;
  }

  private Vector2 CalculateOscillatingPosition()
  {
    var straightPath = _flightStartPosition.Lerp (_targetPosition, _flightProgress);
    var direction = (_targetPosition - _flightStartPosition).Normalized();
    var perpendicular = new Vector2 (-direction.Y, direction.X);
    var oscillation = Mathf.Sin (_flightProgress * Mathf.Pi * OscillationFrequency + _oscillationOffset) * OscillationWidth;
    return straightPath + perpendicular * oscillation;
  }
}
