using Godot;

namespace com.forerunnergames.coa2.ui.background;

public partial class BackgroundUI : CanvasLayer
{
  private ColorRect _colorRect = null!;
  public override void _Ready() => _colorRect = GetNode <ColorRect> ("%ColorRect");
}
