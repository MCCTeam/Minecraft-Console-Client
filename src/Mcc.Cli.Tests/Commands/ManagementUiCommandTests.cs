using DMCBK.Core;
using DMCBK.Core.Commands;
using Umpk.Auth;
using Xunit;

namespace Mcc.Cli.Tests.Commands;

public sealed class ManagementUiCommandTests
{
    [Theory]
    [InlineData("help ui", CommandBrowserTab.Commands)]
    [InlineData("man ui", CommandBrowserTab.Manual)]
    public async Task CommandAndManualUi_OpenTheRequestedSharedBrowserTab(
        string command, CommandBrowserTab expectedTab)
    {
        var ui = new RecordingUi();
        await using Client client = BuildClient(ui);

        CmdResult result = await client.Commands.DispatchAsync(command);

        Assert.Equal(CmdStatus.Done, result.Status);
        Assert.Equal(expectedTab, ui.OpenedCommandTab);
    }

    [Fact]
    public async Task ScriptsUi_OpensTheScriptManager()
    {
        var ui = new RecordingUi();
        await using Client client = BuildClient(ui);

        CmdResult result = await client.Commands.DispatchAsync("scripts ui");

        Assert.Equal(CmdStatus.Done, result.Status);
        Assert.True(ui.ScriptManagerOpened);
    }

    [Fact]
    public async Task MccMenu_OpensTheManagementLauncher()
    {
        var ui = new RecordingUi();
        await using Client client = BuildClient(ui);

        CmdResult result = await client.Commands.DispatchAsync("mcc-menu");

        Assert.Equal(CmdStatus.Done, result.Status);
        Assert.True(ui.MccMenuOpened);
    }

    [Theory]
    [InlineData("recipebook ui")]
    [InlineData("entity ui")]
    [InlineData("achievement ui")]
    [InlineData("advancements ui")]
    [InlineData("chunk ui")]
    [InlineData("scoreboard ui")]
    public async Task SessionBrowsers_ParseTheirExplicitUiVerb(string command)
    {
        var ui = new RecordingUi();
        await using Client client = BuildClient(ui);

        CmdResult result = await client.Commands.DispatchAsync(command);

        Assert.Equal(CmdStatus.Fail, result.Status);
        Assert.NotNull(result.Message);
        Assert.DoesNotContain("<--[HERE]", result.Message, StringComparison.Ordinal);
    }

    private static Client BuildClient(IHostUi ui)
    {
        Client client = new ClientBuilder().UseCommands().UseBeacon()
            .UseUsername("Tester")
            .UseServer("localhost")
            .UseHostInterface(new UiHost(ui))
            .Build();
        client.Commands.RegisterHostCommand(new Mcc.Cli.Commands.MccMenuCommand());
        return client;
    }

    private sealed class RecordingUi : IHostUi, Mcc.Cli.Commands.IMccNavigation
    {
        public bool ScriptManagerOpened { get; private set; }

        public bool MccMenuOpened { get; private set; }

        public CommandBrowserTab? OpenedCommandTab { get; private set; }

        public bool TryOpenScriptManager()
        {
            ScriptManagerOpened = true;
            return true;
        }

        public bool TryOpenMccMenu()
        {
            MccMenuOpened = true;
            return true;
        }

        public bool TryOpenCommandBrowser(CommandBrowserTab initialTab)
        {
            OpenedCommandTab = initialTab;
            return true;
        }
    }

    private sealed class UiHost(IHostUi ui) : IHostInterface
    {
        public IUserPrompt? Prompt => null;

        public IAuthInteraction? AuthInteraction => null;

        public ICommandOutput? CommandOutput => null;

        public IHostUi Ui { get; } = ui;
    }
}
