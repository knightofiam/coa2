using Godot;

namespace com.forerunnergames.coa2.ui.audio.sfx;

/// <summary>
/// Utility to add SFX to buttons
/// </summary>
public static class ButtonSfx
{
  public static void AddClickAndHoverSfx (BaseButton button)
  {
    button.MouseEntered += () => OnButtonHover (button);
    button.Pressed += () => OnButtonClick (button);
  }

  private static void OnButtonHover (BaseButton button)
  {
    if (button.Disabled) return;
    AudioManager.PlaySfx (SfxId.ButtonHover);
  }

  private static void OnButtonClick (BaseButton button)
  {
    if (button.Disabled) return;
    AudioManager.PlaySfx (SfxId.ButtonClick);
  }
}
