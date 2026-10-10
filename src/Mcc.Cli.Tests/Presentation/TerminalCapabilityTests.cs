using Mcc.Cli.Presentation;
using Mcc.Cli;
using Xunit;

namespace Mcc.Cli.Tests.Presentation;

/// <summary>
/// Color gating: <see cref="TerminalCapability.ResolveColor"/> folds the config toggle, the NO_COLOR convention, and the MCC_FORCE_COLOR override (which the live spread sets so the ANSI path is exercised even when stdout is redirected to a log file).
/// These tests mutate the two env vars, so they share a collection to run without parallel interference and always restore prior values.
/// </summary>
[Collection("terminal-capability-env")]
public sealed class TerminalCapabilityTests
{
    [Fact]
    public void ConfigDisabled_IsAlwaysOff()
    {
        WithEnv(noColor: null, forceColor: "1", () => Assert.False(TerminalCapability.ResolveColor(false)));
    }

    [Fact]
    public void NoColor_ForcesOff_EvenWhenConfigEnabled()
    {
        WithEnv(noColor: "1", forceColor: null, () => Assert.False(TerminalCapability.ResolveColor(true)));
    }

    [Fact]
    public void ForceColor_TurnsColorOn_WhenConfigEnabled()
    {
        WithEnv(noColor: null, forceColor: "1", () => Assert.True(TerminalCapability.ResolveColor(true)));
    }

    [Fact]
    public void NoColor_WinsOverForceColor()
    {
        WithEnv(noColor: "1", forceColor: "1", () => Assert.False(TerminalCapability.ResolveColor(true)));
    }

    private static void WithEnv(string? noColor, string? forceColor, Action body)
    {
        string? prevNo = Environment.GetEnvironmentVariable("NO_COLOR");
        string? prevForce = Environment.GetEnvironmentVariable("MCC_FORCE_COLOR");
        try
        {
            Environment.SetEnvironmentVariable("NO_COLOR", noColor);
            Environment.SetEnvironmentVariable("MCC_FORCE_COLOR", forceColor);
            body();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NO_COLOR", prevNo);
            Environment.SetEnvironmentVariable("MCC_FORCE_COLOR", prevForce);
        }
    }
}
