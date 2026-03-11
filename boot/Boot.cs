using com.forerunnergames.coa2.core.settings;
using com.forerunnergames.coa2.tools;
using com.forerunnergames.coa2.tools.events;
using com.forerunnergames.coa2.tools.events.args;
using com.forerunnergames.coa2.tools.logging;
using com.forerunnergames.coa2.ui;
using com.forerunnergames.coa2.ui.audio;
using com.forerunnergames.coa2.ui.audio.music;
using com.forerunnergames.coa2.ui.intro;
using com.forerunnergames.coa2.ui.screens;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.boot;

// Main Scene
// Loads after UI.cs
public partial class Boot : Node
{
  private Logger _log = null!;
  private UI _ui = null!;
  private VideoIntro _videoIntro = null!;
  public override void _ExitTree() => _videoIntro.VideoFinished -= OnVideoIntroFinished;
  private void OnVideoIntroFinished() => StartGame();

  public override void _Ready()
  {
    _ui = GetNode <UI> ("/root/UI");
    _videoIntro = _ui.GetNode <VideoIntro> ("%VideoIntro");
    _videoIntro.VideoFinished += OnVideoIntroFinished;
    var parser = new CommandLineParser ("flags");
    // Load settings from file first
    // Then override with command line flags if present
    if (parser.IsSet ("disable-screen-transitions")) Settings.Instance.DisableScreenTransitions = true;
    if (parser.IsSet ("disable-tooltips")) Settings.Instance.DisableTooltips = true;
    if (parser.IsSet ("start-screen")) Settings.Instance.StartScreen = parser.TryGet ("start-screen", or: (ScreenId?)null);
    ConfigureLogging (parser);
    #if GODOT_WINDOWS
    DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
    #endif
    CallDeferred (MethodName.PlayIntroVideo);
  }

  private void StartGame()
  {
    // Don't fade out (video already faded), but do fade in; set fadeOutDuration to 0
    _ui.GoToScreen (Settings.Instance.StartScreen.HasValue ? Settings.Instance.StartScreen.Value : ScreenId.MainMenu, fade: true, fadeOutDuration: 0.0f, fadeInDuration: 0.35f);
  }

  private void PlayIntroVideo()
  {
    AudioManager.PlayMusic (MusicId.MainMenu); // Start menu music (continues to Hub screen)
    _videoIntro.Play();
  }

  private void ConfigureLogging (CommandLineParser parser)
  {
    // If we're passing a help flag via the command line, print the help output to the console and then shut down.
    if (parser.ShouldShowHelp()) parser.ShowHelp (after: () => OS.Kill (OS.GetProcessId()));
    Logging.DisableStringQuoting(); // Don't show strings in unnecessary double quotes in log messages, improves log readability.
    // Configure the minimum log level, i.e., the most detailed level that is allowed to log.
    // Try to obtain a custom level from the command line flag, then fall back to settings, then fall back to the default.
    Logging.IsLogLevelFlagSet = parser.IsSet ("log-console");

    if (Logging.IsLogLevelFlagSet)
    {
      // Command line flag takes precedence
      var cmdLineLogLevel = Logging.GetMinConsoleLogLevel() ?? Logging.DefaultLogLevel;
      Logging.SetMinConsoleLogLevel (parser.TryGet ("log-console", or: cmdLineLogLevel));
    }
    else
    {
      // Apply log level from settings if no command line flag
      Settings.Instance.ApplyLogLevel();
    }

    var minConsoleLogLevel = Logging.GetMinConsoleLogLevel() ?? Logging.DefaultLogLevel;

    // Apply file log level if specified
    if (parser.IsSet ("log-file"))
    {
      var fileLogLevel = parser.TryGet ("log-file", or: LogLevel.Trace);
      Logging.SetMinFileLogLevel (fileLogLevel);
    }

    // Apply global minimum if specified
    var globalMinimumLogLevel = parser.TryGet <LogLevel?> ("log-all", null);
    if (globalMinimumLogLevel != null) Logging.SetGlobalThresholdLogLevel (globalMinimumLogLevel);
    // Also print to Godot console when running in editor.
    // Note: IDE's use command-line godot, which still detects itself as being run in the editor,
    // so we have to pass a custom flag, --no-editor-logging to the command line to avoid double-logging in the IDE console.
    // If --no-editor-logging is not present, such as when running from the graphical editor, then editor logging will be
    // activated via GD.Print with a custom logging target, while continuing to log to file.
    var isRunningInGraphicalEditor = OS.HasFeature ("editor") && !parser.TryGet ("no-editor-logging", or: false);
    if (isRunningInGraphicalEditor) Logging.RegisterGodotConsoleTarget();
    _log = LogManager.GetLogger (GetType().FullName);
    _log.Info ("Console log level [{level}]", minConsoleLogLevel);
    _log.Info ("Godot editor console logging is {enabledOrDisabled}", isRunningInGraphicalEditor ? "enabled" : "disabled");
    EventBus.Emit (new LoggingInitializedEventArgs());
  }
}
