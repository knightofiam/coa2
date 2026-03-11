using com.forerunnergames.coa2.ui.audio;
using com.forerunnergames.coa2.ui.audio.music;
using com.forerunnergames.coa2.ui.screens;
using Godot;

namespace com.forerunnergames.coa2.ui.data;

/// <summary>
/// Centralized audio data facade.
/// Delegates to MusicData and SfxData for implementation.
/// </summary>
public static class AudioData
{
  public static AudioStream? GetMusic (MusicId? musicId) => MusicData.GetMusic (musicId);
  public static AudioStream? GetSfx (SfxId? sfx) => SfxData.GetSfx (sfx);
  public static MusicId? GetMusicId (ScreenId screenId) => MusicData.GetMusicId (screenId);

  public static void Load()
  {
    MusicData.Load();
    SfxData.Load();
  }
}
