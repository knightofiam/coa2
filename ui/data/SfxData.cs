using System.Collections.Generic;
using com.forerunnergames.coa2.ui.audio;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.data;

public static class SfxData
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private static readonly HashSet <string> ValidSfxExtensions = ["mp3", "wav", "ogg"];
  private static Dictionary <SfxId, AudioStream> _sfxIdsToAudioStreams = null!;
  public static AudioStream? GetSfx (SfxId? sfx) => sfx.HasValue ? _sfxIdsToAudioStreams.GetValueOrDefault (sfx.Value) : null;

  // TODO Add SFX files.
  private static readonly Dictionary <SfxId, string> SfxPaths = new()
  {
    [SfxId.ButtonClick] = "",
    [SfxId.ButtonHover] = "",
    [SfxId.Error] = ""
  };

  public static void Load()
  {
    _sfxIdsToAudioStreams = new Dictionary <SfxId, AudioStream>();
    foreach (var (id, path) in SfxPaths) LoadSfx (id, path);
    Log.Debug ("Loaded SFX data: {sfxCount} SFX sounds", _sfxIdsToAudioStreams.Count);
  }

  private static void LoadSfx (SfxId id, string path)
  {
    if (!CheckPathValid (path, id)) return;
    var stream = ResourceLoader.Load <AudioStream> (path);
    if (!CheckStreamValid (stream, path, id)) return;
    _sfxIdsToAudioStreams [id] = stream;
  }

  private static bool CheckStreamValid (AudioStream? stream, string path, SfxId id)
  {
    if (stream != null) return true;
    // Log.Error ("Failed to load SFX [{sfxId}], invalid audio stream at [{path}]", id, path); // TODO Uncomment when we have SFX.
    return false;
  }

  private static bool CheckPathValid (string path, SfxId id)
  {
    var extension = path.GetExtension().ToLowerInvariant();
    if (ValidSfxExtensions.Contains (extension) && ResourceLoader.Exists (path)) return true;
    // Log.Error ("Failed to load SFX [{sfxId}], invalid path [{path}]", id, path); // TODO Uncomment when we have SFX.
    return false;
  }
}
