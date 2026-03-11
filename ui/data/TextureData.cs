using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.data;

public static class TextureData
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  public static void Load() => Log.Debug ("Loaded texture data");
}
