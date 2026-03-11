using NLog;
using NLog.Targets;
using Godot;

namespace com.forerunnergames.coa2.tools.logging;

[Target ("GodotConsole")]
public sealed class GodotConsoleLoggingTarget : TargetWithLayout
{
  protected override void Write (LogEventInfo logEvent)
  {
    var logMessage = Layout.Render (logEvent);

    switch (logEvent.Level.Name.ToLower())
    {
      case "warn":
      {
        GD.PushWarning (logMessage);
        GD.Print (logMessage);
        break;
      }
      case "error" or "fatal":
      {
        GD.PushError (logMessage);
        GD.PrintErr (logMessage);
        break;
      }
      default:
      {
        GD.Print (logMessage);
        break;
      }
    }
  }
}
