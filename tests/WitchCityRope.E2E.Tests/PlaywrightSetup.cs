using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WitchCityRope.E2E.Tests;

[TestClass]
public static class PlaywrightSetup
{
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext context)
    {
        // Install Playwright browsers if not already installed
        var exitCode = Program.Main(new[] { "install" });
        if (exitCode != 0)
        {
            throw new Exception($"Playwright installation failed with exit code {exitCode}");
        }
    }

    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        // Cleanup any global resources if needed
    }
}