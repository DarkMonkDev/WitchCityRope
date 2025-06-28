using System;
using System.Collections.Generic;

namespace WitchCityRope.PerformanceTests.Infrastructure
{
    public class PerformanceTestSettings
    {
        public string BaseUrl { get; set; } = "https://localhost:5001";
        public TimeSpan WarmupDuration { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan TestDuration { get; set; } = TimeSpan.FromMinutes(1);
        public List<ReportFormat> ReportFormats { get; set; } = new() { ReportFormat.Html, ReportFormat.Csv };
        public Dictionary<string, ScenarioSettings> Scenarios { get; set; } = new();
        public ThresholdSettings Thresholds { get; set; } = new();
        public TestUserSettings TestUsers { get; set; } = new();
    }

    public class ScenarioSettings
    {
        public int Rate { get; set; }
        public TimeSpan Duration { get; set; }
        public int KeepConstant { get; set; }
    }

    public class ThresholdSettings
    {
        public ResponseTimeThresholds ResponseTime { get; set; } = new();
        public ErrorRateThresholds ErrorRate { get; set; } = new();
        public ThroughputThresholds Throughput { get; set; } = new();
    }

    public class ResponseTimeThresholds
    {
        public double P50 { get; set; } = 100;
        public double P75 { get; set; } = 200;
        public double P95 { get; set; } = 500;
        public double P99 { get; set; } = 1000;
    }

    public class ErrorRateThresholds
    {
        public double MaxPercentage { get; set; } = 1.0;
    }

    public class ThroughputThresholds
    {
        public double MinRequestsPerSecond { get; set; } = 50;
    }

    public class TestUserSettings
    {
        public int Count { get; set; } = 1000;
        public string PasswordPattern { get; set; } = "TestUser@123";
    }

    public enum ReportFormat
    {
        Html,
        Csv,
        Json,
        Txt,
        Md
    }
}