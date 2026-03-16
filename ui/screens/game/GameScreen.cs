using System;
using com.forerunnergames.coa2.core.data;
using com.forerunnergames.coa2.core.game;
using com.forerunnergames.coa2.core.settings;
using com.forerunnergames.coa2.tools.events;
using com.forerunnergames.coa2.tools.events.args;
using com.forerunnergames.coa2.ui.background;
using com.forerunnergames.coa2.ui.screens.context;
using com.forerunnergames.coa2.ui.tooltips;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.screens.game;

public partial class GameScreen : Control, IScreen
{
  // Begin GameData dependencies
  private static Game Game => GameData.Game;
  private static GameSettings GameSettingsSnapshot => GameData.GameSettingsSnapshot;
  // End GameData dependencies

  public ScreenId ScreenId => ScreenId.Game;
  public GameSettings? CurrentGameSettings { get; private set; }
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private UI _ui = null!;
  private GameView _gameView = null!;
  private PanelContainer _background = null!;
  private Label _debugLabel = null!;
  private Control _messageLabelContainer = null!;
  private RichTextLabel _messageLabel = null!;
  private Control _messageSpacerTop = null!;
  private Control _messageSpacerBottom = null!;
  private ScreenContext? _screenContext;
  public Control AsControl() => this;
  public void SetDebugText (string text) => _debugLabel.Text = text;

  public override void _Ready()
  {
    _ui = GetNode <UI> ("/root/UI");
    _gameView = GetNode <GameView> ("%GameView");
    _debugLabel = GetNode <Label> ("%DebugLabel");
    _background = GetNode <PanelContainer> ("%Background");
    _messageLabelContainer = GetNode <Control> ("%MessageLabelContainer");
    _messageLabel = _messageLabelContainer.GetNode <RichTextLabel> ("%MessageLabel");
    _messageSpacerTop = GetNode <Control> ("%MessageSpacerTop");
    _messageSpacerBottom = GetNode <Control> ("%MessageSpacerBottom");
    EventBus.Instance.GameOverEvent += OnGameOverEvent;
    _messageLabel.Modulate = new Color (1, 1, 1, 0);
    Log.Debug ("Loaded {name} Screen", Name);
  }

  public override void _ExitTree()
  {
    EventBus.Instance.GameOverEvent -= OnGameOverEvent;
    TooltipManager.HideTooltip(); // Hide any active tooltips when leaving game screen
  }

  public override void _UnhandledInput (InputEvent @event)
  {
    _gameView.HandleInput (@event);
  }

  // ReSharper disable once AsyncVoidMethod
  public async void OnScreenActive (ScreenContext? screenContext)
  {
    Log.Debug ("Screen active, has data: {hasData}", screenContext != null);
    _screenContext = screenContext;
    CurrentGameSettings = GameSettingsSnapshot; // Capture actual game settings before starting the game.
    Game.StartGame();
    _ui.SetTopBarGameMode();
    await ToSignal (GetTree(), SceneTree.SignalName.ProcessFrame); // Wait a frame for automatic layout to finish.
  }

  // ReSharper disable once AsyncVoidEventHandlerMethod
  private async void OnGameOverEvent (object? sender, GameOverEventArgs e)
  {
    try
    {
      Log.Debug ("Game Over event received");

      // TODO Implement game over visuals instead of going to game over screen.
      // await ToSignal (GetTree(), SceneTree.SignalName.ProcessFrame); // Marshall back to the Godot thread from the C#'s event thread. Can't use CallDeferred due to lack of Variant-compatibility with ScreenContext.
      // Log.Trace ("ToSignal completed, transitioning to GameOver screen");
      // var screenContext = new ScreenContext { TransitionAction = Game.OnGameOver };
      // _ui.GoToScreen (ScreenId.GameOver, screenContext);
    }
    catch (Exception ex)
    {
      Log.Error (ex, "Exception in OnGameOverEvent");
    }
  }

  private void FadeOutMessageLabel()
  {
    if (_messageLabel.Modulate.A < 0.01f) return;
    var tween = CreateTween();
    tween.TweenProperty (_messageLabel, "modulate:a", 0.0f, 0.3f);
  }
}
