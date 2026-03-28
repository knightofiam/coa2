using System;
using System.Linq;
using System.Runtime.InteropServices;
using com.forerunnergames.coa2.tools.events;
using com.forerunnergames.coa2.tools.events.args;
using com.forerunnergames.coa2.ui.data;
using Godot;
using Godot.Collections;
using NLog;
using Logger = NLog.Logger;
using Strings = com.forerunnergames.coa2.tools.Strings;

namespace com.forerunnergames.coa2.ui.player;

public partial class PlayerSprite : Sprite2D
{
  [Signal] public delegate void HoveredEventHandler (PlayerSprite sprite);
  [Signal] public delegate void UnhoveredEventHandler (PlayerSprite sprite);
  [Export] public AnimationPlayer PrimaryAnimationPlayer { get; set; } = null!;
  [Export] public bool Equipable { get; set; }
  [Export] public Array <string> DependsOnSprites = [];
  [Export] public Array <string> OppositeSprites = [];
  public string ShortName => Name.ToString().Replace ("-", " ");
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private Image? _imageCopy;
  private Rect2 _worldRect;
  private Vector2I _frameSize;
  private Vector2 _globalSize;
  private Rect2I _currentFrameRegion;
  private Vector2I _previousFrameCoords = new(-1, -1);
  private bool _isAnimating;
  private bool _isHovered;
  private bool _isInitialized;
  private Texture2D? _originalTexture;
  private void OnPrimaryAnimationFinished (StringName animName) => _isAnimating = IsAnimating (GetCurrentAnimation());
  private bool IsAnimating (Animation? animation) => animation != null && Enumerable.Range (0, animation.GetTrackCount()).Any (i => animation.TrackGetPath (i).ToString().Contains (Name));
  private string GetCurrentAnimationName() => PrimaryAnimationPlayer.CurrentAnimation;
  private Animation? GetCurrentAnimation() => GetAnimation (GetCurrentAnimationName());
  private Animation? GetAnimation (string? name) => string.IsNullOrEmpty (name) ? null : PrimaryAnimationPlayer.GetAnimation (name);
  private Vector2I GetFrameSize() => new(Texture.GetWidth() / Hframes, Texture.GetHeight() / Vframes);
  private Rect2I GetCurrentSpriteFrameRegion() => new(new Vector2I (FrameCoords.X * _frameSize.X, FrameCoords.Y * _frameSize.Y), _frameSize);
  private Rect2 GetWorldRect() => new(GlobalPosition - _globalSize / 2.0f, _globalSize);
  private Vector2 GetGlobalSize() => GetRect().Size * GlobalScale;
  private Vector2 GetWorldMousePosition() => GetGlobalMousePosition() - new Vector2 (1, 3);
  private Color? GetCurrentSpriteFrameRegionImagePixelColor (int x, int y) => _imageCopy?.GetPixel (x + _currentFrameRegion.Position.X, y + _currentFrameRegion.Position.Y);
  private Image? GetImageCopy() => Texture.GetImage();

  // ReSharper disable once AsyncVoidMethod
  public override async void _Ready()
  {
    EventBus.Instance.PrimaryAnimationStartedEvent += OnPrimaryAnimationStartedEvent;
    EventBus.Instance.PrimaryAnimationEndedEvent += OnPrimaryAnimationEndedEvent;

    await ToSignal (GetTree(), SceneTree.SignalName.ProcessFrame); // Allow texture to fully initialize.
    Initialize();
  }

  public override void _ExitTree()
  {
    EventBus.Instance.PrimaryAnimationStartedEvent -= OnPrimaryAnimationStartedEvent;
    EventBus.Instance.PrimaryAnimationEndedEvent -= OnPrimaryAnimationEndedEvent;
  }

