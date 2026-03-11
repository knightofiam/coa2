using System;
using com.forerunnergames.coa2.tools.events.args;

namespace com.forerunnergames.coa2.tools.events;

// IMPORTANT: READ BEFORE USING THE EVENT BUS!
//
// GODOT THREAD SAFETY REQUIREMENTS & MEMORY LEAK PREVENTION
//
// 1) When handling an event callback from the event bus, always use CallDeferred to
// marshall back to the main Godot thread from the C# event thread, before
// attempting to make any engine calls. For example:
//
//   public partial class MyGodotEngineClass : Node2D
//   {
//     private Sprite2D _myGodotSprite = null!;
//
//     // We must unsubscribe before this class instance is garbage collected in order to prevent memory leaks.
//     public override void _ExitTree() => EventBus.Instance.MyEventBusEvent -= OnMyEventBusEventCallback;
//
//     public override void _Ready()
//     {
//       _myGodotSprite = GetNode <Sprite2D> ("%MyGodotSprite");
//       EventBus.Instance.MyEventBusEvent += OnMyEventBusEventCallback;
//     }
//
//     // We arrive here on the C# event thread, NOT the Godot thread.
//     private void OnMyEventBusEventCallback (object? sender, MyEventBusEventArgs e)
//     {
//        // Also be aware that CallDeferred gets called during the SAME engine process frame.
//        // If you need to wait until the next frame, use the second approach below.
//        CallDeferred (MethodName.ShowSprite, e.MyIntParam, e.MyGodotVector2Param); // Variant-compatible parameters only.
//     }
//
//     // We're back on the Godot thread because the caller used CallDeferred.
//     private void ShowSprite (int myIntParam, Vector2 myGodotVector2Param)
//     {
//       _myGodotSprite.Show(); // Fails if we're not on the Godot thread.
//     }
//   }
//
// 2) If you have Variant-incompatible parameters, you can't
// use CallDeferred without serialization, so as an alternative, you can use:
//
//   await ToSignal (GetTree(), SceneTree.SignalName.ProcessFrame);
//
// which ultimately achieves the same result of marshalling back to the Godot thread. For example:
//
//   public partial class MyGodotEngineClass : Node2D
//   {
//     private Sprite2D _myGodotSprite = null!;
//
//     // We must unsubscribe before this class instance is garbage collected in order to prevent memory leaks.
//     public override void _ExitTree() => EventBus.Instance.MyEventBusEvent -= OnMyEventBusEventCallback;
//
//     public override void _Ready()
//     {
//       _myGodotSprite = GetNode <Sprite2D> ("%MyGodotSprite");
//       EventBus.Instance.MyEventBusEvent += OnMyEventBusEventCallback;
//     }
//
//     // We arrive here on the C# event thread, NOT the Godot thread.
//     //
//     // We must use an async signature here, but because it's an event callback, we use
//     // 'async void' instead of 'async Task', to keep subscription & unsubscription simple, using
//     // the +/- approach with the method name.
//     //
//     // If we use async Task, we'd have to wrap it in an inline anonymous function like:
//     //
//     //   EventBus.Instance.MyEventBusEvent += (sender, e) => _ = OnMyEventBusEventCallback (sender, e);
//     //
//     // which breaks unsubscription because we don't have a reference to that anonymous function instance,
//     // i.e., this doesn't work:
//     //
//     //   EventBus.Instance.MyEventBusEvent -= (sender, e) => _ = OnMyEventBusEventCallback (sender, e);
//     //
//     private async void OnMyEventBusEventCallback (object? sender, MyEventBusEventArgs e)
//     {
//        // Unlike CallDeferred, this is guaranteed to NOT run in the same frame.
//        await ToSignal (GetTree(), SceneTree.SignalName.ProcessFrame); // After this call, we will be back on the Godot thread.
//        ShowSprite (e.MyCustomParam1, e.MyCustomParam2); // Variant-incompatible parameters prevent using the CallDeferred approach.
//     }
//
//     // The caller used await...ProcessFrame, so we're back on the engine thread here.
//     private void ShowSprite (MyCustomParamType1 myCustomParam1, MyCustomParamType2 myCustomParam2)
//     {
//       _myGodotSprite.Show(); // Fails if we're not on the Godot thread.
//     }
//   }
//
// 3) If your event callback is in a non-engine class, disregard all of the above, but
// be extremely careful that nothing in your call stack eventually calls an engine method.
// If it does, you'll have to incorporate one of the marshalling approaches above to the first Godot method
// encountered in your call stack - which may not be anywhere near your original event bus callback method. For example:
//
//   public class MyVanillaCSharpClass (MyGodotClass myGodotClass)
//   {
//     private readonly MyGodotClass _myGodotClass = myGodotClass;
//     public MyVanillaCSharpClass() => EventBus.Instance.MyEventBusEvent += OnMyEventBusEventCallback;
//
//     // We must unsubscribe before this class instance is garbage collected in order to prevent memory leaks.
//     public void Cleanup() => EventBus.Instance.MyEventBusEvent -= OnMyEventBusEventCallback;
//
//     // We arrive here on the C# event thread, NOT the Godot thread.
//     // This is NOT a Godot class, but we will eventually make an engine call from here.
//     private void OnMyEventBusEventCallback (object? sender, MyEventBusEventArgs e)
//     {
//        // We can't use CallDeferred or await...ProcessFrame here because we're in a non-engine class.
//        MyVanillaCSharpMethod (e.MyCustomParam1, e.MyCustomParam2);
//     }
//
//     // We arrive here on the C# event thread, NOT the Godot thread.
//     // This is NOT a Godot class, but we eventually make an engine call from here.
//     private void MyVanillaCSharpMethod (MyCustomParamType1 myCustomParam1, MyCustomParamType2 myCustomParam2)
//     {
//        // Eventually makes an engine call.
//        _myGodotClass.SomeMethodCalledFromEventBusCallback (myCustomParam1, myCustomParam2);
//     }
//   }
//
//   public partial class MyGodotClass : Node2D
//   {
//     private Sprite2D _myGodotSprite = null!;
//     public override void _Ready() => _myGodotSprite = GetNode <Sprite2D> ("%MyGodotSprite");
//
//     // We arrive here on the C# event thread, NOT the Godot thread.
//     // We must marshall back to the Godot thread, or ShowSprite() will fail.
//     private void SomeMethodCalledFromEventBusCallback (MyCustomParamType1 myCustomParam1, MyCustomParamType2 myCustomParam2)
//     {
//       // We make some other non-engine calls here, so we don't encounter any problems yet.
//         ...
//       // Eventually, we make an engine call.
//       // Since we're back in a Godot class, we can use CallDeferred.
//       CallDeferred (MethodName.ShowSprite);
//     }
//
//     // The caller used CallDeferred, so we're back on the engine thread here.
//     private void ShowSprite()
//     {
//       _myGodotSprite.Show(); // Fails if we're not on the Godot thread.
//     }
//   }
public class EventBus
{
  public static readonly EventBus Instance = new();
  public event EventHandler <LoggingInitializedEventArgs>? LoggingInitializedEvent;
  public event EventHandler <ErrorEventArgs>? ErrorEvent;
  public event EventHandler <AudioVolumeChangedEventArgs>? AudioVolumeChangedEvent;
  public event EventHandler <GameMessageRequestEventArgs>? GameMessageRequestEvent;
  public event EventHandler <GameMessageReadyEventArgs>? GameMessageReadyEvent;
  public event EventHandler <GameMessageShownEventArgs>? GameMessageShownEvent;
  public event EventHandler <DialogShownEventArgs>? DialogShownEvent;
  public event EventHandler <DialogHiddenEventArgs>? DialogHiddenEvent;
  public event EventHandler <GamePausedEventArgs>? GamePausedEvent;
  public event EventHandler <GameResumedEventArgs>? GameResumedEvent;
  public event EventHandler <GameStartedEventArgs>? GameStartedEvent;
  public event EventHandler <GameOverEventArgs>? GameOverEvent;

