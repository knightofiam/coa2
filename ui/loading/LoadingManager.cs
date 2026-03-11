using com.forerunnergames.coa2.ui.data;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.loading;

/// <summary>
/// Manages all data loading operations for the game.
/// </summary>
public partial class LoadingManager : Node
{
  public static LoadingManager Instance { get; private set; } = null!;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private UI _ui = null!;

  public override void _Ready()
  {
    Instance = this;
    _ui = GetParent <UI>();
    Log.Debug ("LoadingManager initialized");
  }

  public static void PreloadData()
  {
    StyleData.Load();
    FontData.Load();
    AudioData.Load();
    TextureData.Load();
  }
}
