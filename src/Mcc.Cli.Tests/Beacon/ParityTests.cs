using DMCBK.Core;
using Mcc.Cli.BeaconTooling;
using Mcc.Cli;
using System.Text.Json;
using DMCBK.Core.Beacon;
using DMCBK.Core.Commands;
using DMCBK.Core.Configuration;
using Umpk.Auth;
using Xunit;

namespace Mcc.Cli.Tests.Beacon;

/// <summary>
/// Parity plus in-client behavior: shared fixtures produce byte-identical diagnostics from in-client <c>/scripts lint</c> and headless <c>lint</c> (both call the same engine), and the mute plus stop-all controls behave with status visibility.
/// </summary>
public sealed class ParityTests : IDisposable
{
    private readonly List<string> _roots = [];

    public void Dispose()
    {
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

    private string NewRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "mcc-beacon-parity-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        _roots.Add(root);
        return root;
    }

    private sealed class CaptureOutput : ICommandOutput
    {
        public List<string> Lines { get; } = [];
        public void WriteLine(string line) => Lines.Add(line);
    }

    private sealed class QuietHost(ICommandOutput? output) : IHostInterface
    {
        public IUserPrompt? Prompt => null;
        public IAuthInteraction? AuthInteraction => null;
        public ICommandOutput? CommandOutput { get; } = output;
        public IHostUi? Ui => null;
    }

    private static Client BuildClient(string configsFolder, ICommandOutput? output = null)
        => new ClientBuilder()
            .UseConfiguration(new DmcbkConfiguration { SourceFolder = configsFolder })
            .UseUsername("Tester")
            .UseHostInterface(new QuietHost(output))
            .UseCommands().UseBeacon().Build();

    private static string CanonicalDiagnostics(string json)
    {
        using JsonDocument parsed = JsonDocument.Parse(json);
        JsonElement diagnostics = parsed.RootElement.GetProperty("diagnostics");
        var rows = new List<string>();
        foreach (JsonElement diag in diagnostics.EnumerateArray())
            rows.Add(diag.GetRawText());

        rows.Sort(StringComparer.Ordinal);
        return string.Join("\n", rows);
    }

    private const string CleanSource =
        "# beacon 1\n# needs: chat.send\n\non join:\n  say \"Welcome, {player}!\"\nend on\n";

    private const string ManifestErrorSource =
        "# beacon 1\n# needs: server.send\n\non join:\n  say \"hi\"\nend on\n";

    private const string ForgivenSource =
        "# beacon 1\n\non chat when message contains \"x\"\n  cancel event\nend\n";

    [Theory]
    [InlineData("clean.bcn", CleanSource)]
    [InlineData("manifest.bcn", ManifestErrorSource)]
    [InlineData("forgiven.bcn", ForgivenSource)]
    public async Task SharedFixtures_InClientAndHeadless_AgreeByteForByte(string name, string source)
    {
        string root = NewRoot();
        string configs = Path.Combine(root, "configurations");
        Directory.CreateDirectory(configs);
        string path = Path.Combine(root, name);
        File.WriteAllText(path, source);

        // Headless: the exact JSON document the CLI prints.
        Assert.True(BeaconLintCli.TryParse(["--format", "json", path], out BeaconLintRequest? cliArgs, out _));
        int exit = BeaconLintCli.Execute(cliArgs!, stdinText: null, out string headlessJson, out _);
        Assert.True(exit is BeaconLint.ExitCodes.Clean or BeaconLint.ExitCodes.Errors);

        // In-client: the same engine behind /scripts lint --format json.
        await using Client client = BuildClient(configs);
        CmdResult result = await client.Commands.DispatchAsync($"scripts lint \"{path}\" --format json");
        Assert.Equal(CmdStatus.Done, result.Status);
        Assert.NotNull(result.Message);

        Assert.Equal(CanonicalDiagnostics(headlessJson), CanonicalDiagnostics(result.Message!));
    }

    [Fact]
    public async Task InClientLint_TextMode_ReportsElmView()
    {
        string root = NewRoot();
        string configs = Path.Combine(root, "configurations");
        Directory.CreateDirectory(configs);
        string path = Path.Combine(root, "manifest.bcn");
        File.WriteAllText(path, ManifestErrorSource);

        await using Client client = BuildClient(configs);
        CmdResult result = await client.Commands.DispatchAsync($"scripts lint \"{path}\"");
        Assert.Equal(CmdStatus.Fail, result.Status);
        Assert.Contains("B1001", result.Message, StringComparison.Ordinal);
        Assert.Contains("chat.send", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InClientLint_CleanFile_IsOk()
    {
        string root = NewRoot();
        string configs = Path.Combine(root, "configurations");
        Directory.CreateDirectory(configs);
        string path = Path.Combine(root, "clean.bcn");
        File.WriteAllText(path, CleanSource);

        await using Client client = BuildClient(configs);
        CmdResult result = await client.Commands.DispatchAsync($"scripts lint \"{path}\"");
        Assert.Equal(CmdStatus.Done, result.Status);
    }

    #region mute

    private static string WriteScriptInConfigs(string configs, string name, string source)
    {
        string scripts = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(configs))!, "scripts");
        Directory.CreateDirectory(scripts);
        string path = Path.Combine(scripts, name);
        File.WriteAllText(path, source);
        return path;
    }

