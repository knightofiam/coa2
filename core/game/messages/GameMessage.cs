using System;

namespace com.forerunnergames.coa2.ui.messages;

public readonly struct GameMessage (string text, float durationSeconds, Action? onShow = null, Action? onComplete = null)
{
  public string Text { get; } = text;
  public float DurationSeconds { get; } = durationSeconds;
  public Action? OnShow { get; } = onShow;
  public Action? OnComplete { get; } = onComplete;
}
