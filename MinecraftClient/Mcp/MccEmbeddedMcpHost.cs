using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace MinecraftClient.Mcp;

public sealed class MccEmbeddedMcpHost
{
    private readonly MccMcpConfig config;
    private readonly IMccMcpCapabilities capabilities;
    private readonly SemaphoreSlim stateLock = new(1, 1);
    private WebApplication? app;

    public MccEmbeddedMcpHost(MccMcpConfig config, IMccMcpCapabilities capabilities)
    {
        this.config = config;
        this.capabilities = capabilities;
    }

    public bool IsRunning
    {
        get
        {
            return app is not null;
        }
    }

    public string Endpoint => $"http://{config.Transport.BindHost}:{config.Transport.Port}{NormalizeRoute(config.Transport.Route)}";

    public bool Start(out string? error)
    {
        (bool success, string? startError) = StartAsync().GetAwaiter().GetResult();
        error = startError;
        return success;
    }

    public bool Stop(out string? error)
    {
        (bool success, string? stopError) = StopAsync().GetAwaiter().GetResult();
        error = stopError;
        return success;
    }

    public async Task<(bool Success, string? Error)> StartAsync(CancellationToken cancellationToken = default)
    {
        await stateLock.WaitAsync(cancellationToken);
        try
        {
            if (app is not null)
                return (true, null);

            string route = NormalizeRoute(config.Transport.Route);
            string bindHost = string.IsNullOrWhiteSpace(config.Transport.BindHost) ? "127.0.0.1" : config.Transport.BindHost.Trim();
            if (config.Transport.Port is < 1 or > 65535)
                return (false, "invalid_port");

            string? requiredToken = null;
            if (config.Transport.RequireAuthToken)
            {
                requiredToken = Environment.GetEnvironmentVariable(config.Transport.AuthTokenEnvVar);
                if (string.IsNullOrWhiteSpace(requiredToken))
                    return (false, "missing_auth_token");
            }

            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.Logging.AddFilter(_ => false);
            builder.Services.AddSingleton(capabilities);
            builder.Services.AddSingleton(config);
            builder.Services.AddSingleton<MccMcpGuidanceProvider>();
            builder.Services.AddMcpServer()
                .WithHttpTransport()
                .WithTools<MccMcpToolSet>()
                .WithPrompts<MccMcpPromptSet>();

            builder.WebHost.UseUrls($"http://{bindHost}:{config.Transport.Port}");
            WebApplication builtApp = builder.Build();

            if (config.Transport.RequireAuthToken)
            {
                builtApp.Use(async (context, next) =>
                {
                    if (context.Request.Path.StartsWithSegments(route, StringComparison.OrdinalIgnoreCase))
                    {
                        string auth = context.Request.Headers.Authorization.ToString();
                        if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                            || !string.Equals(auth[7..], requiredToken, StringComparison.Ordinal))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsync("Unauthorized");
                            return;
                        }
                    }

                    await next();
                });
            }

            builtApp.MapMcp(route);

            try
            {
                await builtApp.StartAsync(cancellationToken);
                app = builtApp;
                return (true, null);
            }
            catch
            {
                await builtApp.DisposeAsync();
                throw;
            }
        }
        finally
        {
            stateLock.Release();
        }
    }

    public async Task<(bool Success, string? Error)> StopAsync(CancellationToken cancellationToken = default)
    {
        await stateLock.WaitAsync(cancellationToken);
        try
        {
            if (app is null)
                return (true, null);

            try
            {
                await app.StopAsync(cancellationToken);
                await app.DisposeAsync();
                app = null;
                return (true, null);
            }
            catch
            {
                return (false, "stop_failed");
            }
        }
        finally
        {
            stateLock.Release();
        }
    }

    private static string NormalizeRoute(string route)
    {
        string normalized = string.IsNullOrWhiteSpace(route) ? "/mcp" : route.Trim();
        if (!normalized.StartsWith('/'))
            normalized = '/' + normalized;
        return normalized;
    }
}
