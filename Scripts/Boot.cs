using com.forerunnergames.coa.utilities;
using com.forerunnergames.coa.utilities.logging;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa.boot;

// Main Scene
public partial class Boot : Node
{
  private Logger _log = null!;

  public override void _Ready()
  {
    // If we're passing a help flag via the command line, print the help output to the console and then shut down.
    var parser = new CommandLineParser ("flags");
    if (parser.ShouldShowHelp()) parser.ShowHelp (after: () => OS.Kill (OS.GetProcessId()));

    Logging.DisableStringQuoting(); // Don't show strings in unnecessary double quotes in log messages, improves log readability.

    // Configure the minimum log level, i.e., the most detailed level that is allowed to log.
    // Try to obtain a custom level from the command line flag, then fall back to the default.
    var minConsoleLogLevel = Logging.GetMinConsoleLogLevel() ?? Logging.DefaultLogLevel;
    Logging.IsLogLevelFlagSet = parser.IsSet ("log");
    Logging.SetMinConsoleLogLevel (parser.TryGet ("log", or: minConsoleLogLevel));
    minConsoleLogLevel = Logging.GetMinConsoleLogLevel() ?? Logging.DefaultLogLevel;
    var globalThresholdLogLevel = parser.TryGet <LogLevel?> ("log-global-threshold", null);
    if (globalThresholdLogLevel != null) Logging.SetGlobalThresholdLogLevel (globalThresholdLogLevel);

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
  }
}
