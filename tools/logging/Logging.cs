using System;
using System.Linq;
using System.Text;
using Godot;
using NLog;
using NLog.Config;
using NLog.MessageTemplates;
using NLog.Targets;

namespace com.forerunnergames.coa2.tools.logging;

public static class Logging
{
  public static readonly LogLevel DefaultLogLevel = LogLevel.Info;
  public static bool IsLogLevelFlagSet { get; set; }
  public static LoggingRule? GetConsoleLogRule (Target? target) => LogManager.Configuration.LoggingRules.FirstOrDefault (r => r.Targets.Contains (target));
  private static Target? GetConsoleLogTarget (string name) => LogManager.Configuration.AllTargets.FirstOrDefault (x => x.Name == name);

  public static void DisableStringQuoting()
  {
    // Register the formatter in the LogFactory's service repository
    var logFactory = LogManager.LogFactory;
    var original = logFactory.ServiceRepository.GetService (typeof (IValueFormatter)) as IValueFormatter;
    var stringQuoting = new StringQuotingFormatter (original) { IsQuotingStrings = false };
    logFactory.ServiceRepository.RegisterService (typeof (IValueFormatter), stringQuoting);
    // Force reload configuration from file to recreate all targets with the new formatter
    logFactory.Configuration = logFactory.Configuration.Reload();
    LogManager.ReconfigExistingLoggers();
  }

  public static LogLevel? GetMinConsoleLogLevel()
  {
    var rule = GetConsoleLogRule (GetConsoleLogTarget ("console")); // "GodotConsole" mirrors "console" level, so we ignore it here.
    return rule == null ? null : LogLevel.AllLevels.FirstOrDefault (rule.IsLoggingEnabledForLevel) ?? LogLevel.Off;
  }

  // "GodotConsole" level will always mirror "console" level, if it exists.
  public static void SetMinConsoleLogLevel (LogLevel level)
  {
    var config = LogManager.Configuration;
    if (config == null) return;
    // Only modify catch-all rules (pattern "*"), not specific logger rules
    // This allows specific rules with final="true" to set maximum verbosity limits per logger
    config.LoggingRules.Where (r => r.LoggerNamePattern == "*").Where (rule => rule.Targets.Any (t => t.Name is "console" or "GodotConsole")).ToList().ForEach (rule => rule.SetLoggingLevels (level, LogLevel.Fatal));
    LogManager.ReconfigExistingLoggers();
    // Use GD.Print here so we can always print the log level regardless of level filtering.
    GD.Print ($"{nameof (Logging)}: Set min console log level to [{level}]");
  }

  public static void SetMinFileLogLevel (LogLevel level)
  {
    var config = LogManager.Configuration;
    if (config == null) return;
    // Only modify catch-all rules (pattern "*"), not specific logger rules
    // This allows specific rules with final="true" to set maximum verbosity limits per logger
    config.LoggingRules.Where (r => r.LoggerNamePattern == "*").Where (rule => rule.Targets.Any (t => t.Name == "file")).ToList().ForEach (rule => rule.SetLoggingLevels (level, LogLevel.Fatal));
    LogManager.ReconfigExistingLoggers();
    // Use GD.Print here so we can always print the log level regardless of level filtering.
    GD.Print ($"{nameof (Logging)}: Set min file log level to [{level}]");
  }

  public static void SetGlobalThresholdLogLevel (LogLevel level)
  {
    LogManager.GlobalThreshold = level;
    GD.Print ($"{nameof (Logging)}: Set global log threshold to [{level}]");
  }

  public static void RegisterGodotConsoleTarget()
  {
    var config = LogManager.Configuration;
    if (config == null) return;
    LogManager.Setup().SetupExtensions (e => e.RegisterTarget <GodotConsoleLoggingTarget> ("GodotConsole"));
    var godotConsoleTarget = new GodotConsoleLoggingTarget { Layout = "${newline}${longdate} ${logger} ${uppercase:${level}} ${message} ${exception:format=tostring}" };
    config.AddTarget ("GodotConsole", godotConsoleTarget);
    // Copy rules from console target to GodotConsole instead of logging all levels
    config.LoggingRules.Where (r => r.Targets.Any (t => t.Name == "console")).ToList().ForEach (rule => rule.Targets.Add (godotConsoleTarget));
    LogManager.ReconfigExistingLoggers();
  }

  // See https://github.com/NLog/NLog/issues/3556
  private class StringQuotingFormatter (IValueFormatter? originalFormatter) : IValueFormatter
  {
    public required bool IsQuotingStrings { get; init; }

    public bool FormatValue (object value, string format, CaptureType captureType, IFormatProvider formatProvider, StringBuilder builder)
    {
      // Always handle strings without quotes when IsQuotingStrings is false
      if (!IsQuotingStrings && value is string stringValue)
      {
        builder.Append (stringValue);
        return true;
      }

      if (!IsQuotingStrings && value is char charValue)
      {
        builder.Append (charValue);
        return true;
      }

      // For everything else, use the original formatter if available
      if (originalFormatter != null) return originalFormatter.FormatValue (value, format, captureType, formatProvider, builder);
      builder.Append (value?.ToString() ?? string.Empty); // Fallback: just append ToString()
      return true;
    }
  }
}
