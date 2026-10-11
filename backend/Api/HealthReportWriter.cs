using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CCC.Api;

static class HealthReportWriter
{
    public sealed record HealthResponse(string Status, IReadOnlyDictionary<string, CheckResponse> Checks);

    public sealed record CheckResponse(string Status, string? Description, IReadOnlyDictionary<string, object> Data);

    public static Task WriteAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new HealthResponse(
            report.Status.ToString(),
            report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new CheckResponse(entry.Value.Status.ToString(), entry.Value.Description, entry.Value.Data))));
}
