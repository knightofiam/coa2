using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using com.forerunnergames.coa2.tools.events;
using com.forerunnergames.coa2.tools.events.args;
using com.forerunnergames.coa2.ui.messages;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.core.game.messages;

public class GameMessagesManager
{
  // @formatter:off
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private readonly ConcurrentQueue <GameMessage> _messageQueue = new();
  private readonly object _queueLock = new();
  private bool _isProcessingQueue;
  private TaskCompletionSource? _messageShownTcs;
  private void OnGameMessageRequestEvent (object? sender, GameMessageRequestEventArgs e) => AddMessage (e.Message);
  private bool ShouldStartProcessing() { lock (_queueLock) return !_isProcessingQueue; }
  private bool TryDequeueMessage (out GameMessage message) { lock (_queueLock) return _messageQueue.TryDequeue (out message); }
  private bool IsQueueEmpty() { lock (_queueLock) return _messageQueue.IsEmpty; }
  private void ClearProcessingFlag() { lock (_queueLock) _isProcessingQueue = false; }
  // @formatter:on

  public GameMessagesManager()
  {
    EventBus.Instance.GameMessageRequestEvent += OnGameMessageRequestEvent;
    EventBus.Instance.GameMessageShownEvent += OnGameMessageShownEvent;
  }

  ~GameMessagesManager()
  {
    EventBus.Instance.GameMessageRequestEvent -= OnGameMessageRequestEvent;
    EventBus.Instance.GameMessageShownEvent -= OnGameMessageShownEvent;
  }

  private void OnGameMessageShownEvent (object? sender, GameMessageShownEventArgs e)
  {
    e.Message.OnComplete?.Invoke();
    Interlocked.Exchange (ref _messageShownTcs, null)?.TrySetResult();
  }

  private void AddMessage (GameMessage message)
  {
    _messageQueue.Enqueue (message);
    Log.Debug ("Enqueued message: '{text}' (duration: {duration}s), queue size: {size}", message.Text, message.DurationSeconds, _messageQueue.Count);
    if (!ShouldStartProcessing()) return;
    Log.Trace ("Starting message queue processor");
    _ = ProcessMessagesAsync();
  }

  private async Task ProcessMessagesAsync()
  {
    // @formatter:off
    if (!TryStartMessageProcessing()) return;
    Log.Trace ("ProcessQueue started");
    while (await ProcessNextMessageAsync()) { }
    Log.Trace ("ProcessQueue finished - queue empty");
    // @formatter:on
  }

  private bool TryStartMessageProcessing()
  {
    lock (_queueLock)
    {
      if (_isProcessingQueue) return false;
      _isProcessingQueue = true;
      return true;
    }
  }

  private async Task <bool> ProcessNextMessageAsync()
  {
    if (CheckQueueEmpty()) return false;
    if (!TryDequeueMessage (out var message)) return true;
    Log.Trace ("Processing message: '{text}' (duration: {duration}s)", message.Text, message.DurationSeconds);
    var tcs = new TaskCompletionSource();
    _messageShownTcs = tcs;
    EventBus.Emit (new GameMessageReadyEventArgs (message));
    await tcs.Task;
    return true;
  }

  private bool CheckQueueEmpty()
  {
    if (!IsQueueEmpty()) return false;
    ClearProcessingFlag();
    return true;
  }
}
