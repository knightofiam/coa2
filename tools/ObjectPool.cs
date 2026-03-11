using System;
using System.Collections.Concurrent;

namespace com.forerunnergames.coa2.tools;

// Used primarily for logging performance with Strings.ToString().
public class ObjectPool <T> (Func <T> createItem)
{
  private readonly ConcurrentStack <T> _pool = new();
  public T Get() => _pool.TryPop (out var item) ? item : createItem();
  public void Return (T item) => _pool.Push (item);
}
