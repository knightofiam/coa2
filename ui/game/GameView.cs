using com.forerunnergames.coa2.core.player;
using com.forerunnergames.coa2.ui.player;
using Godot;
using com.forerunnergames.coa2.ui.world;

public partial class GameView : CanvasLayer
{
  [Export] public Vector2 PlayerSpawnPosition = new(0.0f, -250.0f);
  private WorldView _worldView = null!;
  private PlayerView _playerView = null!;
  private Player _player = null!;

  public override void _Ready()
  {
    _worldView = GetNode <WorldView> ("%WorldView");
    _playerView = GetNode <PlayerView> ("%PlayerView");
    _player = GetNode <Player> ("%Player");
    _player.SpawnPosition = PlayerSpawnPosition;
    _player.View = _playerView;
    _playerView.FollowTarget = _player;
  }

  public void HandleInput (InputEvent @event)
  {
    _worldView.HandleInput (@event, playerWorldPosition: _player.GlobalPosition);
    _playerView.HandleInput (@event);
    _player.HandleInput (@event);
  }
}
