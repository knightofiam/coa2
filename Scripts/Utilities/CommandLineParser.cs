using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa.utilities;

public class CommandLineParser (string helpFlagName)
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private readonly Dictionary <string, string> _parsed = Parse (helpFlagName);
  public bool IsSet (string flagName) => _parsed.ContainsKey (flagName);

  public T TryGet <T> (string flagName, T or)
  {
    if (!_parsed.ContainsKey (flagName.ToLower())) return or;

    var name = flagName.ToLower();
    var value = _parsed[name];

    try
    {
      return typeof (T) == typeof (LogLevel)
        ? (T)(object)LogLevel.FromString (value)
        : (T?)TypeDescriptor.GetConverter (typeof (T)).ConvertFrom (value) ?? or;
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
    // Use GD.Print to force printing instead of logging, in case logging is off.
    GD.Print ("\nCommand line help:");
    GD.Print ("\n  Flag format:");
    GD.Print ("\n    --name=value (where \'name\' is the setting name, & \'value\' is the setting value.)");
    GD.Print ("\n  Flags are NOT case sensitive:");
    GD.Print ("\n    --NamE=VaLue --NAME=value --name=value are all equivalent.");
    GD.Print ("\n  Available flags:\n");
    GD.Print ($"    --log: Minimum console log level (optional, values: [{Strings.ToString (LogLevel.AllLevels)}] default: {LogLevel.Info}, ex: --log={LogLevel.Trace}).");
    GD.Print ($"    --log-global-threshold: Global log threshold level (optional, values: [{Strings.ToString (LogLevel.AllLevels)}] default: {LogLevel.Info}, ex: --log={LogLevel.Trace}).");
    GD.Print ("    --no-editor-logging: Prevent double logging by disabling GD.Print when running from an IDE / CLI, default: false, ex: --no-editor-logging=true).");
    GD.Print ($"    --{helpFlagName}: Print all available flags (optional, ex: --{helpFlagName}).");
    GD.Print ("\n  Separate multiple flags with spaces, ex:");
    GD.Print ($"\n    --log=Info --log={LogLevel.Info}");
    GD.Print ("\n  Unrecognized flags are ignored.\n");
    after?.Invoke();
  }

  public bool ShouldShowHelp() => _parsed.ContainsKey (helpFlagName);

  private static Dictionary <string, string> Parse (string helpFlagName)
  {
    var unparsed = OS.GetCmdlineArgs();

    if (unparsed.Any (x => string.Compare (x, $"--{helpFlagName}", StringComparison.OrdinalIgnoreCase) == 0))
    {
      return new Dictionary <string, string> { { $"{helpFlagName.ToLower()}", "" } };
    }

    var parsed = new Dictionary <string, string>();

    foreach (var argument in unparsed)
    {
      if (argument.Find ("=") <= -1) continue;
      var keyValue = argument.Split ("=");
      parsed[keyValue[0].LStrip ("--").ToLower()] = keyValue[1].ToLower();
    }

    return parsed;
  }
}
