using System.Collections.Generic;
using com.forerunnergames.coa2.ui.screens;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.data;

public static class StyleData
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private static Dictionary <ScreenId, StyleBox> _screenIdsToBackgroundStyles = null!;
  public static StyleBox GetScreenBackgroundStyle (ScreenId screenId) => _screenIdsToBackgroundStyles.GetValueOrDefault (screenId) ?? new StyleBoxFlat();

  public static void Load()
  {
    _screenIdsToBackgroundStyles = new Dictionary <ScreenId, StyleBox>
    {
      // TODO Uncomment when we have screen background images.
      // { ScreenId.MainMenu, ResourceLoader.Load <StyleBox> ("res://assets/resources/screens/backgrounds/mainmenu-screen-bg-stylebox.tres") },
      // { ScreenId.Game, ResourceLoader.Load <StyleBox> ("res://assets/resources/screens/backgrounds/game-screen-bg-stylebox.tres") },
      // { ScreenId.GameOver, ResourceLoader.Load <StyleBox> ("res://assets/resources/screens/backgrounds/gameover-screen-bg-stylebox.tres") }
    };

    Log.Debug ("Loaded style data");
  }
}
