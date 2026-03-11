using System.Threading.Tasks;
using com.forerunnergames.coa2.ui.data;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.audio;

/// <summary>
/// Manages sound effects playback with multiple concurrent players.
/// Uses 5 players in round-robin to support overlapping sounds.
/// </summary>
public partial class SfxManager : Node
{
  public static SfxManager Instance { get; private set; } = null!;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private AudioStreamPlayer _sfxPlayer1 = null!;
  private AudioStreamPlayer _sfxPlayer2 = null!;
  private AudioStreamPlayer _sfxPlayer3 = null!;
  private AudioStreamPlayer _sfxPlayer4 = null!;
  private AudioStreamPlayer _sfxPlayer5 = null!;
  private int _lastSfxPlayerIndex;
  private AudioStreamPlayer? _lastHoverSfxPlayer;
  private float _targetVolumeDb;
  public static void PlaySfx (SfxId sfx, float pitch = 1.0f, float delay = 0.0f, float volume = 1.0f) => _ = Instance.PlaySfxInstance (sfx, pitch, delay, volume);
  public static void SetVolume (float volumeDb) => Instance.SetVolumeInstance (volumeDb);
  private AudioStreamPlayer[] GetSfxPlayers() => [_sfxPlayer1, _sfxPlayer2, _sfxPlayer3, _sfxPlayer4, _sfxPlayer5];
  private static float LinearToDb (float linear) => linear > 0.0f ? Mathf.LinearToDb (linear) : -80.0f;

  public override void _Ready()
  {
    Instance = this;
    _sfxPlayer1 = GetNode <AudioStreamPlayer> ("%SfxPlayer1");
    _sfxPlayer2 = GetNode <AudioStreamPlayer> ("%SfxPlayer2");
    _sfxPlayer3 = GetNode <AudioStreamPlayer> ("%SfxPlayer3");
    _sfxPlayer4 = GetNode <AudioStreamPlayer> ("%SfxPlayer4");
    _sfxPlayer5 = GetNode <AudioStreamPlayer> ("%SfxPlayer5");
    Log.Debug ("SfxManager initialized");
  }

  public void Initialize (float volumeDb)
  {
    _targetVolumeDb = volumeDb;
    Log.Debug ("SfxManager initialized with volume {volumeDb} dB", volumeDb);
  }

  private void SetVolumeInstance (float volumeDb)
  {
    Log.Debug ("SetVolume: Setting _targetVolumeDb to {volumeDb} dB (will be applied per-sound when PlaySfx is called)", volumeDb);
    _targetVolumeDb = volumeDb;
  }

  private async Task PlaySfxInstance (SfxId sfx, float pitch = 1.0f, float delay = 0.0f, float volume = 1.0f)
  {
    Log.Trace ("PlaySfxInstance: SfxId={sfx}, pitch={pitch}, delay={delay}, volume={volume} (linear)", sfx, pitch, delay, volume);
    if (!CheckSfxValid (sfx, out var stream)) return;
    if (sfx is SfxId.ButtonHover) _lastHoverSfxPlayer?.Stop(); // Stop previous hover sound to prevent overlap when hovering quickly
    if (delay > 0) await GetTree().ToSignal (GetTree().CreateTimer (delay), SceneTreeTimer.SignalName.Timeout);
    ConfigureAndPlaySfx (stream, pitch, volume, sfx);
  }

  private void ConfigureAndPlaySfx (AudioStream? stream, float pitch, float volume, SfxId sfx)
  {
    if (stream == null) return;
    var player = NextSfxPlayer();
    // Combine per-sound volume multiplier with global SFX volume
    // volume parameter is a multiplier (usually 1.0), _targetVolumeDb is the global volume in dB
    // Convert multiplier to dB, then add to global volume (dB scale is logarithmic, so adding is correct)
    var perSoundVolumeDb = LinearToDb (volume);
    var finalVolumeDb = _targetVolumeDb + perSoundVolumeDb;
    Log.Debug ("ConfigureAndPlaySfx: SfxId={sfx}, GlobalSfxVolume={globalDb} dB, PerSoundMultiplier={volume} ({perSoundDb} dB), FinalVolume={finalDb} dB", sfx, _targetVolumeDb, volume, perSoundVolumeDb, finalVolumeDb);
    player.Stream = stream;
    player.PitchScale = pitch;
    player.VolumeDb = finalVolumeDb;
    player.Play();
    if (sfx is not SfxId.ButtonHover) return; // Track hover sound player so we can stop it for the next hover
    _lastHoverSfxPlayer = player;
  }

  private AudioStreamPlayer NextSfxPlayer()
  {
    var players = GetSfxPlayers();
    return TryGetAvailablePlayer (players) ?? GetNextPlayerRoundRobin (players);
  }

  private AudioStreamPlayer? TryGetAvailablePlayer (AudioStreamPlayer[] players)
  {
    for (var i = 0; i < players.Length; i++)
    {
      var index = (_lastSfxPlayerIndex + i + 1) % players.Length;
      if (players [index].IsPlaying()) continue;
      _lastSfxPlayerIndex = index;
      return players [index];
    }

    return null;
  }

  private AudioStreamPlayer GetNextPlayerRoundRobin (AudioStreamPlayer[] players)
  {
    _lastSfxPlayerIndex = (_lastSfxPlayerIndex + 1) % players.Length;
    return players [_lastSfxPlayerIndex];
  }

  private static bool CheckSfxValid (SfxId sfx, out AudioStream? stream)
  {
    stream = AudioData.GetSfx (sfx);
    if (stream != null) return true;
    // Log.Warn ("SFX sound not found: {sfx}", sfx); // TODO Uncomment when we have SFX.
    return false;
  }
}
