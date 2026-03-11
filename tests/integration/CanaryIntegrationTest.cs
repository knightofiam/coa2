using GdUnit4;
using NLog;
using static GdUnit4.Assertions;
using Logger = NLog.Logger;

namespace com.forerunnergames.coa2.tests.integration;

[TestSuite]
[RequireGodotRuntime]
public class CanaryIntegrationTest
{
  private static readonly Logger Log = LogManager.GetCurrentClassLogger();
  [BeforeTest] public void BeforeTest() => Log.Info ("Before Canary Integration Test");
  [AfterTest] public void AfterTest() => Log.Info ("After Canary Integration Test");

  [TestCase]
  public void CanaryTest()
  {
    AssertBool (true);
    Log.Info ("Canary integration test completed successfully.");
  }
}
