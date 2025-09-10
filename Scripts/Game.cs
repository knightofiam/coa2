using com.forerunnergames.coa.player;
using com.forerunnergames.coa.ui;
using com.forerunnergames.coa.utilities;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa.game;

// Autoload
public partial class Game : Node2D
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private TileMapLayer _rocksBg = null!;
  private Player _player = null!;
  private UI _ui = null!;
  public void SetDebugText (string text) => _ui.SetDebugText (text);

  public override void _Ready()
  {
    _rocksBg = GetNode <TileMapLayer> ("%RocksBg");
    _player = GetNode <Player> ("Player");
    _ui = GetNode <UI> ("UI");
  }

  public override void _Input (InputEvent @event)
  {
    var (mapCoords, terrain) = Tools.GetTileAt (ToLocal (_player.GlobalPosition), _rocksBg);
    var (mapCoords2, terrain2) = Tools.GetTileAt (GetLocalMousePosition(), _rocksBg);
    SetDebugText ($"Mouse hovering Tile: {mapCoords2} ({terrain2}), Player: {mapCoords} ({terrain}), Local Mouse Coords: {GetLocalMousePosition()}");
    if (!Input.IsActionJustReleased ("click")) return;
    Log.Debug ("Player center is at: {mapCoords} ({terrain})", mapCoords, terrain);
    Log.Debug ("Clicked {mapCoords2} ({terrain2})", mapCoords2, terrain2);
    _rocksBg.SetCell (mapCoords2); // TODO Testing tile mouse click detection.
    SetDebugText ($"Mouse clicked Tile: {mapCoords2} ({terrain2}), Player: {mapCoords} ({terrain}), Local Mouse Coords: {GetLocalMousePosition()}");
  }
}
