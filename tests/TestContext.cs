using com.forerunnergames.coa2.core.data;
using com.forerunnergames.coa2.core.game;

namespace com.forerunnergames.coa2.tests;

/// <summary>
/// Test abstraction layer over GameData that allows swapping out components for testing.
/// Tests should use TestContext instead of GameData directly.
/// </summary>
public static class TestContext
{
  private static Game? _testGame;

  /// <summary>
  /// Game used for testing. Defaults to GameData.Game if not overridden.
  /// </summary>
  public static Game Game { get => _testGame ?? GameData.Game; set => _testGame = value; }

  /// <summary>
  /// Reset all test overrides back to GameData defaults.
  /// Call this in test teardown to ensure a clean slate.
  /// </summary>
  public static void Reset() => _testGame = null;
}
