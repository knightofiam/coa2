using com.forerunnergames.coa2.utilities;
using Godot;

namespace com.forerunnergames.coa2.core;

public partial class World : Node2D
{
  private TileMapLayer _rocksBg = null!;

  public override void _Ready()
  {
    _rocksBg = GetNode <TileMapLayer> ("%RocksBg");
  }

  public (Vector2I mapCoords, string terrain) GetTileAtWorldPosition (Vector2 worldPosition)
  {
    var localPosition = _rocksBg.ToLocal (worldPosition);
    return Tools.GetTileAt (localPosition, _rocksBg);
  }

  public (Vector2I mapCoords, string terrain) GetTileAtLocalMousePosition (Vector2 localMousePosition)
  {
    var worldPosition = ToGlobal (localMousePosition);
    return GetTileAtWorldPosition (worldPosition);
  }

  public void ClearTile (Vector2I mapCoords) => _rocksBg.SetCell (mapCoords);
}
