using com.forerunnergames.coa2.build;
using com.forerunnergames.coa2.ui.audio.sfx;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.topbar;

public partial class TopBar : CanvasLayer
{
  [Signal] public delegate void SettingsButtonPressedEventHandler();
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private Control _rootContainer = null!;
  private Label _buildLabel = null!;
  private Button _settingsButton = null!;
  private string _loadingText = "";
  public Rect2 GlobalRect() => _rootContainer.GetGlobalRect();
  public void SetMenuMode() { } // TODO Implement
  public void SetGameMode() { } // TODO Implement
  private void OnSettingsButtonPressed() => EmitSignal (SignalName.SettingsButtonPressed);
  private void UpdateBuildText() => _buildLabel.Text = $"Build: {BuildInfo.Summary()}";

  public override void _Ready()
  {
    _rootContainer = GetNode <Control> ("%RootContainer");
    _buildLabel = GetNode <Label> ("%BuildLabel");
    _settingsButton = GetNode <Button> ("%SettingsButton");
    _settingsButton.Pressed += OnSettingsButtonPressed;
    ButtonSfx.AddClickAndHoverSfx (_settingsButton);
    UpdateBuildText();
    Log.Debug ("{name} loaded", Name);
  }

  public void SetLoadingText (string text)
  {
    _loadingText = text;
    if (string.IsNullOrWhiteSpace (_loadingText)) return;
    Log.Info ("[{loadingText}]", _loadingText);
  }
}
