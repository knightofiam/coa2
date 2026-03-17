using Godot;

public partial class PlayerSprite : Sprite2D
{
  private Image _image = null!;
  private Vector2 _globalSize;
  private Rect2 _worldSpriteRect;
  private Rect2I _currentFrameRegion;
  public void OnFrameChanged() => _currentFrameRegion = GetCurrentSpriteFrameRegion();
  private Color GetCurrentSpriteFrameImagePixelColor (Vector2I pixelPos) => _image.GetPixelv (pixelPos + _currentFrameRegion.Position);

  public override void _Ready()
  {
    _image = Texture.GetImage();
    _globalSize = GetRect().Size * GlobalScale;
    _worldSpriteRect = new Rect2 (GlobalPosition, _globalSize);
    _currentFrameRegion = GetCurrentSpriteFrameRegion();
  }

  public override void _Input (InputEvent @event)
  {
    if (Modulate != Colors.Green) return;
    if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } click) return;
    Visible = !Visible;
  }

  public override void _Process (double delta)
  {
    var worldMousePos = GetGlobalMousePosition() + _globalSize / 2.0f;

    if (!_worldSpriteRect.HasPoint (worldMousePos))
    {
      Modulate = Colors.White;
      GD.Print ("Did not hover player sprite");
      return;
    }

    var spriteTopLeft = _worldSpriteRect.Position;
    var localPos = (worldMousePos - spriteTopLeft) / GlobalScale;
    var pixelPos = new Vector2I ((int)localPos.X, (int)localPos.Y);
    var hoveredColor = GetCurrentSpriteFrameImagePixelColor (pixelPos);

    if (Mathf.IsZeroApprox (hoveredColor.A))
    {
      Modulate = Colors.White;
      return;
    }

    GD.Print ($"Hovered player sprite, pixel color: {hoveredColor}", hoveredColor);
    Modulate = Colors.Green;
  }

  private Rect2I GetCurrentSpriteFrameRegion()
  {
    var texture = Texture;
    var frameCoords = FrameCoords;
    var frameSize = new Vector2I (texture.GetWidth() / Hframes, texture.GetHeight() / Vframes);
    return new Rect2I (new Vector2I (frameCoords.X * frameSize.X, frameCoords.Y * frameSize.Y), frameSize);
  }
}
