using System.Threading;
using System.Threading.Tasks;
using DMCBK.Core.Commands;
using DMCBK.PluginSdk;
using Umpk.Commands;

public sealed class ReleaseSmoke : IPlugin
{
    public void Configure(PluginDescriptor descriptor)
    {
        descriptor.Id = "release-smoke";
        descriptor.Version = "1.0.0";
        descriptor.ApiVersion = PluginApiVersion.Major;
    }

    public Task ActivateAsync(PluginContext context)
    {
        context.Commands.Register(new SmokeCommand());
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private sealed class SmokeCommand : CommandBase
    {
        public override string CmdName => "release-smoke";
        public override string CmdDesc => "Release packaging smoke test.";
        public override string CmdUsage => "release-smoke";

        public override void Register(CommandBuilder<CommandContext> builder)
            => builder.Literal(CmdName, command => command.Executes(
                call => call.Source.Result.Ok("RELEASE_SOURCE_PLUGIN_OK")));
    }
}
