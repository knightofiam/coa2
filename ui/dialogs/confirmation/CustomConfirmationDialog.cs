using System;
using com.forerunnergames.coa2.tools.events;
using com.forerunnergames.coa2.tools.events.args;
using com.forerunnergames.coa2.ui.audio.sfx;
using Godot;

namespace com.forerunnergames.coa2.ui.dialogs.confirmation;

public partial class CustomConfirmationDialog : CanvasLayer
{
  private RichTextLabel _messageLabel = null!;
  private Label _titleLabel = null!;
  private Button _confirmButton = null!;
  private Button _cancelButton = null!;
  private Action? _onConfirm;
  private Action? _onCancel;

  public override void _Ready()
  {
    _messageLabel = GetNode <RichTextLabel> ("%MessageLabel");
    _titleLabel = GetNode <Label> ("%TitleLabel");
    _confirmButton = GetNode <Button> ("%ConfirmButton");
    _cancelButton = GetNode <Button> ("%CancelButton");
    _confirmButton.Pressed += OnConfirmButtonPressed;
    _cancelButton.Pressed += OnCancelButtonPressed;
    ButtonSfx.AddClickAndHoverSfx (_confirmButton);
    ButtonSfx.AddClickAndHoverSfx (_cancelButton);
    Hide();
  }

  public override void _ExitTree()
  {
    _confirmButton.Pressed -= OnConfirmButtonPressed;
    _cancelButton.Pressed -= OnCancelButtonPressed;
  }

  public void Show (string title, string message, string confirmText, string cancelText, Action? onConfirm = null, Action? onCancel = null)
  {
    if (Visible) return;
    _titleLabel.Text = title;
    _messageLabel.Text = message;
    _confirmButton.Text = confirmText;
    _cancelButton.Text = cancelText;
    _onConfirm = onConfirm;
    _onCancel = onCancel;
    base.Show();
    EventBus.Emit (new DialogShownEventArgs());
  }

  private void OnConfirmButtonPressed()
  {
    var callback = _onConfirm;
    Hide();
    callback?.Invoke();
  }

  private void OnCancelButtonPressed()
  {
    var callback = _onCancel;
    Hide();
    callback?.Invoke();
  }

  private new void Hide()
  {
    if (!Visible) return;
    base.Hide();
    _titleLabel.Text = "";
    _messageLabel.Text = "";
    _onConfirm = null;
    _onCancel = null;
    EventBus.Emit (new DialogHiddenEventArgs());
  }
}
