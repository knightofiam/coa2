using com.forerunnergames.coa2.core.game;
using com.forerunnergames.coa2.core.settings;
using Godot;

namespace com.forerunnergames.coa2.core.data;

public static class GameData
{
  public static readonly float Gravity = ProjectSettings.GetSetting ("physics/2d/default_gravity").AsSingle() * 2.0f;
  public static Game Game => _game ??= new Game();
  public static GameSettings GameSettingsSnapshot => Settings.GameSettingsSnapshot;
  private static Game? _game;
}
