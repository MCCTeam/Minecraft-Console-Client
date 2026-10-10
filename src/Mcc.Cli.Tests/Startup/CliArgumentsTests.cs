using Mcc.Cli.Startup;
using Mcc.Cli;
using Xunit;

namespace Mcc.Cli.Tests.Startup;

/// <summary>
/// CLI-binding coverage: the positional contract (<c>username password|- host</c>, configurations folder flag-only), dotted <c>--section.setting=value</c> overrides, the flag forms, and error handling.
/// Precedence (config &lt; positional &lt; dotted) and the offline <c>-</c> semantics are covered end-to-end in <see cref="ConfigurationOverrideTests"/>.
/// </summary>
public sealed class CliArgumentsTests
{
    [Fact]
    public void HarnessContract_ParsesUsernamePasswordAddress()
    {
        Assert.True(CliArguments.TryParse(["MCCBot", "-", "localhost:25565"], out CliArguments parsed, out _));

        Assert.Null(parsed.ConfigArg);
        Assert.Equal("MCCBot", parsed.Overrides.Username);
        Assert.Equal("-", parsed.Overrides.Password);
        Assert.Equal("localhost:25565", parsed.Overrides.Address);
    }

    [Fact]
    public void NoArgs_LeavesOverridesEmpty()
    {
        Assert.True(CliArguments.TryParse([], out CliArguments parsed, out _));

        Assert.Null(parsed.ConfigArg);
        Assert.True(parsed.Overrides.IsEmpty);
    }

    [Theory]
    [InlineData("--configurations", "./my-config")]
    public void ConfigurationsFlag_SpaceForm_MapsToConfigArg(string flag, string value)
    {
        Assert.True(CliArguments.TryParse([flag, value, "u"], out CliArguments parsed, out _));

        Assert.Equal("./my-config", parsed.ConfigArg);
        Assert.Equal("u", parsed.Overrides.Username);
    }

    [Fact]
    public void ConfigurationsFlag_EqualsForm_MapsToConfigArg()
    {
        Assert.True(CliArguments.TryParse(["--configurations=./my-config"], out CliArguments parsed, out _));

        Assert.Equal("./my-config", parsed.ConfigArg);
        Assert.True(parsed.Overrides.IsEmpty);
    }

    [Fact]
    public void ConfigurationsFlag_MissingValue_IsAnError()
    {
        Assert.False(CliArguments.TryParse(["--configurations"], out _, out string? error));
        Assert.NotNull(error);
    }

    [Fact]
    public void DottedFlag_IsCollected()
    {
        Assert.True(CliArguments.TryParse(
            ["u", "-", "h", "--gameplay.terrain=true", "--connection.version=1.16.5"],
            out CliArguments parsed, out _));

        Assert.Contains(parsed.Overrides.Dotted, kv => kv.Key == "gameplay.terrain" && kv.Value == "true");
        Assert.Contains(parsed.Overrides.Dotted, kv => kv.Key == "connection.version" && kv.Value == "1.16.5");
    }

    [Fact]
    public void VersionFlag_MapsToOverride()
    {
        Assert.True(CliArguments.TryParse(["u", "-", "h", "-v", "1.21.5"], out CliArguments parsed, out _));
        Assert.Equal("1.21.5", parsed.Overrides.Version);
    }

    [Theory]
    [InlineData("--auth", "microsoft")]
    public void AuthFlag_SpaceForm_MapsToOverride(string flag, string value)
    {
        Assert.True(CliArguments.TryParse(["u", "-", "h", flag, value], out CliArguments parsed, out _));
        Assert.Equal("microsoft", parsed.Overrides.AuthMode);
    }

    [Fact]
    public void AuthFlag_EqualsForm_MapsToOverride()
    {
        Assert.True(CliArguments.TryParse(["u", "-", "h", "--auth=yggdrasil", "--auth-server=https://ely.by"],
            out CliArguments parsed, out _));
        Assert.Equal("yggdrasil", parsed.Overrides.AuthMode);
        Assert.Equal("https://ely.by", parsed.Overrides.AuthServer);
    }

    [Fact]
    public void MissingFlagValue_IsAnError()
    {
        Assert.False(CliArguments.TryParse(["u", "-", "h", "-v"], out _, out string? error));
        Assert.NotNull(error);
    }

    [Fact]
    public void UnknownSingleDashFlag_IsAnError()
    {
        Assert.False(CliArguments.TryParse(["-x"], out _, out string? error));
        Assert.NotNull(error);
    }

    [Fact]
    public void LoneDash_IsTreatedAsPositionalPassword()
    {
        Assert.True(CliArguments.TryParse(["user", "-"], out CliArguments parsed, out _));
        Assert.Equal("-", parsed.Overrides.Password);
    }

    [Fact]
    public void ExerciseFlag_SpaceForm_IsParsed()
    {
        Assert.True(CliArguments.TryParse(["u", "-", "h", "--exercise", "smoke"], out CliArguments parsed, out _));
        Assert.Equal("smoke", parsed.Exercise);
    }

    [Fact]
    public void ExerciseFlag_EqualsForm_IsParsed()
    {
        Assert.True(CliArguments.TryParse(["u", "-", "h", "--exercise=smoke"], out CliArguments parsed, out _));
        Assert.Equal("smoke", parsed.Exercise);
    }

    [Fact]
    public void ExerciseFlag_IsNotADottedOverride()
    {
        Assert.True(CliArguments.TryParse(["--exercise=smoke"], out CliArguments parsed, out _));
        Assert.Empty(parsed.Overrides.Dotted);
        Assert.Equal("smoke", parsed.Exercise);
    }

    [Fact]
    public void NoExerciseFlag_LeavesExerciseNull()
    {
        Assert.True(CliArguments.TryParse(["u", "-", "h"], out CliArguments parsed, out _));
        Assert.Null(parsed.Exercise);
    }

    [Fact]
    public void ExerciseFlag_MissingValue_IsAnError()
    {
        Assert.False(CliArguments.TryParse(["u", "-", "h", "--exercise"], out _, out string? error));
        Assert.NotNull(error);
    }

    /// <summary>
    /// Four words used to be the config-first order.
    /// It must fail loudly now: silently shifting every slot over is exactly the misparse the flag-only folder fixed.
    /// </summary>
    [Fact]
    public void FourthPositional_IsAnErrorPointingAtConfigurations()
    {
        Assert.False(CliArguments.TryParse(["cfg", "u", "-", "h"], out _, out string? error));

        Assert.NotNull(error);
        Assert.Contains("--configurations", error, StringComparison.Ordinal);
    }

    /// <summary>
    /// A username can never look like a path, so one that does is the old config-first order.
    /// </summary>
    [Theory]
    [InlineData("./my-config")]
    [InlineData("client.toml")]
    [InlineData("configs\\main")]
    public void PathLikeUsername_IsAnErrorPointingAtConfigurations(string username)
    {
        Assert.False(CliArguments.TryParse([username], out _, out string? error));

        Assert.NotNull(error);
        Assert.Contains("--configurations", error, StringComparison.Ordinal);
    }
}
