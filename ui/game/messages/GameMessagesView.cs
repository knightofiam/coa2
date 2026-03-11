using System.Threading.Tasks;
using com.forerunnergames.coa2.tools;
using com.forerunnergames.coa2.tools.events;
using com.forerunnergames.coa2.tools.events.args;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.messages;

public partial class GameMessagesView : Control
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private RichTextLabel _messageLabel = null!;
  private Tween? _currentTween;
  public override void _ExitTree() => EventBus.Instance.GameMessageReadyEvent -= OnGameMessageReadyEvent;
  private bool ShouldFadeOut() => _messageLabel.Modulate.A > 0.01f;
  private async Task WaitForTween (Tween tween) => await ToSignal (tween, Tween.SignalName.Finished);

  public override void _Ready()
  {
    _messageLabel = GetNode <RichTextLabel> ("%MessageLabel");
    EventBus.Instance.GameMessageReadyEvent += OnGameMessageReadyEvent;
  }

  // ReSharper disable once AsyncVoidMethod
  private void OnGameMessageReadyEvent (object? sender, GameMessageReadyEventArgs e) =>
    GodotThreadBridge.RunLater (async void () =>
    {
      await WaitForTween (ShowMessage (e.Message));
      EventBus.Emit (new GameMessageShownEventArgs (e.Message));
    });

  private Tween ShowMessage (GameMessage message)
  {
    _currentTween?.Kill();
    Log.Debug ("Game Message: '{text}' (duration: {duration}s)", message.Text, message.DurationSeconds);
    var (wasInstant, instantTween) = CheckShowInstantly (message);
    if (wasInstant) return instantTween!;
    var tween = CreateTween();
    CheckFadeOutAndUpdateLabel (tween, message);
    tween.TweenProperty (_messageLabel, "modulate:a", 1.0f, 0.5f);
    tween.TweenCallback (Callable.From (() => message.OnShow?.Invoke()));
    if (!CheckDuration (message, tween)) return tween;
    AddFadeOut (tween, message);
    _currentTween = tween;
    return tween;
  }

  private (bool wasInstant, Tween? instantTween) CheckShowInstantly (GameMessage message)
  {
    UpdateLabel (message);
    _messageLabel.Modulate = Colors.White;
    message.OnShow?.Invoke();
    var instantTween = CreateTween();
    var hasDuration = !Mathf.IsZeroApprox (message.DurationSeconds);

    instantTween.TweenCallback (Callable.From (() =>
    {
      if (!hasDuration) return;
      _messageLabel.Modulate = new Color (1.0f, 1.0f, 1.0f, 0.0f);
    }));

    _currentTween = instantTween;
    return (true, instantTween);
  }

  private void CheckFadeOutAndUpdateLabel (Tween tween, GameMessage message)
  {
    if (ShouldFadeOut())
    {
      FadeOutAndUpdateLabel (tween, message);
      return;
    }

    UpdateLabel (message);
  }

  private void FadeOutAndUpdateLabel (Tween tween, GameMessage message)
  {
    tween.TweenProperty (_messageLabel, "modulate:a", 0.0f, 0.3f);
    tween.TweenCallback (Callable.From (() => UpdateLabel (message)));
  }

  private void AddFadeOut (Tween tween, GameMessage message)
  {
    tween.TweenInterval (message.DurationSeconds);
    tween.TweenProperty (_messageLabel, "modulate:a", 0.0f, 0.3f);
  }

  private bool CheckDuration (GameMessage message, Tween tween)
  {
    if (!Mathf.IsZeroApprox (message.DurationSeconds)) return true;
    _currentTween = tween;
    return false;
  }

  private void UpdateLabel (GameMessage message)
  {
    _messageLabel.Modulate = new Color (1, 1, 1, 0);
    _messageLabel.Text = $"[center]{message.Text}[/center]";
  }
}
