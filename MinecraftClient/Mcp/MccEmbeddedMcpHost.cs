using System;
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
    private readonly object stateLock = new();
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
            lock (stateLock)
            {
                return app is not null;
            }
        }
    }

    public string Endpoint => $"http://{config.Transport.BindHost}:{config.Transport.Port}{NormalizeRoute(config.Transport.Route)}";

    public bool Start(out string? error)
    {
        lock (stateLock)
        {
            error = null;
            if (app is not null)
                return true;

            string route = NormalizeRoute(config.Transport.Route);
            string bindHost = string.IsNullOrWhiteSpace(config.Transport.BindHost) ? "127.0.0.1" : config.Transport.BindHost.Trim();
            if (config.Transport.Port is < 1 or > 65535)
            {
                error = "invalid_port";
                return false;
            }

            string? requiredToken = null;
            if (config.Transport.RequireAuthToken)
            {
                requiredToken = Environment.GetEnvironmentVariable(config.Transport.AuthTokenEnvVar);
                if (string.IsNullOrWhiteSpace(requiredToken))
                {
                    error = "missing_auth_token";
                    return false;
                }
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
            builtApp.StartAsync().GetAwaiter().GetResult();
            app = builtApp;
            return true;
        }
    }

    public bool Stop(out string? error)
    {
        lock (stateLock)
        {
            error = null;
            if (app is null)
                return true;

            try
            {
                app.StopAsync().GetAwaiter().GetResult();
                app.DisposeAsync().AsTask().GetAwaiter().GetResult();
                app = null;
                return true;
            }
            catch
            {
                error = "stop_failed";
                return false;
            }
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
