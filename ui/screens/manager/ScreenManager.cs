using System;
using Godot;
using System.Linq;
using System.Threading.Tasks;
using com.forerunnergames.coa2.core.settings;
using com.forerunnergames.coa2.tools.events;
using com.forerunnergames.coa2.tools.events.args;
using com.forerunnergames.coa2.ui.audio;
using com.forerunnergames.coa2.ui.audio.music;
using com.forerunnergames.coa2.ui.data;
using com.forerunnergames.coa2.ui.effects.screenFade;
using com.forerunnergames.coa2.ui.effects.screenShake;
using com.forerunnergames.coa2.ui.screens.context;
using com.forerunnergames.coa2.ui.screens.game;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.screens;

public partial class ScreenManager : Node
{
  private UI _ui = null!;
  private ScreenFade _screenFade = null!;
  private CanvasLayer _screenFadeExclusions = null!;
  private ScreenShakeEffect _screenShakeEffect = null!;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  public GameSettings? GetCurrentGameSettings() => (GetCurrentScreen() as GameScreen)?.CurrentGameSettings;
  public bool IsCurrent (ScreenId screenId) => GetCurrentScreen()?.ScreenId == screenId;
  public void GoTo (ScreenId screenId, ScreenContext? screenContext = null, bool fade = true, float? fadeInDuration = null, float? fadeOutDuration = null, MusicId? nextMusicTrack = null) => _ = GoToScreenAsync (screenId, screenContext, fade, fadeInDuration, fadeOutDuration, nextMusicTrack);
  public void ShakeCurrentScreen (float intensity, float durationSeconds) => _screenShakeEffect.Play (GetCurrentScreen()?.AsControl(), intensity, durationSeconds);
  private IScreen? GetCurrentScreen() => GetTree().GetCurrentScene() as IScreen;
  private static bool IsScenePathValid (string? scenePath) => scenePath != null && ResourceLoader.Exists (scenePath) && scenePath.EndsWith (".tscn");

  public override void _Ready()
  {
    _ui = GetNode <UI> ("/root/UI");
    _screenFade = GetNode <ScreenFade> ("ScreenFade");
    _screenFadeExclusions = GetNode <CanvasLayer> ("%ScreenFadeExclusions");
    _screenShakeEffect = GetNode <ScreenShakeEffect> ("%ScreenShakeEffect");
    _ = CheckScreenRegistrationsAsync();
  }

  private async Task GoToScreenAsync (ScreenId screenId, ScreenContext? screenContext = null, bool fade = true, float? fadeInDuration = null, float? fadeOutDuration = null, MusicId? nextMusicTrack = null)
  {
    var path = ScreenData.GetScenePath (screenId);
    if (!CheckPath (path, screenId)) return;
    if (!await CheckChangeScene (path, screenId, fade, fadeOutDuration, nextMusicTrack)) return;
    await ToSignal (GetTree(), SceneTree.SignalName.ProcessFrame);
    await ToSignal (GetTree(), SceneTree.SignalName.ProcessFrame);
    if (!CheckCurrentScreen (screenId)) return;
    var currentScreen = GetCurrentScreen();
    if (currentScreen == null) return;
    screenContext?.TransitionAction?.Invoke();
    currentScreen.OnScreenActive (screenContext);
    if (fade) await FadeInScreenAsync (fadeInDuration);
  }

  private async Task <bool> CheckChangeScene (string? path, ScreenId screenId, bool fade = true, float? fadeOutDuration = null, MusicId? nextMusicTrack = null)
  {
    if (path == null) return false;
    await CheckFadeOutScreenAsync (fade, fadeOutDuration, nextMusicTrack);
    var error = GetTree().ChangeSceneToFile (path);
    if (error == Error.Ok) return true;
    HandleError ($"Failed to navigate to screen [{screenId}].\n\nGodot error: {error}");
    if (fade) await FadeInScreenAsync();
    return false;
  }

  private async Task CheckFadeOutScreenAsync (bool fade = true, float? fadeOutDuration = null, MusicId? nextMusicTrack = null)
  {
    if (!fade) return;
    _ = AudioManager.CrossfadeToMusic (nextMusicTrack, 4.0f);
    await FadeOutScreenAsync (fadeOutDuration);
  }

  private bool CheckCurrentScreen (ScreenId screenId)
  {
    if (GetTree().GetCurrentScene() is IScreen) return true;
    var currentSceneName = GetTree().GetCurrentScene()?.Name ?? "null";
    HandleError ($"Failed to activate screen [{screenId}].\n\nCurrent scene '{currentSceneName}' does not implement IScreen interface.");
    return false;
  }

  private static bool CheckPath (string? path, ScreenId screenId)
  {
    if (path != null) return true;
    HandleError ($"Screen [{screenId}] has no registered scene path.\n\nAdd an entry to ScreenData.ScreenIdsToScenePaths.");
    return false;
  }

  private async Task CheckScreenRegistrationsAsync()
  {
    await ToSignal (GetTree(), SceneTree.SignalName.ProcessFrame); // Wait a frame for log initialization.
    Enum.GetValues <ScreenId>().ToList().ForEach (CheckScreenRegistration);
  }

  private static void CheckScreenRegistration (ScreenId screenId)
  {
    Log.Trace ("Checking registration for Screen [{screenId}]...", screenId);
    var path = ScreenData.GetScenePath (screenId);
    if (!CheckScenePathRegistration (path, screenId)) return;
    if (IsScenePathValid (path)) return;
    HandleError ($"Screen [{screenId}] has invalid scene path.\n\nPath: {path}\n\nThe .tscn file does not exist or is invalid.");
  }

  private static bool CheckScenePathRegistration (string? path, ScreenId screenId)
  {
    if (path != null) return true;
    HandleError ($"Screen [{screenId}] has no registered scene path.\n\nAdd an entry to ScreenData.ScreenIdsToScenePaths.");
    return false;
  }

  private static void HandleError (string message)
  {
    var displayMessage = $"Screen Error\n\n{message}";
    Log.Error (message);
    EventBus.Emit (new ErrorEventArgs (displayMessage));
  }

  private async Task FadeOutScreenAsync (float? duration = null)
  {
    if (Settings.Instance.DisableScreenTransitions) return;
    await _screenFade.FadeOut (duration);
  }

  private async Task FadeInScreenAsync (float? duration = null)
  {
    _ui.ShowTopBar();
    if (Settings.Instance.DisableScreenTransitions) return;
    await _screenFade.FadeIn (duration);
  }
}
