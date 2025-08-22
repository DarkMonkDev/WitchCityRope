namespace WitchCityRope.E2E.Tests.Infrastructure;

public class TestSettings
{
    public string BaseUrl { get; set; } = "https://localhost:5654";
    public string BrowserType { get; set; } = "chromium";
    public bool Headless { get; set; } = true;
    public int SlowMo { get; set; } = 0;
    public int Timeout { get; set; } = 30000;
    public bool VideoRecording { get; set; } = false;
    public bool ScreenshotsOnFailure { get; set; } = true;
    public bool TraceOnFailure { get; set; } = true;
    public string Locale { get; set; } = "en-US";
    public string TimezoneId { get; set; } = "America/New_York";
    public int ViewportWidth { get; set; } = 1920;
    public int ViewportHeight { get; set; } = 1080;
}

public class TestDataSettings
{
    public string DefaultPassword { get; set; } = "Test123!@#";
    public string TestUserPrefix { get; set; } = "e2e_test_";
    public bool CleanupTestDataAfterRun { get; set; } = true;
}