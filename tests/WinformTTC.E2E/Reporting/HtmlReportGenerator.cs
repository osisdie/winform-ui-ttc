using System.Text;
using WinformTTC.E2E.Logging;

namespace WinformTTC.E2E.Reporting;

public static class HtmlReportGenerator
{
    private static readonly string ReportDir = Path.Combine(AppContext.BaseDirectory, "reports");

    public static string Generate(TestRunResult result)
    {
        Directory.CreateDirectory(ReportDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeName = string.Join("_", result.TestName.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{timestamp}_{safeName}.html";
        var filePath = Path.Combine(ReportDir, fileName);

        var html = BuildHtml(result);
        File.WriteAllText(filePath, html);

        TestLogger.Log.Information("HTML report generated: {Path}", filePath);
        return filePath;
    }

    private static string BuildHtml(TestRunResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
        sb.AppendLine($"<title>E2E Report: {Encode(result.TestName)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(GetCss());
        sb.AppendLine("</style></head><body>");

        // Header
        var statusClass = result.AllPassed ? "pass" : "fail";
        var statusText = result.AllPassed ? "PASSED" : "FAILED";
        sb.AppendLine($"<div class=\"header {statusClass}\">");
        sb.AppendLine($"<h1>{Encode(result.TestName)}</h1>");
        sb.AppendLine($"<span class=\"badge\">{statusText}</span>");
        sb.AppendLine("</div>");

        // Summary
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine($"<p><strong>Start:</strong> {result.StartTime:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine($"<p><strong>Duration:</strong> {result.Duration:mm\\:ss\\.fff}</p>");
        sb.AppendLine($"<p><strong>Steps:</strong> {result.Steps.Count} " +
                       $"({result.Steps.Count(s => s.Passed)} passed, " +
                       $"{result.Steps.Count(s => !s.Passed)} failed)</p>");
        sb.AppendLine("</div>");

        // Steps
        sb.AppendLine("<div class=\"steps\">");
        for (var i = 0; i < result.Steps.Count; i++)
        {
            var step = result.Steps[i];
            var stepClass = step.Passed ? "step-pass" : "step-fail";
            var icon = step.Passed ? "&#10004;" : "&#10008;";

            sb.AppendLine($"<div class=\"step {stepClass}\">");
            sb.AppendLine($"<h3>{icon} Step {i + 1}: {Encode(step.StepName)}</h3>");
            sb.AppendLine($"<p class=\"timestamp\">{step.Timestamp:HH:mm:ss.fff}</p>");

            if (!string.IsNullOrEmpty(step.Detail))
                sb.AppendLine($"<p class=\"detail\">{Encode(step.Detail)}</p>");

            if (!string.IsNullOrEmpty(step.ScreenshotPath) && File.Exists(step.ScreenshotPath))
            {
                var base64 = Convert.ToBase64String(File.ReadAllBytes(step.ScreenshotPath));
                sb.AppendLine($"<img src=\"data:image/png;base64,{base64}\" alt=\"{Encode(step.StepName)}\" />");
            }

            sb.AppendLine("</div>");
        }
        sb.AppendLine("</div>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string GetCss() => """
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
               background: #f5f5f5; color: #333; padding: 20px; }
        .header { padding: 20px; border-radius: 8px; margin-bottom: 20px;
                   display: flex; align-items: center; justify-content: space-between; }
        .header.pass { background: #d4edda; border: 1px solid #28a745; }
        .header.fail { background: #f8d7da; border: 1px solid #dc3545; }
        .header h1 { font-size: 1.4em; }
        .badge { padding: 6px 16px; border-radius: 4px; font-weight: bold; color: #fff; }
        .pass .badge { background: #28a745; }
        .fail .badge { background: #dc3545; }
        .summary { background: #fff; padding: 16px; border-radius: 8px;
                    margin-bottom: 20px; border: 1px solid #ddd; }
        .summary p { margin: 4px 0; }
        .steps { display: flex; flex-direction: column; gap: 16px; }
        .step { background: #fff; padding: 16px; border-radius: 8px; border: 1px solid #ddd; }
        .step-pass { border-left: 4px solid #28a745; }
        .step-fail { border-left: 4px solid #dc3545; }
        .step h3 { margin-bottom: 8px; }
        .step-pass h3 { color: #28a745; }
        .step-fail h3 { color: #dc3545; }
        .timestamp { color: #888; font-size: 0.85em; margin-bottom: 4px; }
        .detail { margin-bottom: 8px; white-space: pre-wrap; font-family: monospace;
                   background: #f8f9fa; padding: 8px; border-radius: 4px; }
        img { max-width: 100%; border: 1px solid #ddd; border-radius: 4px; margin-top: 8px; }
        """;

    private static string Encode(string text) =>
        System.Net.WebUtility.HtmlEncode(text);
}
