using System;
using System.Linq;
using Godot;

namespace com.forerunnergames.coa2.ui.player;

public partial class PlayerSprite : Sprite2D
{
  [Export] public Color HoveredColor { get; set; } = Colors.Green;
  [Export] public AnimationPlayer PrimaryAnimationPlayer { get; set; } = null!;
  [Export] public bool Equipable { get; set; }
  private Image? _imageCopy;
  private Rect2 _worldRect;
  private Vector2I _frameSize;
  private Rect2I _currentFrameRegion;
  private Vector2I _previousFrameCoords = new(-1, -1);
  private bool _isAnimating;
  private bool _isHovered;
  private bool _isInitialized;
  private void OnPrimaryAnimationStarted (StringName animName) => _isAnimating = IsAnimating (GetAnimation (animName));
  private void OnPrimaryAnimationFinished (StringName animName) => _isAnimating = IsAnimating (GetCurrentAnimation());
  private bool IsAnimating (Animation? animation) => animation != null && Enumerable.Range (0, animation.GetTrackCount()).Any (i => animation.TrackGetPath (i).ToString().Contains (Name));
  private Animation? GetCurrentAnimation() => GetAnimation (PrimaryAnimationPlayer.CurrentAnimation);
  private Animation? GetAnimation (string? name) => string.IsNullOrEmpty (name) ? null : PrimaryAnimationPlayer.GetAnimation (name);
  private Vector2I GetFrameSize() => new(Texture.GetWidth() / Hframes, Texture.GetHeight() / Vframes);
  private Rect2I GetCurrentSpriteFrameRegion() => new(new Vector2I (FrameCoords.X * _frameSize.X, FrameCoords.Y * _frameSize.Y), _frameSize);
  private Rect2 GetWorldRect (Vector2 globalSize) => new(GlobalPosition - globalSize / 2.0f, globalSize);
  private Vector2 GetGlobalSize() => GetRect().Size * GlobalScale;
  private Vector2 GetWorldMousePosition() => GetGlobalMousePosition();
  private Color? GetCurrentSpriteFrameRegionImagePixelColor (int x, int y) => _imageCopy?.GetPixel (x + _currentFrameRegion.Position.X, y + _currentFrameRegion.Position.Y);
  private Image? GetImageCopy() => Texture.GetImage();

  // ReSharper disable once AsyncVoidMethod
  public override async void _Ready()
  {
    await ToSignal (GetTree(), SceneTree.SignalName.ProcessFrame); // Allow texture to fully initialize.
    Initialize();
  }

  public override void _ExitTree()
  {
    PrimaryAnimationPlayer.AnimationStarted -= OnPrimaryAnimationStarted;
    PrimaryAnimationPlayer.AnimationFinished -= OnPrimaryAnimationFinished;
  }

  public override void _Input (InputEvent @event)
  {
    if (!Equipable || !_isInitialized || !_isAnimating || !_isHovered) return;
    if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }) return;
    Visible = !Visible;
  }

  public override void _Process (double delta)
  {
    if (!Equipable || !_isInitialized || !_isAnimating) return;
    UpdateCurrentFrame();
    var worldMousePosition = GetWorldMousePosition();
    if (!CheckIsHovering (worldMousePosition)) return;
    var hoveredPixelColor = GetHoveredPixelColor (worldMousePosition);
    SetHovered (hoveredPixelColor?.A >= 0.01f);
  }

  private Color? GetHoveredPixelColor (Vector2 worldMousePosition)
  {
    var worldSpritePosition = _worldRect.Position;
    var localPos = (worldMousePosition - worldSpritePosition) / GlobalScale;
    return GetCurrentSpriteFrameRegionImagePixelColor ((int)localPos.X, (int)localPos.Y);
  }

  private bool CheckIsHovering (Vector2 worldMousePosition)
  {
    if (_worldRect.HasPoint (worldMousePosition)) return true;
    SetHovered (false);
    return false;
  }

  private void UpdateCurrentFrame()
  {
    if (_previousFrameCoords == FrameCoords) return;
    _previousFrameCoords = FrameCoords;
    _currentFrameRegion = GetCurrentSpriteFrameRegion();
  }

  private void SetHovered (bool isHovered)
  {
    _isHovered = isHovered;
    Modulate = isHovered ? HoveredColor : Colors.White;
  }

  private void Initialize()
  {
    if (Texture == null) throw new InvalidOperationException ($"{Name}: Missing texture");
    _imageCopy = GetImageCopy();
    if (_imageCopy == null) throw new InvalidOperationException ($"{Name}: Invalid texture");
    _worldRect = GetWorldRect (GetGlobalSize());
    _frameSize = GetFrameSize();
    _isAnimating = IsAnimating (GetCurrentAnimation());
    UpdateCurrentFrame();
    PrimaryAnimationPlayer.AnimationStarted += OnPrimaryAnimationStarted;
    PrimaryAnimationPlayer.AnimationFinished += OnPrimaryAnimationFinished;
    _isInitialized = true;
  }
}
