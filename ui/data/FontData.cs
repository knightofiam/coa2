using System.Collections.Generic;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.data;

public static class FontData
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private static readonly HashSet <string> ValidFontExtensions = ["ttf", "otf", "fnt"];
  private static readonly string ButtonFontPath = "";
  private static Font? _buttonFontCached;
  public static Font? GetButtonFont() => _buttonFontCached;

  // TODO Add font files.
  public static void Load()
  {
    if (!CheckPathValid (ButtonFontPath)) return;
    _buttonFontCached = ResourceLoader.Load <Font> (ButtonFontPath);
    Log.Debug ("Loaded font data");
  }

  private static bool CheckPathValid (string path)
  {
    var extension = path.GetExtension().ToLowerInvariant();
    if (ValidFontExtensions.Contains (extension) && ResourceLoader.Exists (path)) return true;
    // Log.Error ("Failed to load font, invalid path [{path}]", path); // TODO Uncomment when we have fonts.
    return false;
  }
}
