using DMCBK.Core;
using DMCBK.Core.Configuration;
using Umpk;
using Umpk.Data.Java;
using Umpk.Protocol.Java;
using Umpk.Protocol.Java.Codecs;
using Umpk.Protocol.Java.Packets;
using Umpk.Protocol.Java.Transport;
using DMCBK.Testing.Server;
using Xunit;

namespace Mcc.Cli.Tests.Fakes;

/// <summary>
/// Builds a REAL <see cref="Client"/> against a <see cref="FakeJavaServer"/> and drives a full handshake/login/configuration exchange to the start of play, for plugin-host suites that need a genuine UMPK session (real <c>ClientPluginContext</c>, real per-plugin <c>Detached</c> token, real <c>Events</c>/<c>Scheduler</c>) rather than a hand-rolled <c>ISessionScope</c> stub.
/// Mirrors UMPK's own <c>Umpk.Client.Tests.Support.PluginSessionHarness</c> pattern, adapted to build through <see cref="ClientBuilder"/> instead of <c>UmpkClientBuilder</c> directly: <see cref="ClientBuilder.UseProxy"/> routes the WHOLE session through a fake pipe connection factory (MCC has no lower-level connection-factory seam of its own; proxying is the one place it already lets a caller replace the transport), and <see cref="ClientBuilder.UseVersion"/> pins the version so no status ping is attempted.
/// </summary>
internal static class McPluginSessionHarness
{
    /// <summary>Generous bound for a hermetic exchange; real work should finish in milliseconds.</summary>
    public static readonly TimeSpan Budget = TimeSpan.FromSeconds(15);

    /// <summary>The pinned version every harness session speaks.</summary>
    public static JavaVersion Version { get; } = ResolveVersion();

