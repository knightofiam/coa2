using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.tooltips;

/// <summary>
/// Attach this to any Control to add tooltip functionality.
/// The tooltip will show on mouse hover without stealing focus or blocking clicks.
/// </summary>
public partial class TooltipTrigger : Node
{
  [Export (PropertyHint.MultilineText)] public string TooltipText { get; set; } = string.Empty;
  [Export] public float TooltipDelay { get; set; } = 0.1f;
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private UI _ui = null!;
  private Node? _parent;
  public void SetTooltipText (string text) => TooltipText = text;

  public override void _Ready()
  {
    _ui = GetNode <UI> ("/root/UI");
    _parent = GetParent();
    if (!CheckParentControlValid()) return;
    if (_parent is not Control parent) return; // Disable automatic handling, but allow manually calling Show/Hide.
    SetupAutomaticHandling (parent);
  }

  public override void _ExitTree()
  {
    if (_parent is not Control parent) return;
    TearDownAutomaticHandling (parent);
  }

  public void SetTooltipVisible (bool isVisible)
  {
    if (isVisible)
    {
      ShowTooltip();
      return;
    }

    HideTooltip();
  }

  public void ShowTooltip()
  {
    if (!_ui.IsWindowFocused()) return;
    if (!CheckParentControlValid()) return;
    if (string.IsNullOrEmpty (TooltipText)) return;
    Log.Trace ("Manually showing tooltip for parent {parent}: {text}", _parent!.Name, TooltipText);
    TooltipManager.ShowTooltip (_parent, TooltipText, TooltipDelay);
  }

  public void HideTooltip()
  {
    if (string.IsNullOrEmpty (TooltipText) || _parent == null) return;
    Log.Trace ("Manually hiding tooltip for parent {parent}: {text}", _parent.Name, TooltipText);
    TooltipManager.HideTooltip();
  }

  private void OnParentMouseEntered()
  {
    if (!_ui.IsWindowFocused()) return;
    if (string.IsNullOrEmpty (TooltipText) || _parent == null) return;
    Log.Trace ("Mouse entered {parent}, showing tooltip: {text}", _parent.Name, TooltipText);
    TooltipManager.ShowTooltip (_parent, TooltipText, TooltipDelay);
  }

  private void OnParentMouseExited()
  {
    Log.Trace ("Mouse exited {parent}, hiding tooltip: {text}", _parent?.Name, TooltipText);
    TooltipManager.HideTooltip();
  }

  private bool CheckParentControlValid()
  {
    if (_parent != null) return true;
    Log.Error ("TooltipTrigger must be either 1) Child of a Control node for automatic tooltip handling, or 2) Child of Node / Node2D for manual control (i.e., calling Show & Hide directly)");
    return false;
  }

  private void OnParentGuiInput (InputEvent @event)
  {
    // Hide tooltip on any click
    if (@event is not InputEventMouseButton { Pressed: true }) return;
    Log.Trace ("Mouse clicked {parent}, hiding tooltip: {text}", _parent?.Name, TooltipText);
    TooltipManager.HideTooltip();
  }

  private void SetupAutomaticHandling (Control parent)
  {
    parent.MouseEntered += OnParentMouseEntered;
    parent.MouseExited += OnParentMouseExited;
    parent.GuiInput += OnParentGuiInput;
  }

  private void TearDownAutomaticHandling (Control parent)
  {
    parent.MouseEntered -= OnParentMouseEntered;
    parent.MouseExited -= OnParentMouseExited;
    parent.GuiInput -= OnParentGuiInput;
  }
}
