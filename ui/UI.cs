using com.forerunnergames.coa2.tools.events;
using com.forerunnergames.coa2.tools.events.args;
using System;
using com.forerunnergames.coa2.core.data;
using com.forerunnergames.coa2.core.game;
using com.forerunnergames.coa2.ui.audio;
using com.forerunnergames.coa2.ui.screens;
using com.forerunnergames.coa2.ui.screens.context;
using com.forerunnergames.coa2.ui.tooltips;
using com.forerunnergames.coa2.ui.dialogs.confirmation;
using com.forerunnergames.coa2.ui.dialogs.pause;
using com.forerunnergames.coa2.ui.dialogs.settings;
using com.forerunnergames.coa2.ui.loading;
using com.forerunnergames.coa2.ui.messages;
using com.forerunnergames.coa2.ui.topbar;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui;

// TODO Use button styles, dropdown theme, etc. They aren't being used in Godot editor (showing as having 0 owners).
// Autoload
// Loads before Boot.cs
public partial class UI : Control
{
  // Begin GameData dependencies
  private static Game Game => GameData.Game;
  // End GameData dependencies

  [Export] public StyleBox ScreenNavigationButtonStyle = null!;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private ScreenManager _screenManager = null!;
  private Window _window = null!;
  private TopBar _topBar = null!;
  private SettingsDialog _settingsDialog = null!;
  private PauseDialog _pauseDialog = null!;
  private CustomConfirmationDialog _customConfirmationDialog = null!;
  private AudioManager _audioManager = null!;
  private CustomTooltip _customTooltip = null!;
  private int _visibleDialogs;
  private StyleBox? _screenNavigationButtonStyle;
  public bool IsCurrentScreen (ScreenId screenId) => _screenManager.IsCurrent (screenId);
  public bool IsAnyDialogVisible() => _visibleDialogs > 0;
  public bool IsWindowFocused() => _window.HasFocus();
  public Rect2 GetTopBarGlobalRect() => _topBar.GlobalRect();
  public void GoToScreen (ScreenId screenId, ScreenContext? screenContext = null, bool fade = true, float? fadeInDuration = null, float? fadeOutDuration = null) => _screenManager.GoTo (screenId, screenContext, fade, fadeInDuration, fadeOutDuration);
  public void SetLoadingText (string text) => _topBar.SetLoadingText (text);
  public void HideTopBar() => _topBar.Hide();
  public void ShowTopBar() => _topBar.Show();
  public void SetTopBarMenuMode() => _topBar.SetMenuMode();
  public void SetTopBarGameMode() => _topBar.SetGameMode();
  public void TogglePauseDialog (Action? onResume = null, Action? onQuitToMenu = null) => _pauseDialog.Toggle (onResume, onQuitToMenu);
  public void ToggleSettingsDialog() => _settingsDialog.Toggle (_screenManager.GetCurrentGameSettings());
  public void ShakeScreen (float intensity, float durationSeconds) => _screenManager.ShakeCurrentScreen (intensity, durationSeconds);
  public static void AddGameMessage (string text, float durationSeconds = 0.0f, Action? onShow = null, Action? onComplete = null) => EventBus.Emit (new GameMessageRequestEventArgs (new GameMessage (text, durationSeconds, onShow, onComplete)));
  private void OnSettingsButtonPressed() => ToggleSettingsDialog();

  public override void _Ready()
  {
    _window = GetWindow();
    _screenManager = GetNode <ScreenManager> ("%ScreenManager");
    _topBar = GetNode <TopBar> ("%TopBar");
    _settingsDialog = GetNode <SettingsDialog> ("%SettingsDialog");
    _pauseDialog = GetNode <PauseDialog> ("%PauseDialog");
    _customConfirmationDialog = GetNode <CustomConfirmationDialog> ("%CustomConfirmationDialog");
    _audioManager = GetNode <AudioManager> ("%AudioManager");
    _customTooltip = GetNode <CustomTooltip> ("%CustomTooltip");
    TooltipManager.Initialize (_customTooltip, defaultDelay: 0.5f);
    _topBar.SettingsButtonPressed += OnSettingsButtonPressed;
    EventBus.Instance.LoggingInitializedEvent += OnLoggingInitialized; // Wait until logging is initialized in Boot.cs.
    EventBus.Instance.DialogShownEvent += OnDialogShownEvent;
    EventBus.Instance.DialogHiddenEvent += OnDialogHiddenEvent;
  }

  public override void _ExitTree()
  {
    _topBar.SettingsButtonPressed -= OnSettingsButtonPressed;
    EventBus.Instance.LoggingInitializedEvent -= OnLoggingInitialized;
    EventBus.Instance.DialogShownEvent -= OnDialogShownEvent;
    EventBus.Instance.DialogHiddenEvent -= OnDialogHiddenEvent;
  }

  private bool CheckClosePauseDialog()
  {
    if (!_pauseDialog.Visible) return false;
    TogglePauseDialog();
    GetViewport().SetInputAsHandled();
    return true;
  }

  public override void _Input (InputEvent @event)
  {
    if (@event is not InputEventKey { Keycode: Key.Escape, Pressed: true, Echo: false }) return;
    if (_settingsDialog.Visible) return; // If settings dialog is visible, let it handle ESC
    var otherDialogsVisible = _visibleDialogs > (_pauseDialog.Visible ? 1 : 0); // If other dialogs (besides pause) are visible, let them handle ESC
    if (otherDialogsVisible) return;
    if (CheckClosePauseDialog()) return; // If pause dialog is visible (and no other dialogs), close it
    OpenPauseDialog (IsCurrentScreen (ScreenId.Game));
    GetViewport().SetInputAsHandled();
  }

  public void ShowNewGameConfirmationDialog (Action? onConfirm = null, Action? onCancel = null) =>
    _customConfirmationDialog.Show (title: "Game Settings Changed",
      message: "Would you like to start a new game now with the updated game settings?\n\nOtherwise they will be applied to the next game.",
      confirmText: "  New Game  ",
      cancelText: "  Continue Game  ",
      onConfirm: onConfirm,
      onCancel: onCancel);

  private void OnLoggingInitialized (object? sender, LoggingInitializedEventArgs e)
  {
    Log.Debug ("{name} loaded", Name);
    LoadingManager.PreloadData();
  }

  private void OnDialogShownEvent (object? sender, DialogShownEventArgs e)
  {
    _visibleDialogs++;
    Log.Trace ("Dialog shown, {visibleDialogs} dialogs visible", _visibleDialogs);
  }

  private void OnDialogHiddenEvent (object? sender, DialogHiddenEventArgs e)
  {
    if (_visibleDialogs == 0)
    {
      Log.Error ("Dialog hidden but no dialogs are visible. If you added a new dialog, it needs to publish Dialog[Shown|Hidden]EventArgs on the event bus for proper input tracking.");
      return;
    }

    _visibleDialogs--;
    Log.Trace ("Dialog hidden, {visibleDialogs} dialogs visible", _visibleDialogs);
  }

  private void OpenPauseDialog (bool isGameScreenCurrent)
  {
    if (isGameScreenCurrent) Game.Pause();
    TogglePauseDialog (onResume: () => HandleResumeFromPause (isGameScreenCurrent), onQuitToMenu: () => HandleResumeFromPause (isGameScreenCurrent));
  }

  private static void HandleResumeFromPause (bool isGameScreenCurrent)
  {
    if (!isGameScreenCurrent) return;
    Game.Resume();
  }
}
