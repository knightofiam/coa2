using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace com.forerunnergames.coa2.build;

public static class BuildInfo
{
  public static string Summary()
  {
    const string path = "res://build/build_info.json";
    if (!FileAccess.FileExists (path)) return "dev-local";
    using var f = FileAccess.Open (path, FileAccess.ModeFlags.Read);
    var dict = (Dictionary)Json.ParseString (f.GetAsText (false));
    var ver = dict.GetValueOrDefault ("version", "dev").AsString();
    var sha = dict.GetValueOrDefault ("commit", "local").AsString();
    var run = dict.GetValueOrDefault ("run", "").AsString();
    return string.IsNullOrEmpty (run) ? $"{ver}+{sha}" : $"{ver}+{sha} (#{run})";
  }
}
