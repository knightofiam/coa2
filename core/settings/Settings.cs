using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using com.forerunnergames.coa2.tools.logging;
using com.forerunnergames.coa2.ui.screens;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.core.settings;

public class Settings
{
  // @formatter:off

  public static Settings Instance => _instance ??= Load();
  public static GameSettings GameSettingsSnapshot => new();
  public float MusicVolume { get; set; } = 1.0f; // 0.0 to 1.0
  public float SfxVolume { get; set; } = 1.0f; // 0.0 to 1.0
  public float PreMuteMusicVolume { get; set; } = 1.0f; // 0.0 to 1.0 - Stores volume before muting
  public float PreMuteSfxVolume { get; set; } = 1.0f; // 0.0 to 1.0 - Stores volume before muting
  // ReSharper disable RedundantDefaultMemberInitializer
  public bool IsMusicMuted { get; set; } = false; // Stores mute state
  public bool IsSfxMuted { get; set; } = false; // Stores mute state
  public bool PlayIntroVideo { get; set; } = true;
  public bool DisableScreenTransitions { get; set; } = false;
  public bool DisableTooltips { get; set; } = false;
  public bool ScreenShakeEnabled { get; set; } = true;
  // ReSharper restore RedundantDefaultMemberInitializer
  [JsonConverter (typeof (JsonStringEnumConverter))]
  public ScreenId? StartScreen { get; set; }
  public string ConsoleLogLevel { get; set; } = "Info"; // Off, Trace, Debug, Info, Warn, Error, Fatal
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private static readonly string SettingsFilePath = Path.Combine (OS.GetUserDataDir(), "settings.json");
  private static Settings? _instance;
  // @formatter:on

  public static void Reload()
  {
    Log.Debug ("=== RELOADING SETTINGS ===");
    _instance = Load();
    Log.Debug ("=== SETTINGS RELOADED ===");
  }

  public void Save()
  {
    try
    {
      Log.Debug ("=== SAVING SETTINGS ===");
      Log.Debug ("  Game Settings:");
      Log.Debug ("  Audio Settings:");
      Log.Debug ("    MusicVolume: {musicVolume}", MusicVolume);
      Log.Debug ("    IsMusicMuted: {isMusicMuted}", IsMusicMuted);
      Log.Debug ("    PreMuteMusicVolume: {preMuteMusicVolume}", PreMuteMusicVolume);
      Log.Debug ("    SfxVolume: {sfxVolume}", SfxVolume);
      Log.Debug ("    IsSfxMuted: {isSfxMuted}", IsSfxMuted);
      Log.Debug ("    PreMuteSfxVolume: {preMuteSfxVolume}", PreMuteSfxVolume);
      Log.Debug ("  UI Settings:");
      Log.Debug ("  Logging Settings:");
      Log.Debug ("    ConsoleLogLevel: {logLevel}", ConsoleLogLevel);
      var json = JsonSerializer.Serialize (this, new JsonSerializerOptions { WriteIndented = true });
      File.WriteAllText (SettingsFilePath, json);
      Log.Debug ("Settings saved successfully to {path}", SettingsFilePath);
      Log.Debug ("=== SETTINGS SAVED ===");
    }
    catch (Exception e)
    {
      Log.Error (e, "Failed to save settings to {path}", SettingsFilePath);
    }
  }

  private static Settings Load()
  {
    try
    {
      if (File.Exists (SettingsFilePath)) return LoadFromFile();
    }
    catch (Exception e)
    {
      Log.Error (e, "Failed to load settings from {path}, using defaults", SettingsFilePath);
    }

    return LoadDefaultSettings();
  }

  private static Settings LoadFromFile()
  {
    Log.Debug ("Loading settings from file: {path}", SettingsFilePath);
    var json = File.ReadAllText (SettingsFilePath);
    Log.Debug ("JSON content: {json}", json);
    var settings = JsonSerializer.Deserialize <Settings> (json) ?? new Settings();
    Log.Debug ("=== SETTINGS LOADED FROM FILE ===");
    Log.Debug ("  Game Settings:");
    Log.Debug ("  Audio Settings:");
    Log.Debug ("    MusicVolume: {musicVolume}", settings.MusicVolume);
    Log.Debug ("    IsMusicMuted: {isMusicMuted}", settings.IsMusicMuted);
    Log.Debug ("    PreMuteMusicVolume: {preMuteMusicVolume}", settings.PreMuteMusicVolume);
    Log.Debug ("    SfxVolume: {sfxVolume}", settings.SfxVolume);
    Log.Debug ("    IsSfxMuted: {isSfxMuted}", settings.IsSfxMuted);
    Log.Debug ("    PreMuteSfxVolume: {preMuteSfxVolume}", settings.PreMuteSfxVolume);
    Log.Debug ("  Logging Settings:");
    Log.Debug ("    ConsoleLogLevel: {logLevel}", settings.ConsoleLogLevel);
    Log.Debug ("  UI Settings:");
    Log.Debug ("  Debug Settings:");
    Log.Debug ("    StartScreen: {startScreen}", settings.StartScreen);
    Log.Debug ("    DisableScreenTransitions: {disableScreenTransitions}", settings.DisableScreenTransitions);
    return settings;
  }

  private static Settings LoadDefaultSettings()
  {
    Log.Debug ("Using default settings");
    return new Settings();
  }

  public void ApplyLogLevel()
  {
    var level = LogLevel.FromString (ConsoleLogLevel);

    if (level == null)
    {
      Log.Warn ("Invalid console log level '{logLevel}', defaulting to Info", ConsoleLogLevel);
      level = Logging.DefaultLogLevel;
    }

    Log.Info ("Changing console log level to: {logLevel}", level);
    Logging.SetMinConsoleLogLevel (level);
  }
}
