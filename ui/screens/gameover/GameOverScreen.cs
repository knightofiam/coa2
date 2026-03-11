using com.forerunnergames.coa2.ui.screens.context;
using com.forerunnergames.coa2.ui.audio.sfx;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.screens.gameover;

public partial class GameOverScreen : Control, IScreen
{
  public ScreenId ScreenId => ScreenId.GameOver;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private UI _ui = null!;
  private PanelContainer _background = null!;
  private Label _titleLabel = null!;
  private Button _newGameButton = null!;
  private Button _mainMenuButton = null!;
  private ScreenContext? _screenContext;
  public Control AsControl() => this;

  public override void _Ready()
  {
    _ui = GetNode <UI> ("/root/UI");
    _background = GetNode <PanelContainer> ("%Background");
    _titleLabel = GetNode <Label> ("%TitleLabel");
    _newGameButton = GetNode <Button> ("%NewGameButton");
    _mainMenuButton = GetNode <Button> ("%MainMenuButton");
    _mainMenuButton.Pressed += OnMainMenuButtonPressed;
    _newGameButton.Pressed += OnNewGameButtonPressed;
    _titleLabel.Hide();
    _ui.SetTopBarMenuMode();
    ButtonSfx.AddClickAndHoverSfx (_mainMenuButton);
    Log.Debug ("Loaded {name} Screen", Name);
  }

  public void OnScreenActive (ScreenContext? screenContext)
  {
    Log.Debug ("Screen active, has data: {hasData}", screenContext != null);
    _screenContext = screenContext;
    _titleLabel.Show();
  }

  private void OnNewGameButtonPressed()
  {
    if (_ui.IsAnyDialogVisible()) return;
    Log.Info ("Pressed new game button");
    _ui.GoToScreen (ScreenId.Game);
  }

  private void OnMainMenuButtonPressed()
  {
    if (_ui.IsAnyDialogVisible()) return;
    Log.Info ("Pressed main menu button");
    _ui.GoToScreen (ScreenId.MainMenu);
  }
}
