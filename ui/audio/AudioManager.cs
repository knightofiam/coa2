using System.Threading.Tasks;
using com.forerunnergames.coa2.core.settings;
using com.forerunnergames.coa2.tools.events;
using com.forerunnergames.coa2.tools.events.args;
using com.forerunnergames.coa2.ui.audio.music;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.audio;

/// <summary>
/// Coordinates audio management between MusicManager and SfxManager.
/// Provides a unified interface for all audio operations.
/// </summary>
public partial class AudioManager : Node
{
  public static AudioManager Instance { get; private set; } = null!;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private MusicManager _musicManager = null!;
  private SfxManager _sfxManager = null!;
  public override void _ExitTree() => EventBus.Instance.AudioVolumeChangedEvent -= OnAudioVolumeChanged;
  public static void PlaySfx (SfxId sfx, float pitch = 1.0f, float delay = 0.0f, float volume = 1.0f) => SfxManager.PlaySfx (sfx, pitch, delay, volume);
  public static void PlayMusic (MusicId? id, bool loop = true) => MusicManager.PlayMusic (id, loop);
  public static Task CrossfadeToMusic (MusicId? nextTrack, float duration = 0.2f) => MusicManager.CrossfadeToMusic (nextTrack, duration);
  private static float LinearToDb (float linear) => linear > 0.0f ? Mathf.LinearToDb (linear) : -80.0f;

  public override void _Ready()
  {
    Instance = this;
    _musicManager = GetNode <MusicManager> ("%MusicManager");
    _sfxManager = GetNode <SfxManager> ("%SfxManager");
    EventBus.Instance.AudioVolumeChangedEvent += OnAudioVolumeChanged;
    Initialize();
    Log.Debug ("AudioManager initialized");
  }

  private void Initialize()
  {
    var musicVolumeDb = LinearToDb (Settings.Instance.MusicVolume);
    var sfxVolumeDb = LinearToDb (Settings.Instance.SfxVolume);
    _musicManager.Initialize (musicVolumeDb);
    _sfxManager.Initialize (sfxVolumeDb);
    Log.Debug ("AudioManager initialized with MusicVolume={musicVolumeDb}dB, SfxVolume={sfxVolumeDb}dB", musicVolumeDb, sfxVolumeDb);
  }

  private static void OnAudioVolumeChanged (object? sender, AudioVolumeChangedEventArgs e)
  {
    Log.Debug ("AudioManager.OnAudioVolumeChanged: Received event with MusicVolume={musicVolume}, SfxVolume={sfxVolume}", e.MusicVolume, e.SfxVolume);
    var musicVolumeDb = LinearToDb (e.MusicVolume);
    var sfxVolumeDb = LinearToDb (e.SfxVolume);
    MusicManager.SetVolume (musicVolumeDb);
    SfxManager.SetVolume (sfxVolumeDb);
  }
}
