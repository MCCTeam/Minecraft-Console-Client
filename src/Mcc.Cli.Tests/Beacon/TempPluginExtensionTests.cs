using Mcc.Cli.Tests.Beacon.Fixtures;
using Mcc.Cli.BeaconTooling;
using Mcc.Cli;
using DMCBK.Core.Beacon;
using Xunit;

namespace Mcc.Cli.Tests.Beacon;

/// <summary>
/// Temporary-plugin extension tests: the <see cref="TempBeaconExtensionPlugin"/> fixture exercises custom functions, variables, events, suppression, unload withdrawal, and capability-gated lint through the same engines the headless CLI drives.
/// </summary>
public sealed class TempPluginExtensionTests : IDisposable
{
    private readonly List<string> _roots = [];

    public void Dispose()
    {
        ScriptTestHelpers.Reset();
        foreach (string root in _roots)
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
            }
        }
    }

    private string NewFile(string name, string content)
    {
        string root = Path.Combine(Path.GetTempPath(), "mcc-beacon-temp-plugin-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        _roots.Add(root);
        string path = Path.Combine(root, name);
        File.WriteAllText(path, content);
        return path;
    }

    private const string ExternPrologue =
        "# needs: temp.econ temp.free\n" +
        "extern temp_shout from \"temp-fixture\"\n" +
        "extern temp_deposit from \"temp-fixture\"\n";

    [Fact]
    public async Task Functions_PureAndGated_ReturnValues()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        using var plugin = new TempBeaconExtensionPlugin();
        plugin.Attach(engine);

        BeaconRunResult run = await ScriptTestHelpers.RunAsync(engine, "calls",
            ExternPrologue + "show temp_shout(\"hi\")\nshow temp_deposit(5)\n");
        Assert.True(run.Success, run.Error?.Message ?? "run failed");
        Assert.Equal(["HI", "5"], run.LocalOutput);
        Assert.Equal(1, plugin.DepositCalls);
    }

    [Fact]
    public async Task Variables_ReadBothDirections_AndWriteRoundTrip()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        using var plugin = new TempBeaconExtensionPlugin();
        plugin.Attach(engine);
        plugin.SetBalance(7);

        BeaconRunResult run = await ScriptTestHelpers.RunAsync(engine, "vault",
            ExternPrologue + "show tempvault.balance\nshow temp_deposit(5)\nshow tempvault.balance\n");
        Assert.True(run.Success, run.Error?.Message ?? "run failed");
        Assert.Equal(["7", "12", "12"], run.LocalOutput);
        Assert.Equal(12, plugin.GetBalance());
    }

    [Fact]
    public async Task Events_DeliversFieldsToHandler()
    {
        var (engine, host, _) = ScriptTestHelpers.NewEngine();
        using var plugin = new TempBeaconExtensionPlugin();
        plugin.Attach(engine);

        BeaconRunResult run = await ScriptTestHelpers.RunAsync(engine, "clerk",
            "# needs: chat.send temp.econ\n" +
            "on temp_sale when item is \"bread\":\nsay \"Sold {item} for {price}.\"\nend on\n");
        Assert.True(run.Success, run.Error?.Message ?? "run failed");

        BeaconFireResult fire = await plugin.FireSaleAsync(engine, "bread", 5);
        Assert.Single(fire.Handlers);
        Assert.False(fire.Suppressed);
        Assert.Equal("Sold bread for 5.", Assert.Single(host.Says));
    }

    [Fact]
    public async Task Events_StopEventSuppressesWhenSuppressible()
    {
        var (engine, host, _) = ScriptTestHelpers.NewEngine();
        using var plugin = new TempBeaconExtensionPlugin();
        plugin.Attach(engine);

        BeaconRunResult run = await ScriptTestHelpers.RunAsync(engine, "gate",
            "# needs: chat.send temp.econ\n" +
            "on temp_sale:\nsay \"seen\"\nstop event\nend on\n");
        Assert.True(run.Success, run.Error?.Message ?? "run failed");

        BeaconFireResult fire = await plugin.FireSaleAsync(engine, "bread", 5);
        Assert.True(fire.Suppressed);
        Assert.Equal("seen", Assert.Single(host.Says));
    }

    [Fact]
    public async Task Unload_WithdrawsFunctionsVariablesAndEvent()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        using IDisposable environmentScope = engine.Environment.Enter();
        var plugin = new TempBeaconExtensionPlugin();
        plugin.Attach(engine);
        Assert.True(engine.Bridge.TryGetFunction("temp_shout", out _));
        Assert.True(engine.Bridge.TryGetVariable("tempvault", out _));
        Assert.True(BeaconHookCatalog.IsKnown("temp_sale"));
        Assert.True(BeaconProviders.TryGetEvent("temp_sale", out _, out _));

        plugin.Dispose();
        Assert.False(engine.Bridge.TryGetFunction("temp_shout", out _));
        Assert.False(engine.Bridge.TryGetVariable("tempvault", out _));
        Assert.False(BeaconHookCatalog.IsKnown("temp_sale"));
        Assert.False(BeaconProviders.TryGetEvent("temp_sale", out _, out _));
        Assert.False(BeaconProviders.TryGetFunction("temp_shout", out _, out _));

        BeaconFireResult fire = await plugin.FireSaleAsync(engine, "bread", 5);
        Assert.Empty(fire.Handlers);

        BeaconRunResult run = await ScriptTestHelpers.RunAsync(engine, "calls",
            ExternPrologue + "try\nshow temp_shout(\"hi\")\ncatch err\nshow err.message\nend try\n");
        Assert.True(run.Success);
        Assert.Contains("temp-fixture", Assert.Single(run.LocalOutput), StringComparison.Ordinal);

        BeaconLintReport lint = BeaconLint.LintSource(
            "gone.bcn", ScriptTestHelpers.WithHeader("on temp_sale\nshow item\nend on\n"));
        Assert.Contains(
            lint.Diagnostics,
            d => d.Code == BeaconDiagnosticCodes.UnknownEvent && d.Severity == BeaconSeverity.Warning);
    }

    [Fact]
    public void Lint_ManifestWithoutCapabilityRefusesWithPasteLine()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        using IDisposable environmentScope = engine.Environment.Enter();
        using var plugin = new TempBeaconExtensionPlugin();
        plugin.Attach(engine);

        BeaconLintReport refused = BeaconLint.LintSource(
            "stand.bcn",
            "# beacon 1\n# needs: chat.send\n" +
            "extern temp_deposit from \"temp-fixture\"\nshow temp_deposit(5)\n");
        Assert.False(refused.Ok);
        BeaconDiagnostic error = Assert.Single(
            refused.Diagnostics, d => d.Code == BeaconDiagnosticCodes.ManifestNeedsMismatch);
        Assert.Contains("temp.econ", error.Message, StringComparison.Ordinal);
        Assert.Contains("# needs:", error.Suggestion ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("temp.econ", error.Suggestion ?? string.Empty, StringComparison.Ordinal);

        BeaconLintReport allowed = BeaconLint.LintSource(
            "stand.bcn",
            "# beacon 1\n# needs: temp.econ temp.free\n" +
            "extern temp_shout from \"temp-fixture\"\n" +
            "extern temp_deposit from \"temp-fixture\"\n" +
            "show temp_shout(\"hi\")\nshow tempvault.balance\n" +
            "on temp_sale:\nshow item\nend on\n");
        Assert.True(allowed.Ok);
        Assert.Contains("temp.econ", allowed.Permissions);
    }

    [Fact]
    public async Task Harness_HeadlessLintAndRunCoverPluginScript()
    {
        var (engine, host, _) = ScriptTestHelpers.NewEngine();
        using var plugin = new TempBeaconExtensionPlugin();
        plugin.Attach(engine);

        const string source =
            "# beacon 1\n# needs: chat.send temp.econ temp.free\n" +
            "extern temp_shout from \"temp-fixture\"\n" +
            "extern temp_deposit from \"temp-fixture\"\n" +
            "try\nshow temp_shout(\"hi\")\ncatch err\nshow \"no provider: {err.message}\"\nend try\n" +
            "on temp_sale:\nsay \"Sold {item} for {price}.\"\nend on\n";
        string path = NewFile("stand.bcn", source);

        Assert.True(BeaconLintCli.TryParse([path], out BeaconLintRequest? lintArgs, out string lintError), lintError);
        Assert.Equal(BeaconLint.ExitCodes.Clean, BeaconLintCli.Execute(lintArgs!, null, out _, out string lintStderr));
        Assert.Equal(string.Empty, lintStderr);

        BeaconRunResult run = await engine.RunScriptAsync("stand", source);
        Assert.True(run.Success, run.Error?.Message ?? "run failed");
        Assert.Equal("HI", Assert.Single(run.LocalOutput));

        BeaconFireResult fire = await plugin.FireSaleAsync(engine, "bread", 5);
        Assert.Single(fire.Handlers);
        Assert.Equal("Sold bread for 5.", Assert.Single(host.Says));
    }
}
