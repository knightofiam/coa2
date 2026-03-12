using com.forerunnergames.coa2.core.game;
using com.forerunnergames.coa2.core.settings;

namespace com.forerunnergames.coa2.core.data;

public static class GameData
{
  public static readonly float Gravity = 0.0f;//ProjectSettings.GetSetting ("physics/2d/default_gravity").AsSingle();
  public static Game Game => _game ??= new Game();
  public static GameSettings GameSettingsSnapshot => Settings.GameSettingsSnapshot;
  private static Game? _game;
}