  public static void Emit (EventBusEventArgs eventArgs)
  {
    switch (eventArgs)
    {
      case LoggingInitializedEventArgs args:
      {
        Instance.LoggingInitializedEvent?.Invoke (Instance, args);
        break;
      }
      case ErrorEventArgs args:
      {
        Instance.ErrorEvent?.Invoke (Instance, args);
        break;
      }
      case AudioVolumeChangedEventArgs args:
      {
        Instance.AudioVolumeChangedEvent?.Invoke (Instance, args);
        break;
      }
      case GameMessageRequestEventArgs args:
      {
        Instance.GameMessageRequestEvent?.Invoke (Instance, args);
        break;
      }
      case GameMessageReadyEventArgs args:
      {
        Instance.GameMessageReadyEvent?.Invoke (Instance, args);
        break;
      }
      case GameMessageShownEventArgs args:
      {
        Instance.GameMessageShownEvent?.Invoke (Instance, args);
        break;
      }
      case DialogShownEventArgs args:
      {
        Instance.DialogShownEvent?.Invoke (Instance, args);
        break;
      }
      case DialogHiddenEventArgs args:
      {
        Instance.DialogHiddenEvent?.Invoke (Instance, args);
        break;
      }
      case GamePausedEventArgs args:
      {
        Instance.GamePausedEvent?.Invoke (Instance, args);
        break;
      }
      case GameResumedEventArgs args:
      {
        Instance.GameResumedEvent?.Invoke (Instance, args);
        break;
      }
      case GameStartedEventArgs args:
      {
        Instance.GameStartedEvent?.Invoke (Instance, args);
        break;
      }
      case GameOverEventArgs args:
      {
        Instance.GameOverEvent?.Invoke (Instance, args);
        break;
      }
      default:
      {
        throw new ArgumentException ($"Unhandled event bus EventArgs type : {eventArgs.GetType()}");
      }
    }
  }
}
