using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.tools;

public class CommandLineParser (string helpFlagName)
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private readonly Dictionary <string, string> _parsed = Parse (helpFlagName);
  public bool IsSet (string flagName) => _parsed.ContainsKey (flagName);
  public bool ShouldShowHelp() => _parsed.ContainsKey (helpFlagName);

  public T TryGet <T> (string flagName, T or)
  {
    if (!_parsed.ContainsKey (flagName.ToLower())) return or;
    var name = flagName.ToLower();
    var value = _parsed [name];

    try {
      return typeof (T) == typeof (LogLevel) ? (T)(object)LogLevel.FromString (value) : (T?)TypeDescriptor.GetConverter (typeof (T)).ConvertFrom (value) ?? or;
    }
    catch (Exception e)
    {
      Log.Error ("Ignoring invalid flag value of --{name}={value}: {exceptionMessage}", name, value, e.Message);
      Log.Warn ("Falling back to default flag value of --{name}={or}", name, or);
      return or;
    }
  }

  public void ShowHelp (Action? after)
  {
    // @formatter:off
    // Use GD.Print to force printing instead of logging, in case logging is off.
    GD.Print ("\nCommand line help:");
    GD.Print ("\n  Flag format:");
    GD.Print ("\n    --name=value (where \'name\' is the setting name, & \'value\' is the setting value.)");
    GD.Print ("\n  Flags are NOT case sensitive:");
    GD.Print ("\n    --NamE=VaLue --NAME=value --name=value are all equivalent.");
    GD.Print ("\n  Available flags:\n");
    GD.Print ( "    --disable-screen-transitions: Change screens instantly, don't wait for fades (optional, values: [true, false] default: false, ex: --disable-screen-transitions=true).");
    GD.Print ( "    --disable-tooltips: Disable all tooltips (optional, values: [true, false] default: false, ex: --disable-tooltips=true).");
    GD.Print ( "    --start-screen: Start the game at a specific screen (optional, values: [MainMenu, Game, GameOver, etc.] default: MainMenu, ex: --start-screen=Game).");
    GD.Print ($"    --log-console: Console log level (optional, values: [{Strings.ToString (LogLevel.AllLevels)}] default: {LogLevel.Info}, ex: --log-console={LogLevel.Trace}). Affects console/GodotConsole output only, respects per-logger rules in nlog.config.");
    GD.Print ($"    --log-file: File log level (optional, values: [{Strings.ToString (LogLevel.AllLevels)}] default: {LogLevel.Trace}, ex: --log-file={LogLevel.Info}). Affects file output only, respects per-logger rules in nlog.config.");
    GD.Print ($"    --log-all: Hard minimum log level for ALL targets (optional, values: [{Strings.ToString (LogLevel.AllLevels)}] default: {LogLevel.Trace}, ex: --log-all={LogLevel.Warn}). Overrides everything - use only for global cutoffs.");
    GD.Print ( "    --no-editor-logging: Prevent double logging by disabling GD.Print when running from an IDE / CLI (optional, values: [true, false] default: false, ex: --no-editor-logging=true).");
    GD.Print ($"    --{helpFlagName}: Print all available flags (optional, ex: --{helpFlagName}).");
    GD.Print ("\n  Separate multiple flags with spaces, ex:");
    GD.Print ("\n    --log-console=Info --disable-screen-transitions=true --start-screen=Game");
    GD.Print ("\n  Unrecognized flags are ignored.\n");
    after?.Invoke();
    // @formatter:on
  }

  private static Dictionary <string, string> Parse (string helpFlagName)
  {
    var unparsed = OS.GetCmdlineArgs();
    if (unparsed.Any (x => string.Compare (x, $"--{helpFlagName}", StringComparison.OrdinalIgnoreCase) == 0)) return new Dictionary <string, string> { { $"{helpFlagName.ToLower()}", "" } };
    var parsed = new Dictionary <string, string>();

    foreach (var argument in unparsed)
    {
      if (argument.Find ("=") <= -1) continue;
      var keyValue = argument.Split ("=");
      parsed [keyValue [0].LStrip ("--").ToLower()] = keyValue [1].ToLower();
    }

    return parsed;
  }
}
