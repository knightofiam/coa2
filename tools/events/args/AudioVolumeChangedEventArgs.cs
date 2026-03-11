namespace com.forerunnergames.coa2.tools.events.args;

public class AudioVolumeChangedEventArgs (float musicVolume, float sfxVolume) : EventBusEventArgs
{
  public float MusicVolume { get; } = musicVolume;
  public float SfxVolume { get; } = sfxVolume;
}
