using System.Threading.Tasks;
using Godot;

namespace com.forerunnergames.coa2.ui.effects.screenFade;

public partial class ScreenFade : CanvasLayer
{
  [Export] public float TimeSeconds { get; set; } = 0.35f;
  private ColorRect _colorRect = null!;

  public override void _Ready()
  {
    _colorRect = GetNode <ColorRect> ("ColorRect");
    Hide();
  }

  public async Task FadeOut (float? duration = null)
  {
    var fadeDuration = duration ?? TimeSeconds;
    if (fadeDuration <= 0.0f)
    {
      Show();
      var modulate = _colorRect.Modulate;
      modulate.A = 1.0f;
      _colorRect.Modulate = modulate;
      return;
    }
    Show();
    var tween = CreateTween();
    var startModulate = _colorRect.Modulate;
    startModulate.A = 0.0f;
    _colorRect.Modulate = startModulate;
    tween.TweenProperty (_colorRect, "modulate:a", 1.0f, fadeDuration);
    await ToSignal (tween, Tween.SignalName.Finished);
  }

  public async Task FadeIn (float? duration = null)
  {
    var fadeDuration = duration ?? TimeSeconds;
    if (fadeDuration <= 0.0f)
    {
      var modulate = _colorRect.Modulate;
      modulate.A = 0.0f;
      _colorRect.Modulate = modulate;
      Hide();
      return;
    }
    var tween = CreateTween();
    var startModulate = _colorRect.Modulate;
    startModulate.A = 1.0f;
    _colorRect.Modulate = startModulate;
    tween.TweenProperty (_colorRect, "modulate:a", 0.0f, fadeDuration);
    await ToSignal (tween, Tween.SignalName.Finished);
    Hide();
  }
}
