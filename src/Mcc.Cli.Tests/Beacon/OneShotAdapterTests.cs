using Mcc.Cli.BeaconTooling;
using Mcc.Cli;
using DMCBK.Core.Beacon;
using Xunit;

namespace Mcc.Cli.Tests.Beacon;

/// <summary>
/// Adapter characterization for the three Beacon one-shot Console adapters (lint, run, format).
/// Pins only the transport contract: exact argv[0] matching, stdin read only after a successful requested parse, nonempty stdout before nonempty stderr with WriteLine newlines, empty streams left unwritten, and usage and execution exit codes.
/// Engine behavior stays covered by LintTests, FormatTests, and ScriptToolingTests.
/// Tests that redirect Console share this non-parallel collection and restore every saved stream in finally.
/// </summary>
[CollectionDefinition("console-io", DisableParallelization = true)]
public sealed class ConsoleIoCollection
{
}

[Collection("console-io")]
public sealed class OneShotAdapterTests : IDisposable
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
        string root = Path.Combine(Path.GetTempPath(), "mcc-beacon-oneshot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        _roots.Add(root);
        string path = Path.Combine(root, name);
        File.WriteAllText(path, content);
        return path;
    }

    private const string CleanSource =
        "# beacon 1\n# needs: chat.send\n\non join:\n  say \"Welcome, {player}!\"\nend on\n";

    private sealed class RecordingWriter(string sink, List<string> order) : StringWriter
    {
        public override void WriteLine(string? value)
        {
            order.Add(sink);
            base.WriteLine(value);
        }

        public override void WriteLine()
        {
            order.Add(sink);
            base.WriteLine();
        }
    }

    private static bool Invoke(
        string verb,
        string[] argv,
        TextReader? stdin,
        out int exit,
        out string outText,
        out string errText,
        List<string>? order = null)
    {
        TextWriter savedOut = Console.Out;
        TextWriter savedErr = Console.Error;
        TextReader savedIn = Console.In;
        using StringWriter outWriter = order is null ? new StringWriter() : new RecordingWriter("out", order);
        using StringWriter errWriter = order is null ? new StringWriter() : new RecordingWriter("err", order);
        Console.SetOut(outWriter);
        Console.SetError(errWriter);
        Console.SetIn(stdin ?? TextReader.Null);
        bool handled;
        try
        {
            handled = verb switch
            {
                "lint" => BeaconLintOneShot.TryHandle(argv, out exit),
                "run" => BeaconRunOneShot.TryHandle(argv, out exit),
                "format" => BeaconFormatOneShot.TryHandle(argv, out exit),
                _ => throw new ArgumentOutOfRangeException(nameof(verb)),
            };
        }
        finally
        {
            outText = outWriter.ToString();
            errText = errWriter.ToString();
            Console.SetOut(savedOut);
            Console.SetError(savedErr);
            Console.SetIn(savedIn);
        }

        Assert.Same(savedOut, Console.Out);
        Assert.Same(savedErr, Console.Error);
        Assert.Same(savedIn, Console.In);
        return handled;
    }

    [Theory]
    [InlineData("lint", "lint", true)]
    [InlineData("lint", "run", false)]
    [InlineData("lint", "format", false)]
    [InlineData("lint", "Lint", false)]
    [InlineData("lint", "./lint/", false)]
    [InlineData("lint", "lintx", false)]
    [InlineData("run", "run", true)]
    [InlineData("run", "lint", false)]
    [InlineData("run", "format", false)]
    [InlineData("run", "Run", false)]
    [InlineData("run", "runx", false)]
    [InlineData("format", "format", true)]
    [InlineData("format", "lint", false)]
    [InlineData("format", "run", false)]
    [InlineData("format", "Format", false)]
    [InlineData("format", "formatx", false)]
    public void ExactTokenAtZero_IsHandled(string verb, string token, bool expected)
    {
        Assert.Equal(expected, Invoke(verb, [token], null, out _, out _, out _));
    }

    [Theory]
    [InlineData("lint")]
    [InlineData("run")]
    [InlineData("format")]
    public void UnmatchedArguments_AreNotHandled(string verb)
    {
        Assert.False(Invoke(verb, [], null, out _, out _, out _));
        Assert.False(Invoke(verb, ["other", verb], null, out _, out _, out _));
        Assert.False(Invoke(verb, ["--format", "json", verb], null, out _, out _, out _));
    }

    [Theory]
    [InlineData("lint")]
    [InlineData("run")]
    [InlineData("format")]
    public void UnknownFlag_UsageFailure_WritesStderrOnly(string verb)
    {
        Assert.True(Invoke(verb, [verb, "--frobnicate", "x.bcn"], null, out int exit, out string outText, out string errText));
        Assert.Equal(BeaconLint.ExitCodes.Usage, exit);
        Assert.Equal(2, exit);
        Assert.Equal(string.Empty, outText);
        Assert.Contains("unknown flag", errText, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("lint")]
    [InlineData("run")]
    [InlineData("format")]
    public void BareVerb_ExecutesUsage_WritesStderrOnly(string verb)
    {
        Assert.True(Invoke(verb, [verb], null, out int exit, out string outText, out string errText));
        Assert.Equal(BeaconLint.ExitCodes.Usage, exit);
        Assert.Equal(string.Empty, outText);
        Assert.Contains("no input files", errText, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("lint")]
    [InlineData("run")]
    [InlineData("format")]
    public void FailedParse_BeforeStdin_LeavesInputUnread(string verb)
    {
        string[] argv = verb switch
        {
            "lint" => ["lint", "--stdin", "--format", "yaml", "x.bcn"],
            "run" => ["run", "--stdin", "--format", "yaml", "x.bcn"],
            _ => ["format", "--stdin", "--frobnicate", "x.bcn"],
        };
        const string sentinel = "piped source stays unread";
        using var stdin = new StringReader(sentinel);
        Assert.True(Invoke(verb, argv, stdin, out int exit, out string outText, out _));
        Assert.Equal(2, exit);
        Assert.Equal(string.Empty, outText);
        Assert.Equal(sentinel, stdin.ReadToEnd());
    }

    [Fact]
    public void RequestedStdin_LintReportsUnderGivenName()
    {
        using var stdin = new StringReader(CleanSource);
        string[] argv = ["lint", "--stdin", "--stdin-name", "quiz.bcn", "--format", "json"];
        Assert.True(Invoke("lint", argv, stdin, out int exit, out string outText, out string errText));
        Assert.Equal(BeaconLint.ExitCodes.Clean, exit);
        Assert.Contains("quiz.bcn", outText, StringComparison.Ordinal);
        Assert.Equal(string.Empty, errText);
        Assert.EndsWith(Environment.NewLine, outText, StringComparison.Ordinal);
    }

    [Fact]
    public void RequestedStdin_FormatPrintsFormattedWithNewline()
    {
        using var stdin = new StringReader("# beacon 1\nshow 1 + 2   ");
        Assert.True(Invoke("format", ["format", "--stdin", "--stdin-name", "piped.bcn"], stdin, out int exit, out string outText, out string errText));
        Assert.Equal(BeaconLint.ExitCodes.Clean, exit);
        Assert.Equal("# beacon 1\nshow 1 + 2\n" + Environment.NewLine, outText);
        Assert.Equal(string.Empty, errText);
    }

    [Fact]
    public void RequestedStdin_RunExecutesPipedSource()
    {
        using var stdin = new StringReader("# beacon 1\nshow \"hi\"\n");
        Assert.True(Invoke("run", ["run", "--stdin", "--stdin-name", "piped.bcn"], stdin, out int exit, out string outText, out string errText));
        Assert.Equal(BeaconLint.ExitCodes.Clean, exit);
        Assert.Contains("hi", outText, StringComparison.Ordinal);
        Assert.Equal(string.Empty, errText);
    }

    [Fact]
    public void NonStdinFileInput_LeavesStdinUnread()
    {
        string path = NewFile("clean.bcn", CleanSource);
        const string sentinel = "stdin must survive a file run";
        using var stdin = new StringReader(sentinel);
        Assert.True(Invoke("lint", ["lint", path], stdin, out int exit, out string outText, out string errText));
        Assert.Equal(BeaconLint.ExitCodes.Clean, exit);
        Assert.Equal(0, exit);
        Assert.Contains("ok", outText, StringComparison.Ordinal);
        Assert.Equal(string.Empty, errText);
        Assert.Equal(sentinel, stdin.ReadToEnd());
    }

    [Fact]
    public void ExecutionExitCodes_PassThrough()
    {
        string broken = NewFile("broken.bcn", "# beacon 1\n# needs: server.send\n\non join:\n  say \"hi\"\nend on\n");
        Assert.True(Invoke("lint", ["lint", broken], null, out int lintExit, out string lintOut, out _));
        Assert.Equal(BeaconLint.ExitCodes.Errors, lintExit);
        Assert.Equal(1, lintExit);
        Assert.NotEmpty(lintOut);

        string messy = NewFile("messy.bcn", "# beacon 1\nshow 1 + 2   ");
        Assert.True(Invoke("format", ["format", "--check", messy], null, out int formatExit, out string formatOut, out _));
        Assert.Equal(BeaconLint.ExitCodes.Errors, formatExit);
        Assert.Contains("would change", formatOut, StringComparison.Ordinal);
        Assert.Equal("# beacon 1\nshow 1 + 2   ", File.ReadAllText(messy));

        string failing = NewFile("fail.bcn", "# beacon 1\nassert(1 is 2, \"bot survived\")\n");
        Assert.True(Invoke("run", ["run", failing], null, out int runExit, out _, out _));
        Assert.Equal(BeaconLint.ExitCodes.Errors, runExit);
    }

    [Fact]
    public void BothStreamsNonempty_StdoutWrittenBeforeStderr()
    {
        var order = new List<string>();
        using var stdin = new StringReader("# beacon 1\nsay \"unterminated   \n");
        Assert.True(Invoke("format", ["format", "--stdin", "--stdin-name", "piped.bcn"], stdin, out int exit, out string outText, out string errText, order));
        Assert.Equal(BeaconLint.ExitCodes.Errors, exit);
        Assert.NotEmpty(outText);
        Assert.NotEmpty(errText);
        Assert.EndsWith(Environment.NewLine, outText, StringComparison.Ordinal);
        Assert.EndsWith(Environment.NewLine, errText, StringComparison.Ordinal);
        Assert.Equal(["out", "err"], order);
    }

    [Fact]
    public void ConsoleStreams_RestoredAfterHandle_IncludingExceptionPath()
    {
        TextWriter savedOut = Console.Out;
        TextWriter savedErr = Console.Error;
        TextReader savedIn = Console.In;

        Invoke("lint", ["lint", "--frobnicate", "x.bcn"], null, out _, out _, out _);
        Assert.Same(savedOut, Console.Out);
        Assert.Same(savedErr, Console.Error);
        Assert.Same(savedIn, Console.In);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Invoke("bogus", ["bogus"], null, out _, out _, out _);
        });
        Assert.Same(savedOut, Console.Out);
        Assert.Same(savedErr, Console.Error);
        Assert.Same(savedIn, Console.In);
    }
}
