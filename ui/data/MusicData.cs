using System.Collections.Generic;
using com.forerunnergames.coa2.ui.audio.music;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.data;

public static class MusicData
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private static readonly HashSet <string> ValidMusicExtensions = ["mp3", "wav", "ogg"];
  private static Dictionary <MusicId, AudioStream> _musicIdsToAudioStreams = null!;
  public static AudioStream? GetMusic (MusicId? musicId) => musicId.HasValue ? _musicIdsToAudioStreams.GetValueOrDefault (musicId.Value) : null;

  // @formatter:off
  private static readonly Dictionary <MusicId, string> MusicPaths = new()
  {
    [MusicId.MainMenu] = "res://assets/music/music2.wav",
    [MusicId.Summer] = "res://assets/music/music5.wav",
    [MusicId.Winter] = "res://assets/music/music6.wav",
    [MusicId.GameOver] = ""
  };
  // @formatter:on

  public static void Load()
  {
    _musicIdsToAudioStreams = new Dictionary <MusicId, AudioStream>();
    foreach (var (id, path) in MusicPaths) LoadMusic (id, path);
    Log.Debug ("Loaded music data: {musicCount} music tracks", _musicIdsToAudioStreams.Count);
  }

  private static void LoadMusic (MusicId id, string path)
  {
    if (!CheckPathValid (path, id)) return;
    var stream = ResourceLoader.Load <AudioStream> (path);
    if (!CheckStreamValid (stream, path, id)) return;
    if (stream is AudioStreamMP3 mp3Stream) mp3Stream.Loop = true;
    _musicIdsToAudioStreams [id] = stream;
  }

  private static bool CheckStreamValid (AudioStream? stream, string path, MusicId id)
  {
    if (stream != null) return true;
    // Log.Error ("Failed to load Music [{musicId}], invalid audio stream at [{path}]", id, path); // TODO Uncomment when we have music.
    return false;
  }

  private static bool CheckPathValid (string path, MusicId id)
  {
    var extension = path.GetExtension().ToLowerInvariant();
    if (ValidMusicExtensions.Contains (extension) && ResourceLoader.Exists (path)) return true;
    // Log.Error ("Failed to load Music [{musicId}], invalid path [{path}]", id, path); // TODO Uncomment when we have music.
    return false;
  }
}
