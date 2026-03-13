using System;
using System.Collections.Generic;
using System.Linq;
using com.forerunnergames.coa2.core.settings;
using com.forerunnergames.coa2.tools.events;
using com.forerunnergames.coa2.tools.events.args;
using com.forerunnergames.coa2.ui.audio;
using com.forerunnergames.coa2.ui.audio.music;
using com.forerunnergames.coa2.ui.screens;
using com.forerunnergames.coa2.ui.audio.sfx;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.dialogs.settings;

public partial class SettingsDialog : CanvasLayer
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private static readonly string[] LogLevels = ["Off", "Trace", "Debug", "Info", "Warn", "Error", "Fatal"];
  private UI _ui = null!;
  private Button _confirmButton = null!;
  private CheckBox _musicVolumeMuteCheckBox = null!;
  private HSlider _musicVolumeSlider = null!;
  private Label _musicVolumeLabel = null!;
  private CheckBox _sfxVolumeMuteCheckBox = null!;
  private HSlider _sfxVolumeSlider = null!;
  private Label _sfxVolumeLabel = null!;
  private CheckBox _playIntroVideoCheckBox = null!;
  private CheckBox _disableScreenTransitionsCheckBox = null!;
  private CheckBox _disableTooltipsCheckBox = null!;
  private OptionButton _startScreenDropdown = null!;
  private OptionButton _consoleLogLevelDropdown = null!;
  private bool _isDraggingSfxSlider;
  private bool _isDraggingMusicSlider;
  private bool _ignoreMusicSliderChange;
  private bool _ignoreSfxSliderChange;
  private bool _ignoreMusicMuteCheckBoxChange;
  private bool _ignoreSfxMuteCheckBoxChange;
  private GameSettings? _currentGameSettings;
  private void OnConfirmButtonPressed() => Close();
  private void OnSfxVolumeSliderDragStarted() => _isDraggingSfxSlider = true;
  private void OnMusicVolumeSliderDragStarted() => _isDraggingMusicSlider = true;
  private void OnNewGameConfirmationDialogCanceled() => Log.Debug ("User cancelled: Continuing current game with saved settings for next game");
  private void ShowNewGameConfirmationDialog() => _ui.ShowNewGameConfirmationDialog (onConfirm: OnNewGameConfirmationDialogConfirmed, onCancel: OnNewGameConfirmationDialogCanceled);
  private void UpdateSfxLabel() => _sfxVolumeLabel.Text = _sfxVolumeMuteCheckBox.ButtonPressed ? "Muted" : $"{Mathf.RoundToInt (_sfxVolumeSlider.Value)}%";
  private void UpdateMusicLabel() => _musicVolumeLabel.Text = _musicVolumeMuteCheckBox.ButtonPressed ? "Muted" : $"{Mathf.RoundToInt (_musicVolumeSlider.Value)}%";
  private bool CurrentGameSettingsChanged() => _currentGameSettings.HasValue && _currentGameSettings.Value != Settings.GameSettingsSnapshot;
  private static List <ScreenId> GetScreenIds() => Enum.GetValues <ScreenId>().ToList();
  private static int AsHumanPlayersDropDownIndex (int humanPlayerCount) => humanPlayerCount;
  private static int AsHumanPlayers (int humanPlayersDropDownIndex) => humanPlayersDropDownIndex;

  private static readonly Dictionary <string, Action <bool>> CheckboxActions = new()
  {
    [nameof (Settings.Instance.PlayIntroVideo)] = pressed => Settings.Instance.PlayIntroVideo = pressed,
    [nameof (Settings.Instance.DisableScreenTransitions)] = pressed => Settings.Instance.DisableScreenTransitions = pressed,
    [nameof (Settings.Instance.DisableTooltips)] = pressed => Settings.Instance.DisableTooltips = pressed,
  };

  public override void _Ready()
  {
    _ui = GetNode <UI> ("/root/UI");
    _confirmButton = GetNode <Button> ("%ConfirmButton");
    _musicVolumeMuteCheckBox = GetNode <CheckBox> ("%MusicVolumeMuteCheckBox");
    _musicVolumeSlider = GetNode <HSlider> ("%MusicVolumeSlider");
    _musicVolumeLabel = GetNode <Label> ("%MusicVolumeLabel");
    _sfxVolumeMuteCheckBox = GetNode <CheckBox> ("%SfxVolumeMuteCheckBox");
    _sfxVolumeSlider = GetNode <HSlider> ("%SfxVolumeSlider");
    _sfxVolumeLabel = GetNode <Label> ("%SfxVolumeLabel");
    _playIntroVideoCheckBox = GetNode <CheckBox> ("%PlayIntroVideoCheckBox");
    _disableScreenTransitionsCheckBox = GetNode <CheckBox> ("%DisableScreenTransitionsCheckBox");
    _disableTooltipsCheckBox = GetNode <CheckBox> ("%DisableTooltipsCheckBox");
    _startScreenDropdown = GetNode <OptionButton> ("%StartScreenDropdown");
    _consoleLogLevelDropdown = GetNode <OptionButton> ("%ConsoleLogLevelDropdown");
    _confirmButton.Pressed += OnConfirmButtonPressed;
    _musicVolumeMuteCheckBox.Toggled += OnMusicVolumeMuteCheckBoxToggled;
    _musicVolumeSlider.ValueChanged += OnMusicVolumeSliderValueChanged;
    _musicVolumeSlider.DragStarted += OnMusicVolumeSliderDragStarted;
    _musicVolumeSlider.DragEnded += OnMusicVolumeSliderDragEnded;
    _sfxVolumeMuteCheckBox.Toggled += OnSfxVolumeMuteCheckBoxToggled;
    _sfxVolumeSlider.ValueChanged += OnSfxVolumeSliderValueChanged;
    _sfxVolumeSlider.DragStarted += OnSfxVolumeSliderDragStarted;
    _sfxVolumeSlider.DragEnded += OnSfxVolumeSliderDragEnded;
    _playIntroVideoCheckBox.Toggled += pressed => OnCheckboxToggled (nameof (Settings.Instance.PlayIntroVideo), pressed);
    _disableScreenTransitionsCheckBox.Toggled += pressed => OnCheckboxToggled (nameof (Settings.Instance.DisableScreenTransitions), pressed);
    _disableTooltipsCheckBox.Toggled += pressed => OnCheckboxToggled (nameof (Settings.Instance.DisableTooltips), pressed);
    _startScreenDropdown.ItemSelected += OnStartScreenDropdownItemSelected;
    _consoleLogLevelDropdown.ItemSelected += OnConsoleLogLevelDropdownItemSelected;
    ButtonSfx.AddClickAndHoverSfx (_confirmButton);
    InitializeDropdowns();
    Hide (notify: false);
  }

  public override void _Input (InputEvent @event)
  {
    if (!Visible) return; // Only handle input when dialog is visible
    if (@event is not InputEventKey { Keycode: Key.Escape, Pressed: true, Echo: false }) return;
    Close();
    GetViewport().SetInputAsHandled();
  }

  public void Toggle (GameSettings? currentGameSettings)
  {
    _currentGameSettings = currentGameSettings;

    if (Visible)
    {
      Close();
      return;
    }

    Show();
  }

  private new void Show()
  {
    if (Visible) return;
    Log.Debug ("=== OPENING SETTINGS DIALOG ===");
    base.Show();
    Settings.Reload();
    Log.Debug ("After Reload - About to update UI controls from loaded settings");
    UpdateGameSettings();
    UpdateAudioSettings();
    UpdateDebugSettings();
    Log.Debug ("=== SETTINGS DIALOG OPENED ===");
    EventBus.Emit (new DialogShownEventArgs());
  }

  private void Close()
  {
    if (!Visible) return;
    Log.Debug ("Settings dialog closed");
    if (CheckCurrentGameSettingsChanged()) return;
    Hide();
  }

  private void Hide (bool notify = true)
  {
    base.Hide();
    if (!notify) return;
    EventBus.Emit (new DialogHiddenEventArgs());
  }

  private void OnSfxVolumeSliderDragEnded (bool valueChanged)
  {
    _isDraggingSfxSlider = false;
    if (!valueChanged) return;
    AudioManager.PlaySfx (SfxId.Error); // Volume reference feedback
    Settings.Instance.Save();
  }

  private void OnMusicVolumeSliderDragEnded (bool valueChanged)
  {
    _isDraggingMusicSlider = false;
    if (!valueChanged) return;
    Settings.Instance.Save();
  }

  private void OnMusicVolumeMuteCheckBoxToggled (bool isMuted)
  {
    if (_ignoreMusicMuteCheckBoxChange) return;
    Log.Debug ("OnMusicVolumeMuteCheckBoxToggled: isMuted={isMuted}, CurrentMusicVolume={currentVolume}, PreMuteMusicVolume={preMuteVolume}", isMuted, Settings.Instance.MusicVolume, Settings.Instance.PreMuteMusicVolume);
    Settings.Instance.PreMuteMusicVolume = isMuted ? Settings.Instance.MusicVolume : Settings.Instance.PreMuteMusicVolume;
    Settings.Instance.MusicVolume = isMuted ? 0.0f : Settings.Instance.PreMuteMusicVolume;
    Settings.Instance.IsMusicMuted = isMuted;
    Log.Debug ("After toggle: MusicVolume={musicVolume}, IsMusicMuted={isMusicMuted}, PreMuteMusicVolume={preMuteMusicVolume}", Settings.Instance.MusicVolume, Settings.Instance.IsMusicMuted, Settings.Instance.PreMuteMusicVolume);
    _ignoreMusicSliderChange = true;
    _musicVolumeSlider.Editable = !isMuted;
    _musicVolumeSlider.Value = Settings.Instance.MusicVolume * 100.0f;
    _ignoreMusicSliderChange = false;
    UpdateMusicLabel();
    ApplyAudioVolumes();
    Settings.Instance.Save();
  }

  private void OnMusicVolumeSliderValueChanged (double value)
  {
    Log.Debug ("OnMusicVolumeSliderValueChanged: value={value}, _ignoreMusicSliderChange={ignore}, IsMuted={isMuted}, _isDragging={isDragging}, SliderEditable={editable}", value, _ignoreMusicSliderChange, _musicVolumeMuteCheckBox.ButtonPressed, _isDraggingMusicSlider, _musicVolumeSlider.Editable);
    if (_ignoreMusicSliderChange || _musicVolumeMuteCheckBox.ButtonPressed) return;
    var volume = (float)value / 100.0f;
    Log.Debug ("Setting MusicVolume to {volume1} and PreMuteMusicVolume to {volume2}", volume, volume);
    Settings.Instance.MusicVolume = volume;
    Settings.Instance.PreMuteMusicVolume = volume;
    UpdateMusicLabel();
    ApplyAudioVolumes();
    if (_isDraggingMusicSlider) return;
    Log.Debug ("Saving settings after music slider change");
    Settings.Instance.Save();
  }

  private void OnSfxVolumeMuteCheckBoxToggled (bool isMuted)
  {
    if (_ignoreSfxMuteCheckBoxChange) return;
    Log.Debug ("OnSfxVolumeMuteCheckBoxToggled: isMuted={isMuted}, CurrentSfxVolume={currentVolume}, PreMuteSfxVolume={preMuteVolume}", isMuted, Settings.Instance.SfxVolume, Settings.Instance.PreMuteSfxVolume);
    Settings.Instance.PreMuteSfxVolume = isMuted ? Settings.Instance.SfxVolume : Settings.Instance.PreMuteSfxVolume;
    Settings.Instance.SfxVolume = isMuted ? 0.0f : Settings.Instance.PreMuteSfxVolume;
    Settings.Instance.IsSfxMuted = isMuted;
    Log.Debug ("After toggle: SfxVolume={sfxVolume}, IsSfxMuted={isSfxMuted}, PreMuteSfxVolume={preMuteSfxVolume}", Settings.Instance.SfxVolume, Settings.Instance.IsSfxMuted, Settings.Instance.PreMuteSfxVolume);
    _ignoreSfxSliderChange = true;
    _sfxVolumeSlider.Editable = !isMuted;
    _sfxVolumeSlider.Value = Settings.Instance.SfxVolume * 100.0f;
    _ignoreSfxSliderChange = false;
    UpdateSfxLabel();
    ApplyAudioVolumes();
    Settings.Instance.Save();
  }

  private void OnSfxVolumeSliderValueChanged (double value)
  {
    Log.Debug ("OnSfxVolumeSliderValueChanged: value={value}, _ignoreSfxSliderChange={ignore}, IsMuted={isMuted}, _isDragging={isDragging}, SliderEditable={editable}", value, _ignoreSfxSliderChange, _sfxVolumeMuteCheckBox.ButtonPressed, _isDraggingSfxSlider, _sfxVolumeSlider.Editable);
    if (_ignoreSfxSliderChange || _sfxVolumeMuteCheckBox.ButtonPressed) return;
    var volume = (float)value / 100.0f;
    Log.Debug ("Setting SfxVolume to {volume1} and PreMuteSfxVolume to {volume2}", volume, volume);
    Settings.Instance.SfxVolume = volume;
    Settings.Instance.PreMuteSfxVolume = volume;
    UpdateSfxLabel();
    ApplyAudioVolumes();
    if (_isDraggingSfxSlider) return;
    Log.Debug ("Playing Pierce SFX for volume reference at SfxVolume={volume}", Settings.Instance.SfxVolume);
    AudioManager.PlaySfx (SfxId.Error); // Volume reference feedback for keyboard/click
    Log.Debug ("Saving settings after SFX slider change");
    Settings.Instance.Save();
  }

  private static void OnCheckboxToggled (string settingName, bool pressed)
  {
    if (CheckboxActions.TryGetValue (settingName, out var action)) action (pressed);
    Settings.Instance.Save();
  }

  private void InitializeDropdowns()
  {
    InitializeStartScreenDropDown();
    InitializeLogLevelDropDown();
  }

  // TODO Implement
  private void UpdateGameSettings() { }

  private void UpdateAudioSettings()
  {
    UpdateMusicSettings();
    UpdateSfxSettings();
  }

  private void UpdateMusicSettings()
  {
    var musicMuted = Settings.Instance.IsMusicMuted;
    Log.Debug ("UpdateMusicSettings: MusicVolume={musicVolume}, IsMusicMuted={isMusicMuted}, PreMuteMusicVolume={preMuteMusicVolume}", Settings.Instance.MusicVolume, musicMuted, Settings.Instance.PreMuteMusicVolume);
    _ignoreMusicMuteCheckBoxChange = true;
    _musicVolumeMuteCheckBox.ButtonPressed = musicMuted;
    _ignoreMusicMuteCheckBoxChange = false;
    _musicVolumeSlider.Editable = !musicMuted;
    _ignoreMusicSliderChange = true;
    _musicVolumeSlider.Value = musicMuted ? Settings.Instance.PreMuteMusicVolume * 100.0f : Settings.Instance.MusicVolume * 100.0f;
    _ignoreMusicSliderChange = false;
    Log.Debug ("UpdateMusicSettings: Setting slider to {sliderValue}%", _musicVolumeSlider.Value);
    UpdateMusicLabel();
  }

  private void UpdateSfxSettings()
  {
    var sfxMuted = Settings.Instance.IsSfxMuted;
    Log.Debug ("UpdateSfxSettings: SfxVolume={sfxVolume}, IsSfxMuted={isSfxMuted}, PreMuteSfxVolume={preMuteSfxVolume}", Settings.Instance.SfxVolume, sfxMuted, Settings.Instance.PreMuteSfxVolume);
    _ignoreSfxMuteCheckBoxChange = true;
    _sfxVolumeMuteCheckBox.ButtonPressed = sfxMuted;
    _ignoreSfxMuteCheckBoxChange = false;
    _sfxVolumeSlider.Editable = !sfxMuted;
    _ignoreSfxSliderChange = true;
    _sfxVolumeSlider.Value = sfxMuted ? Settings.Instance.PreMuteSfxVolume * 100.0f : Settings.Instance.SfxVolume * 100.0f;
    _ignoreSfxSliderChange = false;
    Log.Debug ("UpdateSfxSettings: Setting slider to {sliderValue}%, Editable={editable}", _sfxVolumeSlider.Value, _sfxVolumeSlider.Editable);
    UpdateSfxLabel();
  }

  private void UpdateDebugSettings()
  {
    _playIntroVideoCheckBox.ButtonPressed = Settings.Instance.PlayIntroVideo;
    _disableScreenTransitionsCheckBox.ButtonPressed = Settings.Instance.DisableScreenTransitions;
    _disableTooltipsCheckBox.ButtonPressed = Settings.Instance.DisableTooltips;
    _startScreenDropdown.Selected = Settings.Instance.StartScreen.HasValue ? (int)Settings.Instance.StartScreen.Value : (int)ScreenId.MainMenu; // Default to Hub;
    var logLevelIndex = Array.IndexOf (LogLevels, Settings.Instance.ConsoleLogLevel);
    _consoleLogLevelDropdown.Selected = logLevelIndex >= 0 ? logLevelIndex : 3; // Default to Info
  }

  private bool CheckCurrentGameSettingsChanged()
  {
    if (!CurrentGameSettingsChanged()) return false;
    Hide();
    ShowNewGameConfirmationDialog();
    return true;
  }

  private void OnNewGameConfirmationDialogConfirmed()
  {
    Log.Debug ("User confirmed: Starting new game with updated settings");
    _ui.GoToScreen (ScreenId.Game, nextMusicTrack: MusicId.Summer);
  }

  private void InitializeStartScreenDropDown()
  {
    _startScreenDropdown.Clear();
    GetScreenIds().ForEach (screenId => _startScreenDropdown.AddItem (screenId.ToString(), (int)screenId));
  }

  private void InitializeLogLevelDropDown()
  {
    _consoleLogLevelDropdown.Clear();
    for (var i = 0; i < LogLevels.Length; ++i) _consoleLogLevelDropdown.AddItem (LogLevels [i], i);
  }

  private static void OnStartScreenDropdownItemSelected (long index)
  {
    Settings.Instance.StartScreen = (ScreenId)index;
    Settings.Instance.Save();
  }

  private static void OnConsoleLogLevelDropdownItemSelected (long index)
  {
    Settings.Instance.ConsoleLogLevel = LogLevels [index];
    Settings.Instance.ApplyLogLevel();
    Settings.Instance.Save();
  }

  private static void SyncDefaultCheckBox (CheckBox checkBox, ref bool ignoreFlag, bool isDefault)
  {
    if (checkBox.ButtonPressed == isDefault) return;
    ignoreFlag = true;
    checkBox.ButtonPressed = isDefault;
    ignoreFlag = false;
  }

  private static void ApplyAudioVolumes()
  {
    Log.Debug ("ApplyAudioVolumes: Broadcasting MusicVolume={musicVolume}, SfxVolume={sfxVolume}", Settings.Instance.MusicVolume, Settings.Instance.SfxVolume);
    EventBus.Emit (new AudioVolumeChangedEventArgs (Settings.Instance.MusicVolume, Settings.Instance.SfxVolume));
  }
}
