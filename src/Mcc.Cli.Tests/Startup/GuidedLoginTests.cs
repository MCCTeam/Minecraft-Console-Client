using Mcc.Cli.Startup;
using Mcc.Cli;
using DMCBK.Core;
using DMCBK.Core.Configuration;
using Xunit;

namespace Mcc.Cli.Tests.Startup;

/// <summary>
/// Coverage for the first-run login prompt and the provider-URL rules behind it: which configurations ask for an account, what each menu answer produces, and which provider URLs are accepted, rejected, or sent back to the prompt.
/// The probe is substituted so no test touches the network.
/// </summary>
public sealed class GuidedLoginTests
{
    private static DmcbkConfiguration ConfigWith(params ConfiguredAccount[] accounts)
        => new()
        {
            Accounts = new AccountsConfig { Accounts = accounts },
            ResolvedAccount = accounts.Length > 0 ? accounts[0] : new ConfiguredAccount(),
        };

    private static async Task<ConfiguredAccount?> RunAsync(
        IEnumerable<string> answers,
        List<string> written,
        AuthServerProbeResult probeResult = AuthServerProbeResult.Valid)
    {
        Queue<string> queue = new(answers);
        return await GuidedLogin.PromptAsync(
            written.Add,
            () => queue.Count > 0 ? queue.Dequeue() : null,
            (_, _) => Task.FromResult(probeResult));
    }

    [Fact]
    public void IsNeeded_NoAccounts_IsTrue()
        => Assert.True(GuidedLogin.IsNeeded(ConfigWith()));

    [Fact]
    public void IsNeeded_AccountWithNeitherNameNorLogin_IsTrue()
        => Assert.True(GuidedLogin.IsNeeded(ConfigWith(new ConfiguredAccount { Name = "  ", Login = "" })));

    [Fact]
    public void IsNeeded_AccountWithOnlyAName_IsFalse()
    {
        // ClientBuilder falls back to the name when the login is blank, so such an account CAN log in and must not be second-guessed by a prompt.
        Assert.False(GuidedLogin.IsNeeded(ConfigWith(new ConfiguredAccount { Name = "Steve" })));
    }

    [Fact]
    public void IsNeeded_AccountWithALogin_IsFalse()
        => Assert.False(GuidedLogin.IsNeeded(ConfigWith(new ConfiguredAccount { Login = "steve@example.com" })));

    [Theory]
    [InlineData("1")]
    [InlineData("offline")]
    [InlineData("  OFFLINE  ")]
    public async Task Offline_TakesAUsername(string answer)
    {
        List<string> written = [];
        ConfiguredAccount? account = await RunAsync([answer, " Steve "], written);

        Assert.NotNull(account);
        Assert.Equal(DmcbkAccountKind.Offline, account.Kind);
        Assert.Equal("Steve", account.Name);
        Assert.Equal("Steve", account.Login);
    }

    [Fact]
    public async Task Offline_TooLongUsername_AsksAgain()
    {
        List<string> written = [];
        ConfiguredAccount? account = await RunAsync(["1", new string('x', 17), "Steve"], written);

        Assert.Equal("Steve", account?.Login);
        Assert.Contains(written, w => w.Contains("1 to 16", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("2")]
    [InlineData("microsoft")]
    [InlineData("online")]
    public async Task Microsoft_AsksNothingElse(string answer)
    {
        List<string> written = [];
        ConfiguredAccount? account = await RunAsync([answer], written);

        Assert.NotNull(account);
        Assert.Equal(DmcbkAccountKind.MicrosoftDeviceCode, account.Kind);
        Assert.Null(account.AuthServer);

        // Only the menu itself was printed: the device-code flow resolves the profile on its own.
        Assert.Single(written);
    }

    [Fact]
    public async Task Yggdrasil_TakesAProviderUrl_AndNormalizesIt()
    {
        List<string> written = [];
        ConfiguredAccount? account = await RunAsync(["3", "http://127.0.0.1:25585/authlib-injector"], written);

        Assert.NotNull(account);
        Assert.Equal(DmcbkAccountKind.Yggdrasil, account.Kind);
        Assert.Equal("http://127.0.0.1:25585/authlib-injector/", account.AuthServer);

        // The username and password belong to the auth interaction at login time; asking here would ask twice.
        Assert.Equal(2, written.Count);
    }

    [Fact]
    public async Task Yggdrasil_MalformedUrl_AsksAgainWithoutProbing()
    {
        List<string> written = [];
        int probes = 0;
        Queue<string> answers = new(["3", "not-a-url", "https://auth.example.com/api/yggdrasil/"]);

        ConfiguredAccount? account = await GuidedLogin.PromptAsync(
            written.Add,
            () => answers.Count > 0 ? answers.Dequeue() : null,
            (_, _) =>
            {
                probes++;
                return Task.FromResult(AuthServerProbeResult.Valid);
            });

        Assert.Equal("https://auth.example.com/api/yggdrasil/", account?.AuthServer);
        Assert.Equal(1, probes);
        Assert.Contains(written, w => w.Contains("absolute HTTP", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(AuthServerProbeResult.Unreachable, "Could not reach")]
    [InlineData(AuthServerProbeResult.InvalidResponse, "did not return valid")]
    public async Task Yggdrasil_RejectedProvider_AsksAgain(AuthServerProbeResult result, string expected)
    {
        List<string> written = [];
        ConfiguredAccount? account = await RunAsync(["3", "https://auth.example.com/", ""], written, result);

        Assert.Null(account);
        Assert.Contains(written, w => w.Contains(expected, StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnknownAnswer_AsksAgain()
    {
        List<string> written = [];
        ConfiguredAccount? account = await RunAsync(["9", "1", "Steve"], written);

        Assert.Equal("Steve", account?.Login);
        Assert.Contains(written, w => w.Contains("1, 2, or 3", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BlankAnswer_GivesUp()
    {
        // Also what a closed stdin looks like: the loop has to end rather than spin forever unattended.
        List<string> written = [];
        Assert.Null(await RunAsync([""], written));
    }

    [Fact]
    public async Task EndOfInput_GivesUp()
    {
        List<string> written = [];
        Assert.Null(await RunAsync([], written));
    }

    /// <summary>
    /// The offline-name rule both hosts share (classic prompt and TUI dialog): trim, reject empty and over-long, accept everything else.
    /// </summary>
    [Theory]
    [InlineData("Steve", "Steve")]
    [InlineData("  Steve  ", "Steve")]
    [InlineData("0123456789abcdef", "0123456789abcdef")]
    public void CleanOfflineName_AcceptsAndTrims(string raw, string expected)
    {
        Assert.True(GuidedLogin.TryCleanOfflineName(raw, out string clean));
        Assert.Equal(expected, clean);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0123456789abcdefg")]
    public void CleanOfflineName_RejectsEmptyAndTooLong(string? raw)
        => Assert.False(GuidedLogin.TryCleanOfflineName(raw, out _));
}
