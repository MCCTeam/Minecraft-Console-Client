using DMCBK.Core.Beacon;
using DMCBK.Core.Commands;
using Xunit;

namespace Mcc.Cli.Tests.Beacon;

internal static class ScriptTestHelpers
{
    internal static (BeaconEngine Engine, ScriptTestHost Host, VirtualClock Clock) NewEngine(int seed = 7)
    {
        var host = new ScriptTestHost();
        var clock = new VirtualClock();
        var engine = new BeaconEngine(host, clock, new SeededRng(seed), new FuelBudget());
        engine.Variables = new VariableStore();
        return (engine, host, clock);
    }

    internal static string WithHeader(string body) => "# beacon 1\n" + body;

    internal static async Task<string> ShowAsync(BeaconEngine engine, string scriptId, string expr)
    {
        BeaconRunResult run = await engine.RunScriptAsync(scriptId, WithHeader($"show {expr}\n"));
        Assert.True(run.Success, run.Error?.Message ?? "run failed");
        return Assert.Single(run.LocalOutput);
    }

    internal static async Task<BeaconRunResult> RunAsync(BeaconEngine engine, string scriptId, string body)
    {
        return await engine.RunScriptAsync(scriptId, WithHeader(body));
    }

    internal static void Reset()
    {
        BeaconProviders.Clear();
        BeaconHookCatalog.ClearCustomHooks();
    }
}
