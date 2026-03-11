using System.Collections.Generic;
using com.forerunnergames.coa2.ui.screens;

namespace com.forerunnergames.coa2.ui.data;

public static class ScreenData
{
  public static string? GetScenePath (ScreenId screenId) => ScreenIdsToScenePaths.GetValueOrDefault (screenId);

  private static readonly Dictionary <ScreenId, string> ScreenIdsToScenePaths = new()
  {
    { ScreenId.MainMenu, "res://ui/screens/mainmenu/MainMenuScreen.tscn" },
    { ScreenId.Game, "res://ui/screens/game/GameScreen.tscn" },
    { ScreenId.GameOver, "res://ui/screens/gameover/GameOverScreen.tscn" }
  };
}
