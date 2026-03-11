using System;

namespace com.forerunnergames.coa2.ui.screens.context;

// Designed to pass data between screens that get disposed after each use.
public sealed class ScreenContext
{
  public Action? TransitionAction { get; init; } // Runs after the new screen is current and just before OnScreenActive is called on the new screen. Not called if the screen change fails.
}