    [Fact]
    public async Task Mute_On_GagsChatWithVisibility_Off_RestoresFailureWithoutSession()
    {
        string root = NewRoot();
        string configs = Path.Combine(root, "configurations");
        Directory.CreateDirectory(configs);
        WriteScriptInConfigs(configs, "loud.bcn", "# beacon 1\n# needs: chat.send\n\non start:\n  say \"hello\"\nend on\n");

        await using Client client = BuildClient(configs);

        CmdResult before = await client.Commands.DispatchAsync("scripts mute");
        Assert.Equal(CmdStatus.Done, before.Status);
        Assert.Contains("off", before.Message, StringComparison.OrdinalIgnoreCase);

        CmdResult on = await client.Commands.DispatchAsync("scripts mute on");
        Assert.Equal(CmdStatus.Done, on.Status);

        // Muted: the say is held, so the run succeeds with no live session.
        CmdResult run = await client.Commands.DispatchAsync("scripts run loud");
        Assert.Equal(CmdStatus.Done, run.Status);

        CmdResult list = await client.Commands.DispatchAsync("scripts list");
        Assert.Equal(CmdStatus.Done, list.Status);
        Assert.Contains("muted", list.Message, StringComparison.OrdinalIgnoreCase);

        // Unmuted with no session: the send has nowhere to go, so the run fails loudly.
        CmdResult off = await client.Commands.DispatchAsync("scripts mute off");
        Assert.Equal(CmdStatus.Done, off.Status);
        CmdResult loud = await client.Commands.DispatchAsync("scripts run loud");
        Assert.Equal(CmdStatus.Fail, loud.Status);
    }

    [Fact]
    public async Task Mute_HoldsCount_IsVisible()
    {
        string root = NewRoot();
        string configs = Path.Combine(root, "configurations");
        Directory.CreateDirectory(configs);
        WriteScriptInConfigs(configs, "loud.bcn", "# beacon 1\n# needs: chat.send\n\non start:\n  say \"hello\"\nend on\n");

        await using Client client = BuildClient(configs);
        await client.Commands.DispatchAsync("scripts mute on");
        await client.Commands.DispatchAsync("scripts run loud");

        CmdResult status = await client.Commands.DispatchAsync("scripts mute");
        Assert.Equal(CmdStatus.Done, status.Status);
        Assert.Contains("on", status.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1", status.Message, StringComparison.Ordinal);
    }

    #endregion
    #region stop all

    [Fact]
    public async Task StopAll_KillsEveryRunningScript()
    {
        string root = NewRoot();
        string configs = Path.Combine(root, "configurations");
        Directory.CreateDirectory(configs);
        WriteScriptInConfigs(configs, "one.bcn", "# beacon 1\n\non start:\n  show \"one\"\nend on\n");
        WriteScriptInConfigs(configs, "two.bcn", "# beacon 1\n\non start:\n  show \"two\"\nend on\n");

        await using Client client = BuildClient(configs);
        Assert.Equal(CmdStatus.Done, (await client.Commands.DispatchAsync("scripts run one")).Status);
        Assert.Equal(CmdStatus.Done, (await client.Commands.DispatchAsync("scripts run two")).Status);

        CmdResult list = await client.Commands.DispatchAsync("scripts list");
        Assert.Contains("one", list.Message, StringComparison.Ordinal);
        Assert.Contains("two", list.Message, StringComparison.Ordinal);

        CmdResult stop = await client.Commands.DispatchAsync("scripts stop all");
        Assert.Equal(CmdStatus.Done, stop.Status);
        Assert.Contains("2", stop.Message, StringComparison.Ordinal);

        CmdResult after = await client.Commands.DispatchAsync("scripts list");
        Assert.DoesNotContain("one", after.Message ?? string.Empty, StringComparison.Ordinal);
        Assert.DoesNotContain("two", after.Message ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stop_UnknownId_Fails()
    {
        string root = NewRoot();
        string configs = Path.Combine(root, "configurations");
        Directory.CreateDirectory(configs);

        await using Client client = BuildClient(configs);
        CmdResult stop = await client.Commands.DispatchAsync("scripts stop ghost");
        Assert.Equal(CmdStatus.Fail, stop.Status);
    }
    #endregion
}