    /// <summary>
    /// Builds an unconnected client wired to <paramref name="server"/>'s pipe, offline, physics/pathfinding off (no terrain needed for the login/config/play handshake this harness drives).
    /// </summary>
    public static Client BuildClient(
        FakeJavaServer server,
        string username,
        string pluginsRoot,
        Microsoft.Extensions.Logging.ILoggerFactory loggerFactory,
        IHostInterface? hostInterface = null,
        IConnectionFactory? connections = null,
        DmcbkConfiguration? configuration = null,
        JavaVersion? version = null)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginsRoot);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        ClientBuilder builder = new ClientBuilder()
            .UseUsername(username)
            .UseServer("test-harness", 25565)
            .UseVersion(version ?? Version)
            .UseProxy(connections ?? new PipeConnectionFactory(server.ClientPipe))
            .UseLoggerFactory(loggerFactory)
            .ConfigureFeatures(f =>
            {
                f.Physics = false;
                f.Pathfinding = false;
            });

        if (configuration is not null)
            builder.UseConfiguration(configuration);

        return (hostInterface is null ? builder : builder.UseHostInterface(hostInterface)).UseCommands().UseBeacon().Build();
    }

    /// <summary>Drives handshake, login and configuration to the start of play; caller starts/awaits StartAsync itself.</summary>
    public static async Task DriveLoginAsync(FakeJavaServer server, CancellationToken ct)
    {
        await server.NextFrameAsync(ct); // handshake (intention)
        server.ServerConnection.SetPhase(ProtocolPhase.Login);
        await server.NextFrameAsync(ct); // hello
        await SendAsync(server, ProtocolPhase.Login,
            new ClientboundLoginFinishedPacket(Guid.NewGuid(), "Tester", [], null), ct);
        await server.NextFrameAsync(ct); // login_acknowledged
        server.ServerConnection.SetPhase(ProtocolPhase.Configuration);
        await server.NextFrameAsync(ct); // client_information
        await SendAsync(server, ProtocolPhase.Configuration, new ClientboundFinishConfigurationPacket(), ct);
        await server.NextFrameAsync(ct); // finish_configuration (ack)
        server.ServerConnection.SetPhase(ProtocolPhase.Play);

        // UMPK's connect-time readiness gate: the client does not report a PLAY session until the first clientbound PLAY item crosses the wire, because vanilla switches its inbound decoder at that boundary rather than at login success.
        // Send the same response-free readiness frame UMPK's own
        // ScriptedServer.SendPlayReadinessFrameAsync sends (a benign time update needing no world);
        // without it StartAsync never completes.
        await SendAsync(server, ProtocolPhase.Play,
            new ClientboundSetTimePacket(GameTime: 0, DayTime: 0, TickDayTime: false, ClockUpdates: []), ct);
    }

    /// <summary>
    /// Sends a clientbound PLAY plugin message.
    /// There is no packet record for this one: the play-phase custom payload is routed raw by channel name, so the frame is built the way the wire has it, a string channel followed by the bytes.
    /// </summary>
    public static async Task SendPlayPluginMessageAsync(
        FakeJavaServer server, Identifier channel, byte[] data, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(server);
        ProtocolDescriptor descriptor = Version.Protocol;
        Assert.True(descriptor.TryGetRegistry(ProtocolPhase.Play, PacketFlow.Clientbound, out PhaseRegistry registry));

        Identifier customPayload = Identifier.Minecraft("custom_payload");
        foreach ((int wireId, PacketType type) in registry.Packets)
        {
            if (type.Id != customPayload)
                continue;

            var buffer = new System.Buffers.ArrayBufferWriter<byte>();
            var writer = new PacketWriter(buffer);
            writer.WriteString(channel.ToString());
            writer.WriteBytes(data);
            await server.SendFrameAsync(wireId, buffer.WrittenSpan.ToArray(), ct);
            return;
        }

        throw new Xunit.Sdk.XunitException("This version registers no clientbound play custom_payload.");
    }

    /// <summary>Encodes one clientbound packet for the negotiated version and sends it as a frame.</summary>
    public static async Task SendAsync<TPacket>(FakeJavaServer server, ProtocolPhase phase, TPacket packet, CancellationToken ct, JavaVersion? version = null)
        where TPacket : class, IPacket
    {
        ProtocolDescriptor descriptor = (version ?? Version).Protocol;
        Assert.True(descriptor.TryGetRegistry(phase, PacketFlow.Clientbound, out PhaseRegistry registry));
        foreach ((int wireId, PacketType type) in registry.Packets)
        {
            if (type.Id != packet.Type.Id)
                continue;

            Assert.True(registry.TryGetInbound(wireId, out BoundPacketCodec codec));
            var buffer = new System.Buffers.ArrayBufferWriter<byte>();
            var writer = new PacketWriter(buffer);
            codec.Encode(ref writer, packet, PacketCodecContext.Registryless);
            await server.SendFrameAsync(wireId, buffer.WrittenSpan.ToArray(), ct);
            return;
        }

        throw new Xunit.Sdk.XunitException($"{packet.Type.Id} is not registered clientbound in {phase}.");
    }

    private static JavaVersion ResolveVersion()
    {
        Assert.True(JavaVersions.TryGetByName("1.21.5", out JavaVersion version));
        return version;
    }

    private sealed class PipeConnectionFactory(System.IO.Pipelines.IDuplexPipe pipe) : IConnectionFactory
    {
        public ValueTask<System.IO.Pipelines.IDuplexPipe> ConnectAsync(ServerEndpoint endpoint, CancellationToken ct)
            => ValueTask.FromResult(pipe);
    }

    /// <summary>
    /// Hands out a pipe per host name and records every address it was asked for, so a test can assert WHERE the client dialled and not only that it connected.
    /// </summary>
    internal sealed class RoutingConnectionFactory(Dictionary<string, System.IO.Pipelines.IDuplexPipe> byHost) : IConnectionFactory
    {
        public List<ServerEndpoint> Dialed { get; } = [];

        public ValueTask<System.IO.Pipelines.IDuplexPipe> ConnectAsync(ServerEndpoint endpoint, CancellationToken ct)
        {
            Dialed.Add(endpoint);
            return byHost.TryGetValue(endpoint.Host, out System.IO.Pipelines.IDuplexPipe? pipe)
                ? ValueTask.FromResult(pipe)
                : throw new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.HostNotFound);
        }
    }
}
