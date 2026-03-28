using System.Collections.Generic;
using System.Linq;
using com.forerunnergames.coa2.tools;
using com.forerunnergames.coa2.tools.events;
using com.forerunnergames.coa2.tools.events.args;
using com.forerunnergames.coa2.utilities;
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
  private List <PlayerSprite> _sprites = null!;
  private readonly List <PlayerSprite> _hoveredSprites = [];
  private bool _wasOnFloor;
  public override void _ExitTree() => EventBus.Instance.PrimaryAnimationEndedEvent -= OnPrimaryAnimationEndedEvent;
  public void ResetVisualOffset() => _visualOffset = Vector2.Zero;
  public bool HasLocalPoint (Vector2 localPoint) => _sprites.Where (s => s.Visible).Any (s => s.GetRect().HasPoint (localPoint));
  private static void OnPrimaryAnimationEndedEvent (object? sender, PrimaryAnimationEndedEventArgs e) => Log.Info ("Primary animation ended: {animationName}, was looping: {isLooping}", e.AnimationName, e.WasLooping);

  public override void _Ready()
  {
    _ui = GetNode <UI> ("/root/UI");
    _primaryPlayer = GetNode <AnimationPlayer> ("%Primary");
    _secondaryPlayer = GetNode <AnimationPlayer> ("%Secondary");
    _sprites = GetNode <Node2D> ("%Sprites").GetChildren().ToList().OfType <PlayerSprite>().ToList();
    _sprites.ForEach (s => s.Hovered += OnPlayerSpriteHovered);
    _sprites.ForEach (s => s.Unhovered += OnPlayerSpriteUnhovered);
    EventBus.Instance.PrimaryAnimationEndedEvent += OnPrimaryAnimationEndedEvent;
    // _primaryPlayer.Play (IdleLeftAnimation); // TODO Restore
    _primaryPlayer.Play (CliffHangingAnimation); // TODO Testing, remove
  }

  // TODO Implement clicking on player to equip/unequip
  public bool HandleInput (InputEvent @event)
  {
    // if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left } click) return false;
    // if (@event is not InputEventMouseMotion click) return false;
    // if (!HasLocalPoint (ToLocal (click.GlobalPosition))) return false;
    // _ui.SetDebugText ($"Clicked player at global position: {click.GlobalPosition}, local position: {ToLocal (click.Position)}");
    return true;
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
    // var hasAnimation = _primaryPlayer.CurrentAnimation != "" && _primaryPlayer.IsPlaying();
    var animationName = isWalking ? WalkLeftAnimation : isRunning ? RunLeftAnimation : IdleLeftAnimation;
    var facingRight = Mathf.Sign (hDirection) > 0;
    var animationSpeed = isWalking ? WalkAnimationSpeed : isRunning ? RunAnimationSpeed : 1.0f;
    var movementSpeed = isWalking ? WalkSpeed : isRunning ? RunSpeed : 1.0f;
    var speedScale = movementSpeed / animationSpeed;
    Scale = new Vector2 (facingRight ? -8.0f : 8.0f, 8.0f);
    if (_primaryPlayer.CurrentAnimation == animationName && _primaryPlayer.IsPlaying()) return;
    var previousAnimationName = _primaryPlayer.CurrentAnimation;
    EventBus.EmitIf (previousAnimationName != "", new PrimaryAnimationEndedEventArgs (previousAnimationName, wasLooping: Tools.IsAnimationLooping (previousAnimationName, _primaryPlayer)));
    _primaryPlayer.Play (animationName, customSpeed: speedScale);
    // await ToSignal (GetTree(), SceneTree.SignalName.ProcessFrame);
    var isPlaying = _primaryPlayer.CurrentAnimation == animationName && _primaryPlayer.IsPlaying();
    GD.Print ("isPlaying: " + isPlaying);
    if (!isPlaying) return;
    EventBus.Emit (new PrimaryAnimationStartedEventArgs (animationName, Tools.IsAnimationLooping (animationName, _primaryPlayer)));
    Log.Info ("Playing animation {animationName} at {speedScale:F1} speed, looping: {isLooping}", animationName, speedScale, Tools.IsAnimationLooping (animationName, _primaryPlayer));
  }

  private void OnPlayerSpriteHovered (PlayerSprite sprite)
  {
    if (!_hoveredSprites.Contains (sprite)) _hoveredSprites.Add (sprite);
    UpdateSpriteHoverText();
  }

  private void UpdateSpriteHoverText()
  {
    var visibleSprites = _hoveredSprites.Where (s => s.Visible).ToList();
    var debugTextSprites = visibleSprites.Count > 0 ? visibleSprites : _hoveredSprites;

    if (debugTextSprites.Count == 0)
    {
      _ui.SetDebugText ("");
      return;
    }

    _ui.SetDebugText ($"Hovered {Strings.ToString (debugTextSprites, f: s => s.ShortName)}");
  }

  private void OnPlayerSpriteUnhovered (PlayerSprite sprite)
  {
    _hoveredSprites.Remove (sprite);
    UpdateSpriteHoverText();
  }
}
