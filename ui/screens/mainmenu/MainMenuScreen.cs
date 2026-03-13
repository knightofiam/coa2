using com.forerunnergames.coa2.ui.audio.music;
using com.forerunnergames.coa2.ui.data;
using com.forerunnergames.coa2.ui.screens.context;
using com.forerunnergames.coa2.ui.audio.sfx;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.screens.mainmenu;

public partial class MainMenuScreen : Control, IScreen
{
  public ScreenId ScreenId => ScreenId.MainMenu;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private UI _ui = null!;
  private PanelContainer _background = null!;
  private Button _startGameButton = null!;
  private Button _quitGameButton = null!;
  private ScreenContext? _screenContext;
  private bool _isLeftMouseButtonPressed;
  public Control AsControl() => this;

  public override void _Ready()
  {
    _ui = GetNode <UI> ("/root/UI");
    _background = GetNode <PanelContainer> ("%Background");
    _startGameButton = GetNode <Button> ("%StartGameButton");
    _quitGameButton = GetNode <Button> ("%QuitGameButton");
    _startGameButton.Pressed += OnStartGameButtonPressed;
    _quitGameButton.Pressed += OnQuitGameButtonPressed;
    _background.AddThemeStyleboxOverride ("panel", StyleData.GetScreenBackgroundStyle (ScreenId.MainMenu));
    ButtonSfx.AddClickAndHoverSfx (_startGameButton);
    ButtonSfx.AddClickAndHoverSfx (_quitGameButton);
    _ui.SetTopBarMenuMode();
    Log.Debug ("Loaded {name} Screen", Name);
  }

  public void OnScreenActive (ScreenContext? screenContext)
  {
    Log.Debug ("Screen active, has data: {hasData}", screenContext != null);
    _screenContext = screenContext;
  }

  private void OnStartGameButtonPressed()
  {
    if (_ui.IsAnyDialogVisible()) return;
    Log.Info ("Pressed Start Game button");
    _ui.GoToScreen (ScreenId.Game, nextMusicTrack: MusicId.Summer);
  }

  private void OnQuitGameButtonPressed()
  {
    if (_ui.IsAnyDialogVisible()) return;
    Log.Info ("Pressed Quit Game button");
    _quitGameButton.Disabled = true;
    GetTree().Quit();
  }
}