  public override void _Input (InputEvent @event)
  {
    if (!Equipable || !_isInitialized || !_isAnimating || !_isHovered) return;
    if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }) return;
    Visible = !Visible;
    UpdateAnimation (GetCurrentAnimationName());
    Log.Trace ($"Clicked: Animating {Name}: {_isAnimating}, ZIndex: {ZIndex}, DependsOnSprites [{Strings.ToString ((object)DependsOnSprites)}], OppositeSprites [{Strings.ToString ((object)OppositeSprites)}]");
  }

  public override void _Process (double delta)
  {
    if (!Equipable || !_isInitialized || !_isAnimating) return;
    _worldRect = GetWorldRect();
    UpdateCurrentFrame();
    var worldMousePosition = GetWorldMousePosition();
    if (!CheckIsHovering (worldMousePosition)) return;
    var hoveredPixelColor = GetHoveredPixelColor (worldMousePosition);
    SetHovered (hoveredPixelColor?.A >= 0.01f);
  }

  private void OnPrimaryAnimationStarted (StringName animName)
  {
    UpdateAnimation (animName);
    Log.Trace ($"OnPrimaryAnimationStarted: {animName}, animating {Name}: {_isAnimating}, ZIndex: {ZIndex}, DependsOnSprites [{Strings.ToString ((object)DependsOnSprites)}], OppositeSprites [{Strings.ToString ((object)OppositeSprites)}]");
  }

  private void UpdateAnimation (string animation)
  {
    if (animation == "") return;
    _isAnimating = IsAnimating (GetAnimation (animation));
    if (!_isAnimating) Visible = false;
    ZIndex = PlayerData.AnimationsToSpriteNamesToZIndices.TryGetValue (animation, out var spriteNamesToZIndices) && spriteNamesToZIndices.TryGetValue (Name, out var zIndex) ? zIndex : ZIndex;
    DependsOnSprites.Select (dos => GetNodeOrNull <PlayerSprite> ($"../{dos}")).OfType <PlayerSprite>().ToList().ForEach (dos => dos.Visible = Visible);
    OppositeSprites.Select (os => GetNodeOrNull <PlayerSprite> ($"../{os}")).OfType <PlayerSprite>().ToList().ForEach (os => os.Visible = !Visible);
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
    if (isHovered == _isHovered) return;
    _isHovered = isHovered;
    if (CheckUnhovered()) return;
    var dataCopy = _imageCopy?.GetData();
    Validate (_imageCopy, dataCopy);
    Hover (_imageCopy!, dataCopy!);
  }

  private bool CheckUnhovered()
  {
    if (_isHovered) return false;
    if (_originalTexture == null) return true;
    Unhover();
    return true;
  }

  private void Unhover()
  {
    Texture = _originalTexture;
    _originalTexture = null;
    _imageCopy = GetImageCopy();
    EmitSignal (SignalName.Unhovered, this);
  }

  private void Hover (Image validatedImage, byte[] validatedImageData)
  {
    Highlight (validatedImageData);
    var texture = new ImageTexture();
    validatedImage.SetData (validatedImage.GetWidth(), validatedImage.GetHeight(), validatedImage.HasMipmaps(), validatedImage.GetFormat(), validatedImageData);
    texture.Image = validatedImage;
    _originalTexture ??= Texture;
    Texture = texture;
    EmitSignal (SignalName.Hovered, this);
  }

  private void Initialize()
  {
    if (Texture == null) throw new InvalidOperationException ($"{Name}: Missing texture");
    _imageCopy = GetImageCopy();
    if (_imageCopy == null) throw new InvalidOperationException ($"{Name}: Invalid texture");
    _globalSize = GetGlobalSize();
    _worldRect = GetWorldRect();
    _frameSize = GetFrameSize();
    _isAnimating = IsAnimating (GetCurrentAnimation());
    UpdateCurrentFrame();
    _isInitialized = true;
  }

  private void OnPrimaryAnimationStartedEvent (object? sender, PrimaryAnimationStartedEventArgs e)
  {
    UpdateAnimation (e.AnimationName);
    Log.Trace ($"OnPrimaryAnimationStartedEvent: {e.AnimationName}, is looping: {e.IsLooping}, animating {Name}: {_isAnimating}, ZIndex: {ZIndex}, DependsOnSprites [{Strings.ToString ((object)DependsOnSprites)}], OppositeSprites [{Strings.ToString ((object)OppositeSprites)}]");
  }

  private void OnPrimaryAnimationEndedEvent (object? sender, PrimaryAnimationEndedEventArgs e)
  {
    _isAnimating = IsAnimating (GetCurrentAnimation());
    Log.Trace ($"OnPrimaryAnimationEndedEvent: {e.AnimationName}, was looping: {e.WasLooping}, animating {Name}: {_isAnimating}, ZIndex: {ZIndex}, DependsOnSprites [{Strings.ToString ((object)DependsOnSprites)}], OppositeSprites [{Strings.ToString ((object)OppositeSprites)}]");
  }

  private static void Highlight (byte[] imageData, float blend = 0.6f)
  {
    var pixels = MemoryMarshal.Cast <byte, uint> (imageData);

    foreach (ref var pixel in pixels)
    {
      var a = pixel >> 24;
      if (a == 0) continue;
      var r = (pixel >> 16) & 0xFF;
      var g = (pixel >> 8) & 0xFF;
      var b = pixel & 0xFF;
      r = (uint)(r + (255 - r) * blend);
      g = (uint)(g + (255 - g) * blend);
      b = (uint)(b + (255 - b) * blend);
      pixel = (a << 24) | (r << 16) | (g << 8) | b;
    }
  }

  private static void Validate (Image? image, byte[]? imageData)
  {
    if (image == null) throw new InvalidOperationException ("Image is null");
    if (imageData == null) throw new InvalidOperationException ("Image data is null");
    if (image.GetFormat() is not Image.Format.Rgba8) throw new InvalidOperationException ("Image is not RGBA8");
  }
}
