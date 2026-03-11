using System;
using System.Timers;

namespace com.forerunnergames.coa2.tools;

public class Timers
{
  public static void Delay (float? forSeconds = null, Action? then = null, bool callBackOnGodotThread = false)
  {
    if (CheckInstantCallback (forSeconds, then, callBackOnGodotThread)) return;
    var timer = new Timer();
    timer.Interval = TimeSpan.FromSeconds (forSeconds!.Value).TotalMilliseconds;
    timer.AutoReset = false;
    timer.Elapsed += (_, _) => OnDelayTimerOnElapsed (timer, then, callBackOnGodotThread);
    timer.Start();
  }

  private static bool CheckInstantCallback (float? forSeconds = null, Action? then = null, bool callBackOnGodotThread = false)
  {
    if (forSeconds.HasValue) return false;
    OnDelayTimerOnElapsed (timer: null, then, callBackOnGodotThread);
    return true;
  }

  private static void OnDelayTimerOnElapsed (Timer? timer, Action? then, bool callBackOnGodotThread)
  {
    timer?.Dispose();
    if (CheckRunLater (then, callBackOnGodotThread)) return;
    then?.Invoke();
  }

  private static bool CheckRunLater (Action? then, bool callBackOnGodotThread)
  {
    if (!callBackOnGodotThread) return false;
    GodotThreadBridge.RunLater (then);
    return true;
  }
}
