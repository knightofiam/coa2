using GdUnit4;
using Godot;
using NLog;
using static GdUnit4.Assertions;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.tests.unit;

[TestSuite]
public partial class CanaryUnitTest : Node
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  [BeforeTest] public void BeforeTest() => Log.Info ("Before Canary Unit Test");
  [AfterTest] public void AfterTest() => Log.Info ("After Canary Unit Test");

  [TestCase]
  [RequireGodotRuntime]
  public void CanaryTest()
  {
    AssertBool (true);
    Log.Info ("Canary unit test completed successfully.");
  }
}
