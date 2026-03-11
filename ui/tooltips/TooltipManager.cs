using com.forerunnergames.coa2.core.settings;
using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.tooltips;

/// <summary>
/// Manages a single global tooltip instance for the entire application.
/// Tooltips are shown/hidden based on mouse hover without stealing focus or blocking clicks.
/// </summary>
public static class TooltipManager
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private static CustomTooltip? _tooltip;
  private static float _defaultDelay = 0.5f;
  public static void HideTooltip() => _tooltip?.HideTooltip();

  public static void Initialize (CustomTooltip tooltip, float defaultDelay = 0.5f)
  {
    _tooltip = tooltip;
    _defaultDelay = defaultDelay;
    Log.Debug ("TooltipManager initialized with default delay: {delay}s", defaultDelay);
  }

  public static void ShowTooltip (Node target, string text, float? delay = null)
  {
    if (Settings.Instance.DisableTooltips) return;
    if (!CheckTooltipValid()) return;
    _tooltip?.ShowTooltipFor (target, text, delay ?? _defaultDelay);
  }

  private static bool CheckTooltipValid()
  {
    if (_tooltip != null) return true;
    Log.Warn ("TooltipManager not initialized. Call Initialize() first.");
    return false;
  }
}
