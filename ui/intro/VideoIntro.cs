using System.Threading.Tasks;
using com.forerunnergames.coa2.core.settings;
using com.forerunnergames.coa2.ui.audio;
using com.forerunnergames.coa2.ui.audio.music;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.intro;

public partial class VideoIntro : CanvasLayer
{
  [Export] public float VideoLengthSeconds = 13.0f;
  [Export] public float EarlyFadeOutSeconds = 2.0f;
  [Signal] public delegate void VideoFinishedEventHandler();
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private VideoStreamPlayer _videoPlayer = null!;
  private ColorRect _background = null!;
  private UI _ui = null!;
  private bool _isFadingOut;
  private bool _hasEmittedFinished;
  private double _playStartTime;

  public override void _Ready()
  {
    _ui = GetNode <UI> ("/root/UI");
    _videoPlayer = GetNode <VideoStreamPlayer> ("VideoStreamPlayer");
    _background = GetNode <ColorRect> ("Background");
    _videoPlayer.Volume = 0; // Mute & play separate audio track.
    _videoPlayer.Finished += OnVideoFinished;
  }

  public override void _Process (double delta)
  {
    if (!Visible)
    {
      if (_videoPlayer.IsPlaying()) Log.Trace ("_Process: Video playing but layer not visible");
      return;
    }

    if (!_videoPlayer.IsPlaying())
    {
      Log.Trace ("_Process: Layer visible but video not playing, isFadingOut={fading}", _isFadingOut);
      return;
    }

    if (_isFadingOut) return;

    // Check if we're close to the end and should start fading out
    var currentPosition = _videoPlayer.StreamPosition;
    var timeRemaining = VideoLengthSeconds - currentPosition;

    // Log every 2 seconds to track progress without spamming
    var secondsPlayed = (int)currentPosition;

    if (secondsPlayed % 2 == 0 && currentPosition - secondsPlayed < delta * 2)
    {
      Log.Trace ("Video progress: position={pos:F1}s, remaining={rem:F1}s (fadeAt={fadeAt:F1}s)", currentPosition, timeRemaining, VideoLengthSeconds - EarlyFadeOutSeconds);
    }

    if (!(timeRemaining <= EarlyFadeOutSeconds)) return;
    Log.Info ("Triggering early fade-out at position={pos:F2}s (remaining={rem:F2}s)", currentPosition, timeRemaining);
    _ = FadeOutEarlyAsync();
  }

  // Allow skipping the video with any key press, mouse click, or gamepad button
  // Ignore input for first 1 second to prevent spurious events from triggering skip
  public override void _Input (InputEvent @event)
  {
    if (!Visible) return;
    if (@event is not (InputEventKey or InputEventMouseButton or InputEventJoypadButton)) return;
    if (!@event.IsPressed()) return;
    var timeSinceStart = Time.GetTicksMsec() / 1000.0 - _playStartTime;

    if (timeSinceStart < 1.0)
    {
      Log.Trace ("Ignoring input event during grace period: {eventType}, time since start: {time:F2}s", @event.GetType().Name, timeSinceStart);
      return; // Ignore input for first second
    }

    Log.Trace ("Input event triggered skip: {eventType}, time since start: {time:F2}s", @event.GetType().Name, timeSinceStart);
    Skip();
  }

  public void Play()
  {
    if (!CheckStreamValid()) return;
    if (CheckShouldSkip()) return;
    _isFadingOut = false;
    _playStartTime = Time.GetTicksMsec() / 1000.0;
    Show();
    _ui.HideTopBar(); // Hide TopBar during video
    _videoPlayer.StreamPosition = 2.0f;
    _videoPlayer.Play();
    AudioManager.PlayMusic (MusicId.MainMenu);
    _ = FadeInAsync();
    Log.Debug ("Playing intro video with music, fading in from black");
  }

  private async Task FadeInAsync()
  {
    // Start completely black (alpha = 0)
    _background.Modulate = new Color (1, 1, 1, 0);
    _videoPlayer.Modulate = new Color (1, 1, 1, 0);
    // Fade in over 0.5 seconds
    var tween = CreateTween();
    tween.SetParallel();
    tween.TweenProperty (_background, "modulate:a", 1.0f, 0.5f);
    tween.TweenProperty (_videoPlayer, "modulate:a", 1.0f, 0.5f);
    await ToSignal (tween, Tween.SignalName.Finished);
    Log.Trace ("Video fade-in completed");
  }

  private void Skip()
  {
    Log.Info ("Skipping intro video");
    _videoPlayer.Stop();
    OnVideoFinished();
  }

  private void OnVideoFinished()
  {
    if (_hasEmittedFinished) return; // Prevent double emission
    _hasEmittedFinished = true;
    Hide();
    // TopBar will be shown when Hub screen fades in, not here
    EmitSignal (SignalName.VideoFinished);
    Log.Info ("Intro video finished");
  }

  private bool CheckStreamValid()
  {
    if (_videoPlayer.Stream != null) return true;
    // Log.Error ("Invalid video stream"); // TODO Uncomment when we have a video intro.
    Skip();
    return false;
  }

  private bool CheckShouldSkip()
  {
    if (Settings.Instance.PlayIntroVideo) return false;
    Log.Debug ("Intro video disabled in settings, skipping");
    Skip();
    return true;
  }

  private async Task FadeOutEarlyAsync()
  {
    _isFadingOut = true;
    Log.Trace ("Starting early video fade out");

    // Emit signal immediately to start loading next screen while fading
    if (!_hasEmittedFinished)
    {
      _hasEmittedFinished = true;
      EmitSignal (SignalName.VideoFinished);
      Log.Trace ("Emitted VideoFinished signal at start of fade");
    }

    var tween = CreateTween();
    tween.SetParallel();
    tween.TweenProperty (_background, "modulate:a", 0.0f, EarlyFadeOutSeconds);
    tween.TweenProperty (_videoPlayer, "modulate:a", 0.0f, EarlyFadeOutSeconds);
    await ToSignal (tween, Tween.SignalName.Finished);
    Hide();
  }
}
