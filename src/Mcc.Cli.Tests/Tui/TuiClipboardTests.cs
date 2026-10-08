using Mcc.Cli.Tui.Input;
using Mcc.Cli.Tui;
using Xunit;

namespace Mcc.Cli.Tests.Tui;

/// <summary>
/// The clipboard platform matrix: which native tools are tried, in which order, per OS and display session.
/// The Avalonia/OSC 52 layer above it needs a live UI and is verified by hand; everything below it is pure and pinned here, including the no-tools case (false, fast, no throw).
/// </summary>
public sealed class TuiClipboardTests
{
    [Fact]
    public void Windows_UsesClip()
        => Assert.Equal<(string, string)>([("clip", "")], TuiClipboard.Candidates(windows: true, macos: false, wayland: false, x11: false));

    [Fact]
    public void MacOs_UsesPbcopy()
        => Assert.Equal<(string, string)>([("pbcopy", "")], TuiClipboard.Candidates(windows: false, macos: true, wayland: false, x11: false));

    [Fact]
    public void Wayland_PrefersWlCopy()
        => Assert.Equal<(string, string)>(
            [("wl-copy", ""), ("xclip", "-selection clipboard"), ("xsel", "--clipboard --input")],
            TuiClipboard.Candidates(windows: false, macos: false, wayland: true, x11: false));

    [Fact]
    public void X11_PrefersXclipThenXselThenWlCopy()
        => Assert.Equal<(string, string)>(
            [("xclip", "-selection clipboard"), ("xsel", "--clipboard --input"), ("wl-copy", "")],
            TuiClipboard.Candidates(windows: false, macos: false, wayland: false, x11: true));

    [Fact]
    public void BothSessions_CoversEverythingWithoutDuplicates()
    {
        var candidates = TuiClipboard.Candidates(windows: false, macos: false, wayland: true, x11: true);

        Assert.Equal("wl-copy", candidates[0].File);
        Assert.Equal(candidates.Count, candidates.Distinct().Count());
    }

    [Fact]
    public void NeitherSession_CoversEverythingWithoutDuplicates()
    {
        var candidates = TuiClipboard.Candidates(windows: false, macos: false, wayland: false, x11: false);

        Assert.Contains(candidates, c => c.File == "wl-copy");
        Assert.Contains(candidates, c => c.File == "xclip");
        Assert.Contains(candidates, c => c.File == "xsel");
        Assert.Equal(candidates.Count, candidates.Distinct().Count());
    }

    [Fact]
    public async Task NoToolsInstalled_ReturnsFalseFast()
    {
        // Windows searches system directories even with an empty PATH. Use a missing absolute path.
        string empty = Path.Combine(Path.GetTempPath(), "mcc-clip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(empty);
        try
        {
            Assert.False(await TuiClipboard.TryCopyNativeAsync(
                "TV8DPLYN", [(Path.Combine(empty, "missing-clipboard-tool"), string.Empty)]));
        }
        finally
        {
            Directory.Delete(empty, recursive: true);
        }
    }

    /// <summary>
    /// The success path end to end: a stub <c>xclip</c> that files stdin away.
    /// The stub is created at runtime (no repo files, no real clipboard touched) and the test only runs where <c>sh</c> exists to interpret it.
    /// </summary>
    [Fact]
    public async Task WorkingTool_ReceivesTheText()
    {
        if (!OperatingSystem.IsLinux())
            return;

        string dir = Path.Combine(Path.GetTempPath(), "mcc-clip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string capture = Path.Combine(dir, "capture.txt");
        await File.WriteAllTextAsync(
            Path.Combine(dir, "xclip"),
            "#!/bin/sh\ncat > \"" + capture + "\"\n");
        File.SetUnixFileMode(
            Path.Combine(dir, "xclip"),
            UnixFileMode.UserExecute | UnixFileMode.UserRead | UnixFileMode.UserWrite);

        string? savedPath = Environment.GetEnvironmentVariable("PATH");
        string? savedDisplay = Environment.GetEnvironmentVariable("DISPLAY");
        string? savedWayland = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        Environment.SetEnvironmentVariable("PATH", dir + Path.PathSeparator + savedPath);
        // No real displays: X11 points nowhere (the stub wins before any real tool runs) and Wayland is off, so the order is deterministic and no attempt can block on a compositor.
        Environment.SetEnvironmentVariable("DISPLAY", ":9");
        Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", null);
        try
        {
            Assert.True(await TuiClipboard.TryCopyNativeAsync("TV8DPLYN"));
            Assert.Equal("TV8DPLYN", await File.ReadAllTextAsync(capture));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", savedPath);
            Environment.SetEnvironmentVariable("DISPLAY", savedDisplay);
            Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", savedWayland);
            Directory.Delete(dir, recursive: true);
        }
    }
}
