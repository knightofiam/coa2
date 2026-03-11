using System;
using com.forerunnergames.coa2.tools.events;
using com.forerunnergames.coa2.tools.events.args;
using com.forerunnergames.coa2.ui.screens;
using com.forerunnergames.coa2.ui.tooltips;
using com.forerunnergames.coa2.ui.audio.sfx;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.dialogs.pause;

public partial class PauseDialog : CanvasLayer
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private UI _ui = null!;
  private Button _resumeButton = null!;
  private Button _settingsButton = null!;
  private Button _restartGameButton = null!;
  private Button _quitToMenuButton = null!;
  private Action? _onResume;
  private Action? _onQuitToMenu;
  private void OnResumeButtonPressed() => Resume();
  private void OnQuitToMenuButtonPressed() => QuitToMenu();
  private void OnSettingsButtonPressed() => _ui.ToggleSettingsDialog();

  public override void _Ready()
  {
    _ui = GetNode <UI> ("/root/UI");
    _resumeButton = GetNode <Button> ("%ResumeButton");
    _restartGameButton = GetNode <Button> ("%RestartButton");
    _settingsButton = GetNode <Button> ("%SettingsButton");
    _quitToMenuButton = GetNode <Button> ("%QuitToMenuButton");
    _resumeButton.Pressed += OnResumeButtonPressed;
    _settingsButton.Pressed += OnSettingsButtonPressed;
    _restartGameButton.Pressed += OnRestartGameButtonPressed;
    _quitToMenuButton.Pressed += OnQuitToMenuButtonPressed;
    ButtonSfx.AddClickAndHoverSfx (_resumeButton);
    ButtonSfx.AddClickAndHoverSfx (_restartGameButton);
    ButtonSfx.AddClickAndHoverSfx (_settingsButton);
    ButtonSfx.AddClickAndHoverSfx (_quitToMenuButton);
    Hide (notify: false);
  }

  public override void _ExitTree()
  {
    _resumeButton.Pressed -= OnResumeButtonPressed;
    _settingsButton.Pressed -= OnSettingsButtonPressed;
    _restartGameButton.Pressed -= OnRestartGameButtonPressed;
    _quitToMenuButton.Pressed -= OnQuitToMenuButtonPressed;
  }

  public void Toggle (Action? onResume = null, Action? onQuitToMenu = null)
  {
    if (CheckResume()) return;
    _onResume = onResume;
    _onQuitToMenu = onQuitToMenu;
    Show();
  }

  private new void Show()
  {
    if (Visible) return;
    Log.Debug ("Pause dialog shown");
    TooltipManager.HideTooltip();
    _quitToMenuButton.Text = _ui.IsCurrentScreen (ScreenId.MainMenu) ? "Quit Game" : " Quit to Main Menu ";
    base.Show();
    EventBus.Emit (new DialogShownEventArgs());
  }

  private void Hide (bool notify = true)
  {
    base.Hide();
    _onResume = null;
    _onQuitToMenu = null;
    if (!notify) return;
    EventBus.Emit (new DialogHiddenEventArgs());
  }

  private void Resume()
  {
    if (!Visible) return;
    Log.Debug ("Resuming game");
    var onResumeCopy = _onResume;
    Hide();
    onResumeCopy?.Invoke();
  }

  private void QuitToMenu()
  {
    if (!Visible) return;
    var onQuitToMenu = _onQuitToMenu;
    Hide();
    onQuitToMenu?.Invoke();
    TooltipManager.HideTooltip();
    if (CheckQuitApplication()) return;
    Log.Debug ("Quitting to main menu");
    _ui.GoToScreen (ScreenId.MainMenu);
  }

  private bool CheckResume()
  {
    if (!Visible) return false;
    Resume();
    return true;
  }

  private void OnRestartGameButtonPressed()
  {
    if (!Visible) return;
    Log.Info ("Pressed restart game button");
    Hide();
    _ui.GoToScreen (ScreenId.Game);
  }

  private bool CheckQuitApplication()
  {
    if (!_ui.IsCurrentScreen (ScreenId.MainMenu)) return false;
    Log.Debug ("Quitting application");
    GetTree().Quit();
    return true;
  }
}
