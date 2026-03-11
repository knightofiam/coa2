using com.forerunnergames.coa2.ui.screens.context;
using Godot;

namespace com.forerunnergames.coa2.ui.screens;

public interface IScreen
{
  ScreenId ScreenId { get; }
  Control AsControl();
  void OnScreenActive (ScreenContext? screenContext);
}
