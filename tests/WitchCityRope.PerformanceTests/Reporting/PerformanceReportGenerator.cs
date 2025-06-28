using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using NBomber.Contracts;
using NBomber.Contracts.Stats;

namespace WitchCityRope.PerformanceTests.Reporting
{
    public class PerformanceReportGenerator
    {
        private readonly string _reportPath;

        public PerformanceReportGenerator(string reportPath = "reports")
        {
            _reportPath = reportPath;
            Directory.CreateDirectory(_reportPath);
        }

        public void GenerateReport(NodeStats stats, Dictionary<string, object>? customMetrics = null)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var reportDir = Path.Combine(_reportPath, timestamp);
            Directory.CreateDirectory(reportDir);

            // Generate multiple report formats
            GenerateHtmlReport(stats, Path.Combine(reportDir, "report.html"), customMetrics);
            GenerateJsonReport(stats, Path.Combine(reportDir, "report.json"), customMetrics);
            GenerateMarkdownReport(stats, Path.Combine(reportDir, "report.md"), customMetrics);
            GenerateCsvReport(stats, Path.Combine(reportDir, "report.csv"));

            // Check for performance regressions
            var regressionReport = CheckForRegressions(stats);
            if (regressionReport.HasRegressions)
            {
                File.WriteAllText(Path.Combine(reportDir, "regressions.txt"), regressionReport.ToString());
            }
        }

