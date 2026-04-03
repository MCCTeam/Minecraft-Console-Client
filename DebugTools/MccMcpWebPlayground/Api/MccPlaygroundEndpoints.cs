using DebugTools.MccMcpWebPlayground.Contracts;
using DebugTools.MccMcpWebPlayground.Harness;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DebugTools.MccMcpWebPlayground.Api;

public static class MccPlaygroundEndpoints
{
    public static IEndpointRouteBuilder MapMccPlaygroundEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder api = endpoints.MapGroup("/api");

        api.MapGet("/health", () => Results.Ok(new { ok = true }));

        api.MapGet("/config", (IOptions<MccWebHarnessOptions> options) =>
        {
            MccWebHarnessOptions harnessOptions = options.Value;
            return Results.Ok(new MccConfigResponse(
                Model: harnessOptions.ResolveModel(),
                OpenRouterBaseUrl: harnessOptions.ResolveOpenRouterBaseUrl(),
                McpEndpoint: harnessOptions.ResolveMcpEndpoint(),
                HasApiKey: harnessOptions.HasApiKeyConfigured(),
                ExposeInventoryWindowAction: harnessOptions.ExposeInventoryWindowAction,
                ExposeInternalCommandTool: harnessOptions.ExposeInternalCommandTool));
        });

        api.MapPost("/chat/stream", (ChatStreamRequest request, IMccAgentRunService runService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            return TypedResults.ServerSentEvents(runService.StreamAsync(request, httpContext, cancellationToken));
        })
        .WithRequestTimeout("mcc-stream");

        return endpoints;
    }
}
