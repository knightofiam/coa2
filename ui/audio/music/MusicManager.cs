using System.Threading.Tasks;
using com.forerunnergames.coa2.ui.audio.music;
using com.forerunnergames.coa2.ui.data;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.audio;

/// <summary>
/// Manages music playback with crossfading support.
/// Handles a single music track at a time with smooth transitions.
/// </summary>
public partial class MusicManager : Node
{
  public static MusicManager Instance { get; private set; } = null!;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private AudioStreamPlayer _musicPlayer = null!;
  private MusicId? _currentMusicId;
  private Tween? _activeTween;
  private float _targetVolumeDb;
  public static void PlayMusic (MusicId? id, bool loop = true) => Instance.PlayMusicInstance (id, loop);
  public static Task CrossfadeToMusic (MusicId? nextTrack, float duration = 0.2f) => Instance.CrossfadeToMusicInstance (nextTrack, duration);
  public static void SetVolume (float volumeDb) => Instance.SetVolumeInstance (volumeDb);
  private bool IsPlaying (MusicId? musicId) => musicId.HasValue && _currentMusicId == musicId.Value && _musicPlayer.Playing;
  private bool HasCurrentMusic() => _currentMusicId.HasValue && IsInstanceValid (_musicPlayer) && _musicPlayer.Playing;
  private static float LinearToDb (float linear) => linear > 0.0f ? Mathf.LinearToDb (linear) : -80.0f;

  public override void _Ready()
  {
    Instance = this;
    _musicPlayer = GetNode <AudioStreamPlayer> ("%MusicPlayer");
    Log.Debug ("MusicManager initialized");
  }

  public void Initialize (float volumeDb)
  {
    _targetVolumeDb = volumeDb;
    _musicPlayer.VolumeDb = volumeDb;
    Log.Debug ("MusicManager initialized with volume {volumeDb} dB", volumeDb);
  }

  private void SetVolumeInstance (float volumeDb)
  {
    Log.Debug ("SetVolume: Setting _targetVolumeDb and _musicPlayer.VolumeDb to {volumeDb} dB", volumeDb);
    _targetVolumeDb = volumeDb;
    if (!IsInstanceValid (_musicPlayer)) return;
    _musicPlayer.VolumeDb = volumeDb;
    Log.Debug ("Music player volume set to {volumeDb} dB", volumeDb);
  }

  private void PlayMusicInstance (MusicId? id, bool loop = true)
  {
    if (IsPlaying (id)) return;
    if (!CheckMusicValid (id, out var stream)) return;
    _musicPlayer.Stream = stream;
    _musicPlayer.VolumeDb = _targetVolumeDb; // Ensure music starts at target volume
    _musicPlayer.Play();
    _currentMusicId = id;
    Log.Debug ("Playing music track: {track}", id);
  }

  private async Task CrossfadeToMusicInstance (MusicId? nextTrack, float duration = 0.2f)
  {
    if (IsPlaying (nextTrack)) return;
    KillActiveTween();
    await ExecuteCrossfadePath (nextTrack, duration);
  }

  private async Task ExecuteCrossfadePath (MusicId? nextTrack, float duration)
  {
    var hasCurrentMusic = HasCurrentMusic();
    var hasNextMusic = nextTrack.HasValue;
    LogCrossfadeState (nextTrack, hasCurrentMusic, hasNextMusic, duration);

    // ReSharper disable once ConvertIfStatementToSwitchStatement
    if (hasCurrentMusic && !hasNextMusic)
    {
      await HandleFadeOutOnly (duration);
      return;
    }

    if (!hasCurrentMusic && hasNextMusic)
    {
      await HandleFadeInOnly (nextTrack!.Value, duration);
      return;
    }

    if (hasCurrentMusic && hasNextMusic) await HandleCrossfade (nextTrack!.Value, duration);
  }

  private void LogCrossfadeState (MusicId? nextTrack, bool hasCurrentMusic, bool hasNextMusic, float duration)
  {
    Log.Debug ("CrossfadeToMusic: current={current}, next={next}, hasCurrentMusic={hasCurrent}, hasNextMusic={hasNext}, duration={dur}s", _currentMusicId?.ToString() ?? "None", nextTrack?.ToString() ?? "None", hasCurrentMusic, hasNextMusic, duration);
  }

  private async Task HandleFadeOutOnly (float duration)
  {
    Log.Debug ("Taking FadeOutCurrentMusic path");
    await FadeOutCurrentMusic (duration);
  }

