using Mcc.Cli.BeaconTooling;
using Mcc.Cli;
using DMCBK.Core.Beacon;
using DMCBK.Core.Commands;
using Xunit;

namespace Mcc.Cli.Tests.Beacon;

/// <summary>
/// State, settings, interop, lifecycle, and hooks.
/// </summary>
public sealed class StateAndLifecycleTests : IDisposable
{
    public void Dispose() => ScriptTestHelpers.Reset();

    [Fact]
    public async Task Saved_MissingKey_OrDefault()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        Assert.Equal("{}", await ScriptTestHelpers.ShowAsync(engine, "s", "saved(\"missing\") or {}"));
        Assert.Equal("none", await ScriptTestHelpers.ShowAsync(engine, "s2", "saved(\"missing\")"));
    }

    [Fact]
    public async Task Shared_LockSerializesCounter()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        BeaconRunResult run = await ScriptTestHelpers.RunAsync(engine, "q",
            "lock shared\nset n to shared[\"quiz.plays\"] or 0\nset shared[\"quiz.plays\"] to n + 1\nend lock\nshow shared[\"quiz.plays\"]\n");
        Assert.True(run.Success, run.Error?.Message ?? "run failed");
        Assert.Equal("1", Assert.Single(run.LocalOutput));
    }

    [Fact]
    public async Task Settings_DeclaredReadable()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        BeaconRunResult run = await engine.RunScriptAsync(
            "s", "# beacon 1\n# setting thirst = 5 ; seconds between sips\nshow settings.thirst\n");
        Assert.True(run.Success, run.Error?.Message ?? "run failed");
        Assert.Equal("5", Assert.Single(run.LocalOutput));
    }

    [Fact]
    public async Task Hook_Kick_FiresWithReason()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        BeaconRunResult run = await engine.RunScriptAsync("k", ScriptTestHelpers.WithHeader("on kick\nshow reason\nend on\n"));
        Assert.True(run.Success, run.Error?.Message ?? "run failed");
        BeaconFireResult fire = await engine.FireEventAsync("kick", BeaconEventFields.Kick("griefing"));
        Assert.True(fire.Handlers[0].Result!.Success);
        Assert.Equal("griefing", Assert.Single(fire.Handlers[0].Result!.LocalOutput));
    }

    [Fact]
    public async Task Hook_Disconnect_FiresWithReason()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        BeaconRunResult run = await engine.RunScriptAsync("d", ScriptTestHelpers.WithHeader("on disconnect\nshow reason\nend on\n"));
        Assert.True(run.Success, run.Error?.Message ?? "run failed");
        BeaconFireResult fire = await engine.FireEventAsync("disconnect", BeaconEventFields.Disconnect("timed out"));
        Assert.True(fire.Handlers[0].Result!.Success);
        Assert.Equal("timed out", Assert.Single(fire.Handlers[0].Result!.LocalOutput));
    }

    [Fact]
    public async Task Hook_TpsNone_SurfacesNone()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        BeaconRunResult run = await engine.RunScriptAsync("t", ScriptTestHelpers.WithHeader("on tps\nshow tps\nend on\n"));
        Assert.True(run.Success, run.Error?.Message ?? "run failed");
        BeaconFireResult fire = await engine.FireEventAsync("tps", BeaconEventFields.Tps(null, null));
        Assert.True(fire.Handlers[0].Result!.Success);
        Assert.Equal("none", Assert.Single(fire.Handlers[0].Result!.LocalOutput));
    }

    [Fact]
    public async Task Hook_Health_FiresWithChange()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        BeaconRunResult run = await engine.RunScriptAsync("h", ScriptTestHelpers.WithHeader("on health\nshow change\nend on\n"));
        Assert.True(run.Success, run.Error?.Message ?? "run failed");
        BeaconFireResult fire = await engine.FireEventAsync("health", BeaconEventFields.Health(5, 20, -3));
        Assert.True(fire.Handlers[0].Result!.Success);
        Assert.Equal("-3", Assert.Single(fire.Handlers[0].Result!.LocalOutput));
    }

    [Fact]
    public async Task Hook_Death_FiresWithCause()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        BeaconRunResult run = await engine.RunScriptAsync("d", ScriptTestHelpers.WithHeader("on death\nshow \"{player} {cause}\"\nend on\n"));
        Assert.True(run.Success, run.Error?.Message ?? "run failed");
        BeaconFireResult fire = await engine.FireEventAsync("death", BeaconEventFields.Death("Steve", "fell"));
        Assert.True(fire.Handlers[0].Result!.Success);
        Assert.Equal("Steve fell", Assert.Single(fire.Handlers[0].Result!.LocalOutput));
    }

    [Fact]
    public async Task Hook_Unknown_WarnsWithCatalog()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        engine.LoadSource("u", "u.bcn", "# beacon 1\non frobnicate\nshow 1\nend on\n");
        IReadOnlyList<BeaconDiagnostic> diags = engine.Lint("u");
        BeaconDiagnostic warn = Assert.Single(diags, d => d.Code == BeaconDiagnosticCodes.UnknownEvent);
        Assert.Equal(BeaconSeverity.Warning, warn.Severity);
        Assert.Contains("chat", warn.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CustomHook_RegistersAndValidatesClean()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        using IDisposable environmentScope = engine.Environment.Enter();
        BeaconHookCatalog.RegisterCustomHook("shop_buy");
        try
        {
            engine.LoadSource("s", "s.bcn", "# beacon 1\non shop_buy\nshow 1\nend on\n");
            Assert.DoesNotContain(engine.Lint("s"), d => d.Code == BeaconDiagnosticCodes.UnknownEvent);
            Assert.True(BeaconHookCatalog.IsKnown("shop_buy"));
        }
        finally
        {
            BeaconHookCatalog.UnregisterCustomHook("shop_buy");
        }

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Extern_MissingProvider_RaisesCatchably()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        BeaconRunResult run = await ScriptTestHelpers.RunAsync(engine, "b",
            "extern price_of from \"shop\"\ntry\nshow price_of(\"bread\")\ncatch err\nshow err.message\nend try\n");
        Assert.True(run.Success);
        Assert.Contains("shop", Assert.Single(run.LocalOutput), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Call_MissingTarget_RaisesCatchably()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        BeaconRunResult run = await ScriptTestHelpers.RunAsync(engine, "c",
            "try\nshow call \"nosuch.fn\"()\ncatch err\nshow err.message\nend try\n");
        Assert.True(run.Success);
        Assert.Contains("nosuch", Assert.Single(run.LocalOutput), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Command_ArgReadsBoundValue()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        BeaconRunResult run = await engine.RunScriptAsync("cmd", ScriptTestHelpers.WithHeader(
            "command \"/price <item>\"\nshow arg(\"item\")\nend command\n"));
        Assert.True(run.Success, run.Error?.Message ?? "run failed");
        BeaconScriptCommandSpec spec = Assert.Single(engine.GetCommandSpecs("cmd"));
        Assert.Equal("price", spec.Name);
        BeaconRunResult invoked = await engine.InvokeScriptCommandAsync(
            "price", new Dictionary<string, string>(StringComparer.Ordinal) { ["item"] = "bread" });
        Assert.True(invoked.Success);
        Assert.Equal("bread", Assert.Single(invoked.LocalOutput));
    }

    [Fact]
    public async Task Export_FunctionCallableViaCall()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        BeaconRunResult first = await engine.RunScriptAsync("shop", ScriptTestHelpers.WithHeader(
            "export function daily_report()\nreturn \"open\"\nend function\n"));
        Assert.True(first.Success);
        BeaconRunResult second = await engine.RunScriptAsync("reader", ScriptTestHelpers.WithHeader("show call \"shop.daily_report\"()\n"));
        Assert.True(second.Success, second.Error?.Message ?? "call failed");
        Assert.Equal("open", Assert.Single(second.LocalOutput));
    }

    [Fact]
    public async Task Throttle_CooldownSkipsSecondFire()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        BeaconRunResult run = await engine.RunScriptAsync("thr", ScriptTestHelpers.WithHeader(
            "on tps cooldown 60 seconds named \"w\"\nshow \"hit\"\nend on\n"));
        Assert.True(run.Success);
        BeaconFireResult first = await engine.FireEventAsync("tps", BeaconEventFields.Tps(10, 90));
        Assert.False(first.Handlers[0].Throttled);
        Assert.Equal("hit", Assert.Single(first.Handlers[0].Result!.LocalOutput));
        BeaconFireResult second = await engine.FireEventAsync("tps", BeaconEventFields.Tps(10, 90));
        Assert.True(second.Handlers[0].Throttled);
        Assert.Null(second.Handlers[0].Result);
    }

    [Fact]
    public async Task ChatBucket_StatusMapShape()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        string shown = await ScriptTestHelpers.ShowAsync(engine, "c", "chat_bucket()");
        Assert.Contains("burst", shown, StringComparison.Ordinal);
        Assert.Contains("available", shown, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Budget_TinyFuel_AbortsWithCode()
    {
        var host = new ScriptTestHost();
        var engine = new BeaconEngine(host, new VirtualClock(), new SeededRng(1), new FuelBudget(80));
        engine.Variables = new VariableStore();
        BeaconRunResult run = await engine.RunScriptAsync("b", ScriptTestHelpers.WithHeader("while yes\nshow 1\nend while\n"));
        Assert.False(run.Success);
        Assert.Equal(BeaconDiagnosticCodes.BudgetExhausted, run.Error?.Code);
    }

    [Fact]
    public async Task Once_TwoTimers_FireInDeadlineOrder()
    {
        var host = new ScriptTestHost();
        var clock = new VirtualClock();
        var engine = new BeaconEngine(host, clock, new SeededRng(7), new FuelBudget());
        engine.Variables = new VariableStore();
        BeaconRunResult run = await engine.RunScriptAsync("o", ScriptTestHelpers.WithHeader(
            "in 10 seconds do\nshow \"first\"\nend in\nin 20 seconds do\nshow \"second\"\nend in\n"));
        Assert.True(run.Success, run.Error?.Message ?? "run failed");
        clock.Advance(TimeSpan.FromSeconds(10));
        IReadOnlyList<BeaconOnceRun> first = await engine.TickOnceAsync();
        Assert.Equal("first", Assert.Single(Assert.Single(first).Result.LocalOutput));
        clock.Advance(TimeSpan.FromSeconds(10));
        IReadOnlyList<BeaconOnceRun> second = await engine.TickOnceAsync();
        Assert.Equal("second", Assert.Single(Assert.Single(second).Result.LocalOutput));
    }

    [Fact]
    public async Task Every_UnknownUnit_Refuses()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        engine.LoadSource("e", "e.bcn", "# beacon 1\nevery 5 fortnights\nshow 1\nend every\n");
        Assert.Contains(engine.Lint("e"), d => d.Code == BeaconDiagnosticCodes.UnknownUnit);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task StopEvent_SuppressesChat()
    {
        var (engine, _, _) = ScriptTestHelpers.NewEngine();
        BeaconRunResult run = await engine.RunScriptAsync("s", ScriptTestHelpers.WithHeader(
            "on chat when message contains \"badword\"\nstop event\nend on\n"));
        Assert.True(run.Success);
        BeaconFireResult fire = await engine.FireEventAsync("chat", BeaconEventFields.Chat("G", "has badword here"));
        Assert.True(fire.Handlers[0].Result!.EventSuppressed);
    }

    [Fact]
    public void Import_MissingFile_FailsClosed()
    {
        string root = Path.Combine(Path.GetTempPath(), "mcc-beacon-gap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string path = Path.Combine(root, "root.bcn");
            File.WriteAllText(path, "# beacon 1\nimport \"lib/missing.bcn\" as econ\nshow 1\n");
            BeaconLintReport report = Assert.Single(BeaconLint.LintFiles([path]));
            Assert.Contains(report.Diagnostics, d => d.Code == BeaconDiagnosticCodes.ImportNotFound);
            Assert.False(report.Ok);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void RunCli_MissingFile_ExitsUsage()
    {
        string missing = Path.Combine(Path.GetTempPath(), "mcc-beacon-gap-" + Guid.NewGuid().ToString("N") + ".bcn");
        Assert.True(BeaconRunCli.TryParse([missing], out BeaconRunCliArgs? args, out _));
        int exit = BeaconRunCli.Execute(args!, null, out _, out string stderr);
        Assert.Equal(BeaconLint.ExitCodes.Usage, exit);
        Assert.Contains("cannot read", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public void LintCli_CleanFile_ExitZero()
    {
        string path = Path.Combine(Path.GetTempPath(), "mcc-beacon-gap-" + Guid.NewGuid().ToString("N") + ".bcn");
        File.WriteAllText(path, "# beacon 1\nshow 1\n");
        try
        {
            Assert.True(BeaconLintCli.TryParse([path], out BeaconLintRequest? args, out _));
            int exit = BeaconLintCli.Execute(args!, null, out _, out _);
            Assert.Equal(BeaconLint.ExitCodes.Clean, exit);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    [Fact]
    public void FormatCli_MissingFile_ExitUsage()
    {
        Assert.True(BeaconFormatCli.TryParse([], out BeaconFormatCliArgs? args, out _));
        int exit = BeaconFormatCli.Execute(args!, null, out _, out string stderr);
        Assert.Equal(BeaconLint.ExitCodes.Usage, exit);
        Assert.Contains("Usage", stderr, StringComparison.Ordinal);
    }
}
