using Mcc.Cli.Presentation;
using Mcc.Cli.Startup;
using DMCBK.Core.Configuration;
using DMCBK.Testing;
using Mcc.Cli;
using Xunit;

namespace Mcc.Cli.Tests.Hosting;

public sealed class IdlePluginClaimTests
{
    private const string Source = """
        using System.Threading.Tasks;
        using DMCBK.PluginSdk;
        public sealed class Probe : IPlugin
        {
            public void Configure(PluginDescriptor descriptor) => descriptor.Id = "bridge";
            public Task ActivateAsync(PluginContext context) => Task.CompletedTask;
        }
        """;

    [Fact]
    public async Task TheIdleBannerNamesTheRunningOfflinePlugins()
    {
        await using var host = PluginTestHost.Create();
        host.AddSourcePlugin("bridge", Source, extraManifestKeys: "[services]\noffline = true\n");
        host.AddSourcePlugin("quiet", Source.Replace("\"bridge\"", "\"quiet\""));
        Assert.True((await host.LoadAsync()).Success);
        string banner = IdleStart.Banner(new DmcbkConfiguration(), host.Client, ConsoleColorDepth.Disable);
        Assert.StartsWith("[MCC] ", banner, StringComparison.Ordinal);
        Assert.Contains("bridge", banner, StringComparison.Ordinal);
        Assert.DoesNotContain("quiet", banner, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheIdleBannerSaysNothingExtraWhenNothingClaimsIt()
    {
        await using var host = PluginTestHost.Create();
        string banner = IdleStart.Banner(new DmcbkConfiguration(), host.Client, ConsoleColorDepth.Disable);
        Assert.DoesNotContain('\n', banner);
    }
}
