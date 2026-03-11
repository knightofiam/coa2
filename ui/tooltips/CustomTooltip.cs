using Godot;
using NLog;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.ui.tooltips;

/// <summary>
/// A custom tooltip control that doesn't steal focus or require clicks to dismiss.
/// Tooltips are click-through and follow the mouse cursor.
/// </summary>
public partial class CustomTooltip : CanvasLayer
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  private Label _label = null!;
  private Timer _delayTimer = null!;
  private PanelContainer _panel = null!;
  private Node? _targetNode;
  private string _tooltipText = string.Empty;
  private Vector2 _offset = new(20, 20);

  public override void _Ready()
  {
    _panel = GetNode <PanelContainer> ("TooltipPanel");
    _label = GetNode <Label> ("%TooltipLabel");
    _delayTimer = GetNode <Timer> ("%DelayTimer");
    _delayTimer.Timeout += OnDelayTimeout;
    _panel.MouseFilter = Control.MouseFilterEnum.Ignore; // Make tooltip click-through
    _label.MouseFilter = Control.MouseFilterEnum.Ignore;
    _panel.Hide(); // Initially hide the tooltip
  }

  public override void _Process (double delta)
  {
    if (!_panel.Visible || _targetNode == null) return;
    UpdateTooltipPosition();
  }

  /// <summary>
  /// Show tooltip for a control after a delay
  /// </summary>
  public void ShowTooltipFor (Node target, string text, float delay = 0.5f)
  {
    if (string.IsNullOrEmpty (text)) return;
    Log.Debug ("ShowTooltipFor: target={target}, text length={len}, delay={delay}", target.Name, text.Length, delay);
    _targetNode = target;
    _tooltipText = text;
    // Reset both label and panel sizes before setting new text
    _label.Size = Vector2.Zero;
    _label.CustomMinimumSize = Vector2.Zero;
    _panel.Size = Vector2.Zero;
    _panel.CustomMinimumSize = Vector2.Zero;
    _label.Text = text;
    _delayTimer.WaitTime = delay;
    _delayTimer.Start();
  }

  /// <summary>
  /// Hide and cancel any pending tooltip
  /// </summary>
  public void HideTooltip()
  {
    _delayTimer.Stop();
    _targetNode = null;
    _panel.Hide();
    // Reset both label and panel sizes so they don't retain size from previous tooltip
    _label.Size = Vector2.Zero;
    _label.CustomMinimumSize = Vector2.Zero;
    _panel.Size = Vector2.Zero;
    _panel.CustomMinimumSize = Vector2.Zero;
  }

  private void OnDelayTimeout()
  {
    if (_targetNode == null || string.IsNullOrEmpty (_tooltipText)) return;
    Log.Debug ("OnDelayTimeout: Showing tooltip at mouse position");
    // Force both label and panel to recalculate size
    _label.Size = Vector2.Zero;
    _label.CustomMinimumSize = Vector2.Zero;
    _panel.Size = Vector2.Zero;
    _panel.CustomMinimumSize = Vector2.Zero;
    _panel.Show();
    Show();
    // Wait a frame for size to update, then position
    CallDeferred (MethodName.UpdateTooltipPosition);
  }

  private void UpdateTooltipPosition()
  {
    var mousePos = GetViewport().GetMousePosition();
    var tooltipSize = _panel.Size;
    var viewportSize = GetViewport().GetVisibleRect().Size;
    // Calculate which edges would be exceeded with default positioning
    var defaultPos = mousePos + _offset;
    var exceedsRight = defaultPos.X + tooltipSize.X > viewportSize.X;
    var exceedsBottom = defaultPos.Y + tooltipSize.Y > viewportSize.Y;
    // Position diagonally to avoid blocking the hover target near edges
    var tooltipPos = CalculateDiagonalPosition (mousePos, tooltipSize, exceedsRight, exceedsBottom);
    // Final safety clamp to viewport bounds
    tooltipPos.X = Mathf.Clamp (tooltipPos.X, 0, viewportSize.X - tooltipSize.X);
    tooltipPos.Y = Mathf.Clamp (tooltipPos.Y, 0, viewportSize.Y - tooltipSize.Y);
    _panel.GlobalPosition = tooltipPos;
  }

  private Vector2 CalculateDiagonalPosition (Vector2 mousePos, Vector2 tooltipSize, bool exceedsRight, bool exceedsBottom)
  {
    var xOffset = exceedsRight ? -tooltipSize.X - _offset.X : _offset.X; // Show left of cursor if exceeds right, otherwise right.
    var yOffset = exceedsBottom ? -tooltipSize.Y - _offset.Y : _offset.Y; // Show above cursor if exceeds bottom, otherwise below.
    return mousePos + new Vector2 (xOffset, yOffset);
  }
}
