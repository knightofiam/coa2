using com.forerunnergames.coa2.utilities;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.world;

public partial class WorldView : Node2D
{
  private UI _ui = null!;
  private TileMapLayer _rocksBg = null!;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  public void ClearTile (Vector2I mapCoords) => _rocksBg.SetCell (mapCoords);
  public (Vector2I mapCoords, string terrain) GetTileAtWorldPosition (Vector2 worldPosition) => Tools.GetTileAt (_rocksBg.ToLocal (worldPosition), _rocksBg);
  public (Vector2I mapCoords, string terrain) GetTileAtLocalMousePosition (Vector2 localMousePosition) => GetTileAtWorldPosition (ToGlobal (localMousePosition));

  public override void _Ready()
  {
    _ui = GetNode <UI> ("/root/UI");
    _rocksBg = GetNode <TileMapLayer> ("%RocksBg");
  }

  public void HandleInput (InputEvent @event, Vector2 playerWorldPosition)
  {
    var (mapCoords, terrain) = GetTileAtWorldPosition (playerWorldPosition);
    var (mapCoords2, terrain2) = GetTileAtLocalMousePosition (GetLocalMousePosition());
    // _ui.SetDebugText ($"Hovering Tile: {mapCoords2} ({terrain2})\nPlayer: {mapCoords} ({terrain})\nMouse Local: {GetLocalMousePosition()}\nMouse Global: {GetGlobalMousePosition()}\nPlayer World Position: {playerWorldPosition}");
    if (!Input.IsActionJustReleased ("click")) return;
    Log.Debug ("Player center is at: {mapCoords} ({terrain})", mapCoords, terrain);
    Log.Debug ("Clicked {mapCoords2} ({terrain2})", mapCoords2, terrain2);
    ClearTile (mapCoords2);
    // _ui.SetDebugText ($"Clicked Tile: {mapCoords2} ({terrain2})\nPlayer: {mapCoords} ({terrain})\nLocal Mouse: {GetLocalMousePosition()}");
  }
}
