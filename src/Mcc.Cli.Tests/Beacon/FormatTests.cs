using DMCBK.Core;
using Mcc.Cli.BeaconTooling;
using Mcc.Cli;
using DMCBK.Core.Beacon;
using DMCBK.Core.Commands;
using DMCBK.Core.Configuration;
using Umpk.Auth;
using Xunit;

namespace Mcc.Cli.Tests.Beacon;

/// <summary>
/// The formatter: structural normalization plus line hygiene, idempotent, safe on unparseable input, with headless exit codes matching lint.
/// </summary>
public sealed class FormatTests : IDisposable
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

    private string NewFile(string name, string content)
    {
        string root = Path.Combine(Path.GetTempPath(), "mcc-beacon-format-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        _roots.Add(root);
        string path = Path.Combine(root, name);
        File.WriteAllText(path, content);
        return path;
    }

    private const string CleanSource =
        "# beacon 1\n# needs: chat.send\n\non join:\n  say \"Welcome, {player}!\"\nend on\n";

    [Fact]
    public void AlreadyFormatted_NoChanges()
    {
        BeaconFormatResult result = BeaconFormat.FormatSource("c.bcn", CleanSource);
        Assert.False(result.Changed);
        Assert.Equal(CleanSource, result.Formatted);
        Assert.Equal(string.Empty, result.Diff);
        Assert.False(result.HadErrors);
    }

    [Fact]
    public void TrailingWhitespace_Trimmed()
    {
        BeaconFormatResult result = BeaconFormat.FormatSource(
            "t.bcn", "# beacon 1\nshow 1 + 2   \n");
        Assert.True(result.Changed);
        Assert.Equal("# beacon 1\nshow 1 + 2\n", result.Formatted);
        Assert.Contains("- show 1 + 2   ", result.Diff, StringComparison.Ordinal);
        Assert.Contains("+ show 1 + 2", result.Diff, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingFinalNewline_Added()
    {
        BeaconFormatResult result = BeaconFormat.FormatSource("n.bcn", "# beacon 1\nshow 1 + 2");
        Assert.True(result.Changed);
        Assert.Equal("# beacon 1\nshow 1 + 2\n", result.Formatted);
    }

    [Fact]
    public void Crlf_NormalizedToLf()
    {
        BeaconFormatResult result = BeaconFormat.FormatSource(
            "w.bcn", "# beacon 1\r\nshow 1 + 2\r\n");
        Assert.True(result.Changed);
        Assert.Equal("# beacon 1\nshow 1 + 2\n", result.Formatted);
        Assert.DoesNotContain("\r", result.Formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void IndentNormalization_Applies()
    {
        BeaconFormatResult result = BeaconFormat.FormatSource(
            "i.bcn", "# beacon 1\n\non chat when message contains \"x\"\n say \"hi\"\nend on\n");
        Assert.True(result.Changed);
        Assert.Contains("\n  say \"hi\"\n", result.Formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void BareEnd_Completed()
    {
        BeaconFormatResult result = BeaconFormat.FormatSource(
            "e.bcn", "# beacon 1\n\non chat when message contains \"x\"\n  say \"hi\"\nend\n");
        Assert.True(result.Changed);
        Assert.Contains("\nend on\n", result.Formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void CancelEvent_Rewritten()
    {
        BeaconFormatResult result = BeaconFormat.FormatSource(
            "c.bcn", "# beacon 1\n\non chat when message contains \"x\"\n  cancel event\nend on\n");
        Assert.True(result.Changed);
        Assert.Contains("\n  stop event\n", result.Formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("cancel event", result.Formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void Formatting_IsIdempotent()
    {
        const string messy =
            "# beacon 1\n\non chat when message contains \"x\"   \n say \"hi\"  \nend\n";
        BeaconFormatResult once = BeaconFormat.FormatSource("m.bcn", messy);
        Assert.True(once.Changed);
        BeaconFormatResult twice = BeaconFormat.FormatSource("m.bcn", once.Formatted);
        Assert.False(twice.Changed);
        Assert.Equal(once.Formatted, twice.Formatted);
    }

    [Fact]
    public void Unparseable_StillTrimsAndReportsErrors()
    {
        BeaconFormatResult result = BeaconFormat.FormatSource(
            "b.bcn", "# beacon 1\nsay \"unterminated   \n");
        Assert.True(result.Changed);
        Assert.Equal("# beacon 1\nsay \"unterminated\n", result.Formatted);
        Assert.True(result.HadErrors);
    }

    [Fact]
    public void Empty_StaysEmpty()
    {
        BeaconFormatResult result = BeaconFormat.FormatSource("e.bcn", string.Empty);
        Assert.False(result.Changed);
        Assert.Equal(string.Empty, result.Formatted);
    }

    [Fact]
    public void Cli_NoFiles_ExitUsage()
    {
        Assert.True(BeaconFormatCli.TryParse([], out BeaconFormatCliArgs? args, out _));
        int exit = BeaconFormatCli.Execute(args!, null, out _, out string stderr);
        Assert.Equal(BeaconLint.ExitCodes.Usage, exit);
        Assert.Contains("Usage", stderr, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_UnknownFlag_ExitUsage()
    {
        Assert.False(BeaconFormatCli.TryParse(["--frobnicate", "x.bcn"], out _, out string error));
        Assert.Contains("unknown flag", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_WriteMode_FormatsFileOnDisk()
    {
        string path = NewFile("messy.bcn", "# beacon 1\nshow 1 + 2   ");
        Assert.True(BeaconFormatCli.TryParse([path], out BeaconFormatCliArgs? args, out string error), error);
        int exit = BeaconFormatCli.Execute(args!, null, out string stdout, out _);
        Assert.Equal(BeaconLint.ExitCodes.Clean, exit);
        Assert.Equal("# beacon 1\nshow 1 + 2\n", File.ReadAllText(path));
        Assert.Contains("formatted", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_Check_WouldChangeExitOneAndWritesNothing()
    {
        string path = NewFile("messy.bcn", "# beacon 1\nshow 1 + 2   ");
        Assert.True(BeaconFormatCli.TryParse(["--check", path], out BeaconFormatCliArgs? args, out string error), error);
        int exit = BeaconFormatCli.Execute(args!, null, out string stdout, out _);
        Assert.Equal(BeaconLint.ExitCodes.Errors, exit);
        Assert.Equal("# beacon 1\nshow 1 + 2   ", File.ReadAllText(path));
        Assert.Contains("would change", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_Check_CleanExitZero()
    {
        string path = NewFile("clean.bcn", CleanSource);
        Assert.True(BeaconFormatCli.TryParse(["--check", path], out BeaconFormatCliArgs? args, out string error), error);
        int exit = BeaconFormatCli.Execute(args!, null, out string stdout, out _);
        Assert.Equal(BeaconLint.ExitCodes.Clean, exit);
        Assert.Contains("already formatted", stdout, StringComparison.Ordinal);
    }

    [Fact]
    public void Cli_Errors_ExitOne()
    {
        string path = NewFile(
            "manifest.bcn", "# beacon 1\n# needs: server.send\n\non join:\n  say \"hi\"\nend on\n");
        Assert.True(BeaconFormatCli.TryParse([path], out BeaconFormatCliArgs? args, out string error), error);
        int exit = BeaconFormatCli.Execute(args!, null, out _, out _);
        Assert.Equal(BeaconLint.ExitCodes.Errors, exit);
    }

    [Fact]
    public void Cli_Stdin_PrintsFormatted()
    {
        Assert.True(
            BeaconFormatCli.TryParse(["--stdin", "--stdin-name", "piped.bcn"], out BeaconFormatCliArgs? args, out string error),
            error);
        int exit = BeaconFormatCli.Execute(args!, "# beacon 1\nshow 1 + 2   ", out string stdout, out _);
        Assert.Equal(BeaconLint.ExitCodes.Clean, exit);
        Assert.Equal("# beacon 1\nshow 1 + 2\n", stdout);
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

    [Fact]
    public async Task InClient_Format_RewritesFile()
    {
        string root = Path.Combine(Path.GetTempPath(), "mcc-beacon-format-" + Guid.NewGuid().ToString("N"));
        string configs = Path.Combine(root, "configurations");
        string scripts = Path.Combine(root, "scripts");
        Directory.CreateDirectory(scripts);
        _roots.Add(root);
        string path = Path.Combine(scripts, "messy.bcn");
        File.WriteAllText(path, "# beacon 1\nshow 1 + 2   ");

        await using Client client = BuildClient(configs);
        CmdResult result = await client.Commands.DispatchAsync("scripts format messy");
        Assert.Equal(CmdStatus.Done, result.Status);
        Assert.Contains("Formatted", result.Message ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal("# beacon 1\nshow 1 + 2\n", File.ReadAllText(path));
    }

    [Fact]
    public async Task InClient_FormatCheck_WritesNothing()
    {
        string root = Path.Combine(Path.GetTempPath(), "mcc-beacon-format-" + Guid.NewGuid().ToString("N"));
        string configs = Path.Combine(root, "configurations");
        string scripts = Path.Combine(root, "scripts");
        Directory.CreateDirectory(scripts);
        _roots.Add(root);
        string path = Path.Combine(scripts, "messy.bcn");
        const string messy = "# beacon 1\nshow 1 + 2   ";
        File.WriteAllText(path, messy);

        await using Client client = BuildClient(configs);
        CmdResult result = await client.Commands.DispatchAsync("scripts format messy --check");
        Assert.Equal(CmdStatus.Done, result.Status);
        Assert.Contains("would change", result.Message ?? string.Empty, StringComparison.Ordinal);
        Assert.Equal(messy, File.ReadAllText(path));
    }
}
