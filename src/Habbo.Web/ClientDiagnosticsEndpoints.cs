using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Habbo.Web;

/// <summary>Receives the crash reports HabboAir posts when a fatal client error is caught.
/// The client builds the URL itself as "{url.prefix}/flash_client_error", so the path is fixed by the client,
/// and the body is the flat JSON object HabboAir.reportCrashStack assembles.</summary>
public static class ClientDiagnosticsEndpoints
{
    /// <summary>The path HabboAir appends to url.prefix when no explicit client.fatal.error.url is passed.</summary>
    private const string CrashReportPath = "/flash_client_error";

    /// <summary>Keys HabboAir always fills in; anything else the client sends is logged as extra data.</summary>
    private const string DescriptionKey = "error_desc";
    private const string CategoryKey = "error_cat";
    private const string StackKey = "error_data";
    private const string FatalKey = "is_fatal";
    private const string DebugKey = "debug";

    /// <summary>Bounds on what anyone may post here: the body, each value logged, and how many extra values.</summary>
    private const int MaxBodyBytes = 64 * 1024;
    private const int MaxValueLength = 4000;
    private const int MaxExtraValues = 20;

    public static void MapClientDiagnosticsEndpoints(this WebApplication app)
    {
        app.MapPost(CrashReportPath, async (HttpRequest request, ILoggerFactory loggerFactory, CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("Habbo.Client.Crash");
            if (request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } bodySize)
                bodySize.MaxRequestBodySize = MaxBodyBytes;

            // a body that is not a JSON object is a bad request, not a server error with a stack trace in the log
            JsonDocument document;
            try
            {
                document = await JsonDocument.ParseAsync(request.Body, cancellationToken: ct);
            }
            catch (JsonException)
            {
                return Results.BadRequest();
            }

            using var _ = document;
            var report = document.RootElement;
            if (report.ValueKind != JsonValueKind.Object)
                return Results.BadRequest();

            // the one-line fields stay one line, so a report cannot write log lines of its own
            var description = OneLine(ReadString(report, DescriptionKey));
            var category = OneLine(ReadString(report, CategoryKey));
            var fatal = OneLine(ReadString(report, FatalKey));
            var stack = ReadString(report, StackKey);
            var debug = ReadString(report, DebugKey);

            logger.LogError(
                "Client crash (category {Category}, fatal {Fatal}): {Description}\nStack: {Stack}\nDebug: {Debug}",
                category, fatal, description, string.IsNullOrEmpty(stack) ? "(none reported)" : stack, debug);

            foreach (var property in report.EnumerateObject().Take(MaxExtraValues))
            {
                if (property.NameEquals(DescriptionKey) || property.NameEquals(CategoryKey) ||
                    property.NameEquals(FatalKey) || property.NameEquals(StackKey) || property.NameEquals(DebugKey))
                    continue;

                logger.LogInformation("Client crash detail {Key} = {Value}", OneLine(property.Name), OneLine(Clip(property.Value.ToString())));
            }

            return Results.Ok();
        }).RequireRateLimiting(WebRateLimits.CrashReports);
    }

    private static string ReadString(JsonElement report, string key)
        => report.TryGetProperty(key, out var value) ? Clip(value.ToString()) : string.Empty;

    private static string Clip(string text) => text.Length > MaxValueLength ? text[..MaxValueLength] : text;

    private static string OneLine(string text) => text.ReplaceLineEndings(" ");
}