        private void GenerateHtmlReport(NodeStats stats, string filePath, Dictionary<string, object> customMetrics)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<title>Performance Test Report</title>");
            html.AppendLine(@"
                <style>
                    body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }
                    .container { max-width: 1200px; margin: auto; background: white; padding: 20px; }
                    .header { background: #333; color: white; padding: 20px; margin: -20px -20px 20px; }
                    .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 20px; margin: 20px 0; }
                    .metric-card { background: #f8f9fa; padding: 20px; border-radius: 8px; border: 1px solid #dee2e6; }
                    .metric-value { font-size: 32px; font-weight: bold; color: #333; }
                    .metric-label { color: #666; margin-top: 5px; }
                    .scenario { margin: 30px 0; padding: 20px; background: #f8f9fa; border-radius: 8px; }
                    .scenario h3 { margin-top: 0; }
                    table { width: 100%; border-collapse: collapse; margin: 20px 0; }
                    th, td { padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }
                    th { background: #f8f9fa; font-weight: bold; }
                    .pass { color: #28a745; }
                    .fail { color: #dc3545; }
                    .warning { color: #ffc107; }
                    .chart { margin: 20px 0; height: 300px; background: #f8f9fa; border: 1px solid #ddd; display: flex; align-items: center; justify-content: center; }
                </style>
            ");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div class='container'>");
            
            // Header
            html.AppendLine("<div class='header'>");
            html.AppendLine($"<h1>Performance Test Report</h1>");
            html.AppendLine($"<p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine($"<p>Test Duration: {stats.Duration}</p>");
            html.AppendLine("</div>");

            // Summary metrics
            html.AppendLine("<div class='summary'>");
            html.AppendLine($@"
                <div class='metric-card'>
                    <div class='metric-value'>{stats.AllOkCount:N0}</div>
                    <div class='metric-label'>Successful Requests</div>
                </div>
                <div class='metric-card'>
                    <div class='metric-value'>{stats.AllFailCount:N0}</div>
                    <div class='metric-label'>Failed Requests</div>
                </div>
                <div class='metric-card'>
                    <div class='metric-value'>{stats.AllBytes / 1024.0 / 1024.0:F2} MB</div>
                    <div class='metric-label'>Data Transferred</div>
                </div>
                <div class='metric-card'>
                    <div class='metric-value'>{(double)stats.AllFailCount / (stats.AllOkCount + stats.AllFailCount) * 100:F2}%</div>
                    <div class='metric-label'>Error Rate</div>
                </div>
            ");
            html.AppendLine("</div>");

            // Scenario details
            foreach (var scenario in stats.ScenarioStats)
            {
                html.AppendLine($"<div class='scenario'>");
                html.AppendLine($"<h3>{scenario.ScenarioName}</h3>");
                
                html.AppendLine("<table>");
                html.AppendLine("<tr><th>Metric</th><th>Value</th><th>Status</th></tr>");
                
                // Response times
                // Mean is not directly available, using P50 instead
                html.AppendLine($"<tr><td>P50 Response Time</td><td>{scenario.Ok.Latency.Percent50:F2}ms</td><td class='{GetStatusClass(scenario.Ok.Latency.Percent50, 100)}'>{GetStatus(scenario.Ok.Latency.Percent50, 100)}</td></tr>");
                html.AppendLine($"<tr><td>P75 Response Time</td><td>{scenario.Ok.Latency.Percent75:F2}ms</td><td class='{GetStatusClass(scenario.Ok.Latency.Percent75, 200)}'>{GetStatus(scenario.Ok.Latency.Percent75, 200)}</td></tr>");
                html.AppendLine($"<tr><td>P95 Response Time</td><td>{scenario.Ok.Latency.Percent95:F2}ms</td><td class='{GetStatusClass(scenario.Ok.Latency.Percent95, 500)}'>{GetStatus(scenario.Ok.Latency.Percent95, 500)}</td></tr>");
                html.AppendLine($"<tr><td>P99 Response Time</td><td>{scenario.Ok.Latency.Percent99:F2}ms</td><td class='{GetStatusClass(scenario.Ok.Latency.Percent99, 1000)}'>{GetStatus(scenario.Ok.Latency.Percent99, 1000)}</td></tr>");
                html.AppendLine($"<tr><td>Max Response Time</td><td>{scenario.Ok.Latency.MaxMs:F2}ms</td><td>-</td></tr>");
                
                // Throughput
                html.AppendLine($"<tr><td>Requests/Second</td><td>{scenario.Ok.Request.RPS:F2}</td><td class='{GetStatusClass(scenario.Ok.Request.RPS, 50, true)}'>{GetStatus(scenario.Ok.Request.RPS, 50, true)}</td></tr>");
                html.AppendLine($"<tr><td>Success Rate</td><td>{100.0 - scenario.Fail.Request.Percent:F2}%</td><td class='{GetStatusClass(100.0 - scenario.Fail.Request.Percent, 99, true)}'>{GetStatus(100.0 - scenario.Fail.Request.Percent, 99, true)}</td></tr>");
                
                html.AppendLine("</table>");
                html.AppendLine("</div>");
            }

            // Custom metrics if provided
            if (customMetrics != null && customMetrics.Any())
            {
                html.AppendLine("<div class='scenario'>");
                html.AppendLine("<h3>Custom Metrics</h3>");
                html.AppendLine("<table>");
                html.AppendLine("<tr><th>Metric</th><th>Value</th></tr>");
                
                foreach (var metric in customMetrics)
                {
                    html.AppendLine($"<tr><td>{metric.Key}</td><td>{metric.Value}</td></tr>");
                }
                
                html.AppendLine("</table>");
                html.AppendLine("</div>");
            }

            html.AppendLine("</div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            File.WriteAllText(filePath, html.ToString());
        }

        private void GenerateJsonReport(NodeStats stats, string filePath, Dictionary<string, object>? customMetrics)
        {
            var report = new
            {
                TestInfo = new
                {
                    Timestamp = DateTime.Now,
                    Duration = stats.Duration.ToString(),
                    Status = stats.AllFailCount == 0 ? "Passed" : "Failed"
                },
                Summary = new
                {
                    TotalRequests = stats.AllOkCount + stats.AllFailCount,
                    SuccessfulRequests = stats.AllOkCount,
                    FailedRequests = stats.AllFailCount,
                    ErrorRate = (double)stats.AllFailCount / (stats.AllOkCount + stats.AllFailCount),
                    DataTransferredMB = stats.AllBytes / 1024.0 / 1024.0
                },
                Scenarios = stats.ScenarioStats.Select(s => new
                {
                    Name = s.ScenarioName,
                    Stats = new
                    {
                        Success = new
                        {
                            Count = s.Ok.Request.Count,
                            RPS = s.Ok.Request.RPS,
                            Latency = new
                            {
                                // Mean and StdDev are not available in LatencyStats
                                Min = s.Ok.Latency.MinMs,
                                P50 = s.Ok.Latency.Percent50,
                                P75 = s.Ok.Latency.Percent75,
                                P95 = s.Ok.Latency.Percent95,
                                P99 = s.Ok.Latency.Percent99,
                                Max = s.Ok.Latency.MaxMs
                            }
                        },
                        Failed = new
                        {
                            Count = s.Fail.Request.Count,
                            Percent = s.Fail.Request.Percent
                        }
                    }
                }),
                CustomMetrics = customMetrics
            };

            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        private void GenerateMarkdownReport(NodeStats stats, string filePath, Dictionary<string, object>? customMetrics)
        {
            var md = new StringBuilder();
            
            md.AppendLine("# Performance Test Report");
            md.AppendLine();
            md.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            md.AppendLine($"**Duration:** {stats.Duration}");
            md.AppendLine();
            
            md.AppendLine("## Summary");
            md.AppendLine();
            md.AppendLine("| Metric | Value |");
            md.AppendLine("|--------|-------|");
            md.AppendLine($"| Total Requests | {stats.AllOkCount + stats.AllFailCount:N0} |");
            md.AppendLine($"| Successful | {stats.AllOkCount:N0} |");
            md.AppendLine($"| Failed | {stats.AllFailCount:N0} |");
            md.AppendLine($"| Error Rate | {(double)stats.AllFailCount / (stats.AllOkCount + stats.AllFailCount) * 100:F2}% |");
            md.AppendLine($"| Data Transferred | {stats.AllBytes / 1024.0 / 1024.0:F2} MB |");
            md.AppendLine();

            foreach (var scenario in stats.ScenarioStats)
            {
                md.AppendLine($"## Scenario: {scenario.ScenarioName}");
                md.AppendLine();
                md.AppendLine("### Response Times");
                md.AppendLine();
                md.AppendLine("| Percentile | Time (ms) | Status |");
                md.AppendLine("|------------|-----------|--------|");
                // Mean is not directly available in LatencyStats
                md.AppendLine($"| P50 | {scenario.Ok.Latency.Percent50:F2} | {GetStatus(scenario.Ok.Latency.Percent50, 100)} |");
                md.AppendLine($"| P75 | {scenario.Ok.Latency.Percent75:F2} | {GetStatus(scenario.Ok.Latency.Percent75, 200)} |");
                md.AppendLine($"| P95 | {scenario.Ok.Latency.Percent95:F2} | {GetStatus(scenario.Ok.Latency.Percent95, 500)} |");
                md.AppendLine($"| P99 | {scenario.Ok.Latency.Percent99:F2} | {GetStatus(scenario.Ok.Latency.Percent99, 1000)} |");
                md.AppendLine($"| Max | {scenario.Ok.Latency.MaxMs:F2} | - |");
                md.AppendLine();
                md.AppendLine("### Throughput");
                md.AppendLine();
                md.AppendLine($"- **Requests/Second:** {scenario.Ok.Request.RPS:F2}");
                md.AppendLine($"- **Success Rate:** {100.0 - scenario.Fail.Request.Percent:F2}%");
                md.AppendLine();
            }

            File.WriteAllText(filePath, md.ToString());
        }

        private void GenerateCsvReport(NodeStats stats, string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Scenario,Metric,Value");

            foreach (var scenario in stats.ScenarioStats)
            {
                csv.AppendLine($"{scenario.ScenarioName},Total_Requests,{scenario.Ok.Request.Count + scenario.Fail.Request.Count}");
                csv.AppendLine($"{scenario.ScenarioName},Successful_Requests,{scenario.Ok.Request.Count}");
                csv.AppendLine($"{scenario.ScenarioName},Failed_Requests,{scenario.Fail.Request.Count}");
                csv.AppendLine($"{scenario.ScenarioName},RPS,{scenario.Ok.Request.RPS:F2}");
                // Mean is not directly available, using P50 instead
                csv.AppendLine($"{scenario.ScenarioName},P50_Latency,{scenario.Ok.Latency.Percent50:F2}");
                csv.AppendLine($"{scenario.ScenarioName},P50_Latency,{scenario.Ok.Latency.Percent50:F2}");
                csv.AppendLine($"{scenario.ScenarioName},P75_Latency,{scenario.Ok.Latency.Percent75:F2}");
                csv.AppendLine($"{scenario.ScenarioName},P95_Latency,{scenario.Ok.Latency.Percent95:F2}");
                csv.AppendLine($"{scenario.ScenarioName},P99_Latency,{scenario.Ok.Latency.Percent99:F2}");
                csv.AppendLine($"{scenario.ScenarioName},Max_Latency,{scenario.Ok.Latency.MaxMs:F2}");
                csv.AppendLine($"{scenario.ScenarioName},Error_Rate,{scenario.Fail.Request.Percent:F2}");
            }

            File.WriteAllText(filePath, csv.ToString());
        }

        private RegressionReport CheckForRegressions(NodeStats stats)
        {
            var report = new RegressionReport();
            
            // Load baseline if exists
            var baselinePath = Path.Combine(_reportPath, "baseline.json");
            if (File.Exists(baselinePath))
            {
                var baselineJson = File.ReadAllText(baselinePath);
                // Compare with baseline and detect regressions
                // This is simplified - in real implementation, you'd parse and compare properly
            }

            return report;
        }

        private string GetStatus(double value, double threshold, bool higherIsBetter = false)
        {
            if (higherIsBetter)
                return value >= threshold ? "PASS" : "FAIL";
            else
                return value <= threshold ? "PASS" : "FAIL";
        }

        private string GetStatusClass(double value, double threshold, bool higherIsBetter = false)
        {
            var status = GetStatus(value, threshold, higherIsBetter);
            return status == "PASS" ? "pass" : "fail";
        }
    }

    public class RegressionReport
    {
        public bool HasRegressions { get; set; }
        public List<string> Regressions { get; set; } = new List<string>();

        public override string ToString()
        {
            if (!HasRegressions)
                return "No performance regressions detected.";

            return $"Performance regressions detected:\n{string.Join("\n", Regressions)}";
        }
    }
}