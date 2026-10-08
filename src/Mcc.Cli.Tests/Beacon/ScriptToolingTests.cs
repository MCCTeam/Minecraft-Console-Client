using Mcc.Cli.BeaconTooling;
using Mcc.Cli;
using DMCBK.Core.Beacon;
using Xunit;

namespace Mcc.Cli.Tests.Beacon;

/// <summary>
/// Tooling: headless run exit codes, the formatter pass, and per-script settings files.
/// </summary>
public sealed class ScriptToolingTests : IDisposable
{
    private readonly List<string> _roots = [];

    public void Dispose()
    {
        BeaconProviders.Clear();
        BeaconHookCatalog.ClearCustomHooks();
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
        string root = Path.Combine(Path.GetTempPath(), "mcc-beacon-follow-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        _roots.Add(root);
        string path = Path.Combine(root, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Run_CleanFile_ExitZero()
    {
        string path = NewFile("quiz.bcn", "# beacon 1\nshow 1 + 2\nassert(1 is 1, \"math\")\n");
        Assert.True(BeaconRunCli.TryParse([path], out BeaconRunCliArgs? args, out string error), error);
        Assert.NotNull(args);
        int exit = BeaconRunCli.Execute(args!, null, out string stdout, out string stderr);
        Assert.Equal(BeaconLint.ExitCodes.Clean, exit);
        Assert.Contains("3", stdout, StringComparison.Ordinal);
        Assert.Equal(string.Empty, stderr);
    }

    [Fact]
    public void Run_FailingAssert_ExitOne()
    {
        string path = NewFile("quiz.bcn", "# beacon 1\nassert(1 is 2, \"bot survived\")\n");
        Assert.True(BeaconRunCli.TryParse([path], out BeaconRunCliArgs? args, out string error), error);
        int exit = BeaconRunCli.Execute(args!, null, out string stdout, out string _);
        Assert.Equal(BeaconLint.ExitCodes.Errors, exit);
        Assert.Contains("bot survived", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_NoFiles_ExitUsage()
    {
        Assert.True(BeaconRunCli.TryParse([], out BeaconRunCliArgs? args, out _));
        int exit = BeaconRunCli.Execute(args!, null, out _, out string stderr);
        Assert.Equal(BeaconLint.ExitCodes.Usage, exit);
        Assert.Contains("Usage", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_UnknownFlag_ExitUsage()
    {
        Assert.False(BeaconRunCli.TryParse(["--frobnicate", "x.bcn"], out _, out string error));
        Assert.Contains("unknown flag", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_RelativeImport_ResolvesAgainstScriptFile()
    {
        string root = Path.Combine(Path.GetTempPath(), "mcc-beacon-follow-" + Guid.NewGuid().ToString("N"));
        string lib = Path.Combine(root, "lib");
        Directory.CreateDirectory(lib);
        _roots.Add(root);
        File.WriteAllText(Path.Combine(lib, "e.bcn"), "# beacon 1\nfunction price(item)\nreturn 5\nend function\n");
        string path = Path.Combine(root, "shop.bcn");
        File.WriteAllText(path, "# beacon 1\nimport \"lib/e.bcn\" as econ\nshow econ.price(\"bread\")\n");
        Assert.True(BeaconRunCli.TryParse([path], out BeaconRunCliArgs? args, out string error), error);
        int exit = BeaconRunCli.Execute(args!, null, out string stdout, out string _);
        Assert.Equal(BeaconLint.ExitCodes.Clean, exit);
        Assert.Contains("5", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_Tick_FiresOnceTimers()
    {
        string path = NewFile(
            "timer.bcn", "# beacon 1\nin 30 seconds do\nshow \"fired\"\nend in\n");
        Assert.True(BeaconRunCli.TryParse([path, "--tick", "60"], out BeaconRunCliArgs? args, out string error), error);
        int exit = BeaconRunCli.Execute(args!, null, out string stdout, out string _);
        Assert.Equal(BeaconLint.ExitCodes.Clean, exit);
        Assert.Contains("fired", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_Seed_ReplaysDeterministically()
    {
        string path = NewFile("rng.bcn", "# beacon 1\nshow random(100)\n");
        Assert.True(BeaconRunCli.TryParse([path, "--seed", "42"], out BeaconRunCliArgs? a1, out _));
        Assert.True(BeaconRunCli.TryParse([path, "--seed", "42"], out BeaconRunCliArgs? a2, out _));
        BeaconRunCli.Execute(a1!, null, out string first, out _);
        BeaconRunCli.Execute(a2!, null, out string second, out _);
        Assert.Equal(first, second);
    }

    [Fact]
    public void Run_Json_Contract()
    {
        string path = NewFile("quiz.bcn", "# beacon 1\nshow \"hi\"\n");
        Assert.True(BeaconRunCli.TryParse([path, "--format", "json"], out BeaconRunCliArgs? args, out _));
        int exit = BeaconRunCli.Execute(args!, null, out string stdout, out _);
        Assert.Equal(BeaconLint.ExitCodes.Clean, exit);
        Assert.Contains("\"ok\": true", stdout, StringComparison.Ordinal);
        Assert.Contains("\"summary\"", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_IndentNormalization_Applies()
    {
        const string source = "# beacon 1\non join:\nsay \"hi\"\nend on\n";
        BeaconFixPreview preview = BeaconFix.Preview("f.bcn", source);
        Assert.True(preview.HasFixes);
        Assert.Contains("  say \"hi\"", preview.FixedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_LeavesMeaningAlone()
    {
        const string source = "# beacon 1\non join:\n  say \"hi\"\nend on\n";
        BeaconFixPreview preview = BeaconFix.Preview("f.bcn", source);
        Assert.False(preview.HasFixes);
    }

    [Fact]
    public void Format_QuoteNormalization_Applies()
    {
        string source = "# beacon 1\nshow \u201Chi\u201D\n";
        BeaconFixPreview preview = BeaconFix.Preview("f.bcn", source);
        Assert.True(preview.HasFixes);
        Assert.Contains("show \"hi\"", preview.FixedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_GeneratesCommentedDefaults()
    {
        string root = Path.Combine(Path.GetTempPath(), "mcc-beacon-follow-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        _roots.Add(root);
        string folder = Path.Combine(root, "configurations");
        Directory.CreateDirectory(folder);
        var schema = new List<BeaconSettingDecl>
        {
            new("thirst", BeaconValue.Number(5), "seconds between sips", new SourceSpan("s.bcn", 2, 1, 10)),
        };
        IReadOnlyDictionary<string, BeaconValue> resolved =
            BeaconScriptSettings.Resolve("s", schema, folder, []);
        Assert.Equal(5, ((BeaconNumberValue)resolved["thirst"]).Value);
        string path = Path.Combine(folder, "beacon", "s.settings.toml");
        Assert.True(File.Exists(path));
        string text = File.ReadAllText(path);
        Assert.Contains("seconds between sips", text, StringComparison.Ordinal);
        Assert.Contains("thirst = 5", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_OverlayWins()
    {
        string root = Path.Combine(Path.GetTempPath(), "mcc-beacon-follow-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        _roots.Add(root);
        string folder = Path.Combine(root, "configurations");
        Directory.CreateDirectory(Path.Combine(folder, "beacon"));
        File.WriteAllText(Path.Combine(folder, "beacon", "s.settings.toml"), "thirst = 9\n");
        var schema = new List<BeaconSettingDecl>
        {
            new("thirst", BeaconValue.Number(5), string.Empty, new SourceSpan("s.bcn", 2, 1, 10)),
        };
        IReadOnlyDictionary<string, BeaconValue> resolved =
            BeaconScriptSettings.Resolve("s", schema, folder, []);
        Assert.Equal(9, ((BeaconNumberValue)resolved["thirst"]).Value);
    }

    [Fact]
    public void Header_SettingAfterCode_WarnsLate()
    {
        const string body = "# beacon 1\nshow 1\n# setting thirst = 5\n";
        BeaconHeaderResult header = BeaconHeader.Parse("s.bcn", body, firstCodeLine: 2, comments: null);
        Assert.True(header.Ok);
        Assert.Empty(header.Settings);
        Assert.Contains(header.Diagnostics, d => d.Code == BeaconDiagnosticCodes.LateManifest);
    }
}
