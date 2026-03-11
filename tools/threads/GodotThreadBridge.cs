using System;
using System.Collections.Concurrent;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.tools;

/// <summary>
/// Bridge for executing actions on the Godot main thread from non-Godot code.
/// Use this when core code needs to eventually call Godot engine APIs.
/// </summary>
public partial class GodotThreadBridge : Node
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private readonly ConcurrentQueue <Action> _actions = new();
  private static GodotThreadBridge? _instance;

  public override void _Ready()
  {
    _instance = this;
    Log.Trace ("Initialized and ready to process actions");
  }

  // Process all pending actions on the main Godot thread
  public override void _Process (double delta)
  {
    while (_actions.TryDequeue (out var action)) Invoke (action);
  }

  /// <summary>
  /// Queue an action to run on the Godot main thread.
  /// The action will be executed during the next _Process frame.
  /// </summary>
  public static void RunLater (Action? action = null)
  {
    if (action == null) return;
    if (!CheckInitialized()) return;
    _instance!._actions.Enqueue (action);
  }

  private static void Invoke (Action action)
  {
    try
    {
      action.Invoke();
    }
    catch (Exception e)
    {
      Log.Error (e, "Error invoking action on Godot main thread");
    }
  }

  private static bool CheckInitialized()
  {
    if (_instance != null) return true;
    Log.Error ("Not initialized - cannot run action on main thread");
    return false;
  }
}
