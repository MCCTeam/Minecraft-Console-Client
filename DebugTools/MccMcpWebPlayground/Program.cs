using DebugTools.MccMcpWebPlayground.Api;
using DebugTools.MccMcpWebPlayground.Harness;
using DebugTools.MccMcpWebPlayground.Infrastructure.Mcp;
using DebugTools.MccMcpWebPlayground.Infrastructure.OpenRouter;
using Microsoft.AspNetCore.Http.Timeouts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<MccWebHarnessOptions>()
    .Bind(builder.Configuration.GetSection(MccWebHarnessOptions.SectionName));

builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy("mcc-stream", new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromMinutes(10)
    });
});

builder.Services.AddHttpClient("openrouter", client =>
{
    client.Timeout = TimeSpan.FromMinutes(15);
});

builder.Services.AddSingleton<MccMcpSessionFactory>();
builder.Services.AddSingleton<MccGuidanceSource>();
builder.Services.AddSingleton<MccContextCompressor>();
builder.Services.AddSingleton<MccFinalizer>();
builder.Services.AddSingleton<MccPromptComposer>();
builder.Services.AddSingleton<OpenRouterChatClient>();
builder.Services.AddScoped<IMccAgentRunService, MccAgentRunService>();

var app = builder.Build();

app.UseRequestTimeouts();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapMccPlaygroundEndpoints();

app.Run();
