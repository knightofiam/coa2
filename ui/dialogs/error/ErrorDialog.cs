using com.forerunnergames.coa2.tools.events;
using com.forerunnergames.coa2.tools.events.args;
using com.forerunnergames.coa2.ui.audio.sfx;
using Godot;

namespace com.forerunnergames.coa2.ui.dialogs.error;

public partial class ErrorDialog : CanvasLayer
{
  private RichTextLabel _label = null!;
  private Button _okButton = null!;
  public override void _ExitTree() => EventBus.Instance.ErrorEvent -= OnErrorEvent;
  private void OnErrorEvent (object? sender, ErrorEventArgs e) => CallDeferred (MethodName.ShowDeferred, e.Message);
  private void OnOkButtonPressed() => Hide();

  public override void _Ready()
  {
    _label = GetNode <RichTextLabel> ("%RichTextLabel");
    _okButton = GetNode <Button> ("%OkButton");
    _okButton.Pressed += OnOkButtonPressed;
    ButtonSfx.AddClickAndHoverSfx (_okButton);
    EventBus.Instance.ErrorEvent += OnErrorEvent;
    Hide();
  }

  private void ShowDeferred (string message)
  {
    if (Visible) return;
    _label.Text = message;
    Show();
    EventBus.Emit (new DialogShownEventArgs());
  }

  private new void Hide()
  {
    if (!Visible) return;
    base.Hide();
    _label.Text = "";
    EventBus.Emit (new DialogHiddenEventArgs());
  }
}
