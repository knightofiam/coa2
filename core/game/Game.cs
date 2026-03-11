using com.forerunnergames.coa2.tools.events;
using com.forerunnergames.coa2.tools.events.args;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.core.game;

public class Game
{
  public bool IsPaused { get; private set; }
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  public void OnGameOver() { } // TODO Implement
  public bool IsGameOver() => false; // TODO Implement
  private void ResetGame() => IsPaused = false;

  public void StartGame()
  {
    Log.Info ("New game started");
    EventBus.Emit (new GameStartedEventArgs());
    ResetGame();
  }

  public void Pause()
  {
    if (IsPaused) return;
    IsPaused = true;
    Log.Debug ("Game paused");
    EventBus.Emit (new GamePausedEventArgs());
  }

  public void Resume()
  {
    if (!IsPaused) return;
    IsPaused = false;
    Log.Debug ("Game resumed");
    EventBus.Emit (new GameResumedEventArgs());
  }
}