  private async Task HandleFadeInOnly (MusicId nextTrack, float duration)
  {
    Log.Debug ("Taking FadeInNewMusic path");
    await FadeInNewMusic (nextTrack, duration);
  }

  private async Task HandleCrossfade (MusicId nextTrack, float duration)
  {
    Log.Debug ("Taking CrossfadeBetweenTracks path");
    await CrossfadeBetweenTracks (nextTrack, duration);
  }

  private void StopMusicInstance()
  {
    _musicPlayer.Stop();
    _currentMusicId = null;
    Log.Debug ("Music stopped");
  }

  private async Task FadeOutCurrentMusic (float duration)
  {
    _activeTween = GetTree().CreateTween();
    _activeTween.TweenProperty (_musicPlayer, "volume_db", -80.0f, duration);
    await GetTree().ToSignal (_activeTween, Tween.SignalName.Finished);
    _musicPlayer.Stop();
    _musicPlayer.VolumeDb = _targetVolumeDb;
    _currentMusicId = null;
    Log.Debug ("Faded out music");
  }

  private async Task FadeInNewMusic (MusicId id, float duration)
  {
    if (!CheckMusicValid (id, out var stream)) return;
    _musicPlayer.Stream = stream;
    _musicPlayer.VolumeDb = -80.0f;
    _musicPlayer.Play();
    _currentMusicId = id;
    _activeTween = GetTree().CreateTween();
    _activeTween.TweenProperty (_musicPlayer, "volume_db", _targetVolumeDb, duration);
    await GetTree().ToSignal (_activeTween, Tween.SignalName.Finished);
    Log.Debug ("Faded in music track: {track}", id);
  }

  private async Task CrossfadeBetweenTracks (MusicId nextId, float duration)
  {
    if (!CheckMusicValid (nextId, out var stream)) return;
    CleanupAudioPlayers(); // Clean up any orphaned sibling audio players from interrupted crossfades
    var fadeOutPlayer = CreateFadeOutPlayer(); // Create temporary player for fade-out while main player handles fade-in
    _musicPlayer.AddSibling (fadeOutPlayer);
    fadeOutPlayer.Play();
    fadeOutPlayer.Seek (_musicPlayer.GetPlaybackPosition());
    _musicPlayer.Stop();
    _musicPlayer.Stream = stream; // Switch main player to new track immediately
    _musicPlayer.VolumeDb = -80.0f;
    _musicPlayer.Play();
    _currentMusicId = nextId;
    await CrossFadeTweenAsync (fadeOutPlayer, duration);
    Log.Debug ("Crossfaded to music track: {track} (simultaneous fade over {duration}s)", nextId, duration);
  }

  private async Task CrossFadeTweenAsync (AudioStreamPlayer fadeOutPlayer, float duration)
  {
    // Crossfade tween: fade out temp player while fading in main player simultaneously
    _activeTween = GetTree().CreateTween();
    _activeTween.SetParallel();
    _activeTween.TweenProperty (fadeOutPlayer, "volume_db", -80.0f, duration);
    _activeTween.TweenProperty (_musicPlayer, "volume_db", _targetVolumeDb, duration);
    await GetTree().ToSignal (_activeTween, Tween.SignalName.Finished);
    fadeOutPlayer.Stop();
    fadeOutPlayer.CallDeferred ("queue_free"); // Clean up the temporary fade-out player
  }

  private void CleanupAudioPlayers()
  {
    if (!IsInstanceValid (_musicPlayer)) return;
    var parent = _musicPlayer.GetParent();
    if (parent == null) return;

    foreach (var child in parent.GetChildren())
    {
      if (child is not AudioStreamPlayer player || player == _musicPlayer || !IsInstanceValid (player)) continue;
      Log.Debug ("Cleaning up orphaned audio player");
      player.Stop();
      player.QueueFree();
    }
  }

  private AudioStreamPlayer CreateFadeOutPlayer() =>
    new()
    {
      Stream = _musicPlayer.Stream,
      VolumeDb = _musicPlayer.VolumeDb,
      Bus = _musicPlayer.Bus ?? "Master",
      StreamPaused = false
    };

  private void KillActiveTween()
  {
    if (_activeTween == null || !IsInstanceValid (_activeTween)) return;
    _activeTween.Kill();
    _activeTween = null;
  }

  private static bool CheckMusicValid (MusicId? musicId, out AudioStream? stream)
  {
    stream = AudioData.GetMusic (musicId);
    if (stream != null) return true;
    // Log.Error ("Music track not found: {track}", musicId); // TODO Uncomment when we have music.
    return false;
  }
}
