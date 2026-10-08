using DMCBK.Core;
using Mcc.Cli.Hosting;
using System.Buffers;
using System.IO.Pipelines;
using System.Security.Cryptography;
using Mcc.Cli;
using DMCBK.Core.Commands;
using Umpk;
using Umpk.Client;
using Umpk.Data.Java;
using Umpk.Protocol.Java;
using Umpk.Protocol.Java.Codecs;
using Umpk.Protocol.Java.Packets;
using Umpk.Protocol.Java.Transport;
using Xunit;

namespace Mcc.Cli.Tests.Hosting;

/// <summary>
/// A session the server had already killed kept reporting itself healthy and quit with exit code 0.
/// Three separate defects, and these are their three separate pins.
/// <list type="number">
/// <item>
/// <b>Detection.</b> The loss was noticed only when something tried to WRITE, because the only thing that ends a session is UMPK's <c>Disconnected</c> event and that event is published by the read loop when the read loop finds out.
/// A read that never returns never finds out, and UMPK built its connection with <c>ReadIdleTimeout = TimeSpan.Zero</c>, so the transport's own backstop was off and no knob turned it on.
/// UMPK now owns this directly (its own <c>ReadIdleTimeoutTests</c>): <see cref="ClientOptions.ReadIdleTimeout"/> flows straight into the transport's read-idle backstop, bounded by vanilla's own client-side <c>ReadTimeoutHandler(30)</c>, and <see cref="Client.LivenessTimeout"/> is now a thin override for it rather than a second, MCC-owned watchdog.
/// </item>
/// <item>
/// <b>Stale state.</b> With the status honest, the gate every live-state command already has does the right thing: <c>health</c> refuses instead of answering 20/20 from a snapshot with no upper bound on its age.
/// The gate was never the defect; the status that fed it was.
/// </item>
/// <item>
/// <b>Exit code.</b> A bare <c>quit</c> after a server-side kill returned 0, the documented CLEAN exit.
/// <see cref="HostExit.Resolve"/> answers 3, while still honouring an explicit <c>exit &lt;code&gt;</c> and still answering 0 for a session that was lost and then re-established.
/// </item>
/// </list>
/// <para>
/// The end-to-end case runs a REAL UMPK login against <see cref="JavaServerLogin"/> over an in-memory pipe pair, so the client reaches Playing through the same code a live server drives, and then the server goes silent WITHOUT closing the pipe.
/// That silent-but-open state is the one the transport cannot see and is what makes the test a pin rather than a restatement: with the watchdog removed the client sits in Playing forever and the test times out.
/// </para>
/// </summary>
public sealed class DeadSessionTests
{
    private const string TestVersion = "1.21.5";

    // Not 25565: the default port is the one that triggers the SRV lookup, and this session never touches DNS.
    private const ushort InMemoryPort = 25599;

    // ---------------------------------------------------------------------------------------------------
    // 1. Detection
    // ---------------------------------------------------------------------------------------------------

    // Watchdog_EndsTheWait_WhenTheServerGoesSilent, Watchdog_StaysQuiet_WhileInboundFramesKeepArriving, Watchdog_DefersToARealDisconnect, Watchdog_Disabled_NeverSynthesizesAnEnd and Liveness_MeasuresOnlyWhatTheServerSent all pinned MCC's own SessionLiveness watchdog and Client.WaitForSessionEndAsync, both deleted: the backstop is UMPK's own transport-level ReadIdleTimeout now (pinned by UMPK's Umpk.Client.Tests.ReadIdleTimeoutTests), not a second, MCC-owned poll loop.

    // IdleTimeout_IsAConnectionLoss_AndIsRetryable pinned that an idle timeout classifies as ConnectionLost and is retryable; both are now UMPK's own facts (Umpk.Client.Tests.ClientStatusTests. Classify_MapsEveryCloseReasonOntoAKind and IsRetryable_ConnectionLostOrKick_IsRetryable), which run DisconnectInfo.Classify and ReconnectPolicy.IsRetryable directly rather than through an MCC wrapper.

    // ---------------------------------------------------------------------------------------------------
    // 2. Exit code
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public void Quit_AfterAServerSideKill_Is3_NotClean()
    {
        var control = new HostControl();
        HostExit.ObserveStatus(control, Playing());
        Assert.Equal(HostExit.Clean, HostExit.Resolve(control));

        HostExit.ObserveStatus(control, RemotelyStopped());
        control.RequestExit(0); // a bare "quit"

        Assert.Equal(HostExit.ConnectionLost, HostExit.Resolve(control));
    }

    [Fact]
    public void Quit_AfterACleanSession_IsStillClean()
    {
        var control = new HostControl();
        HostExit.ObserveStatus(control, Playing());
        HostExit.ObserveStatus(control, LocallyStopped());
        control.RequestExit(0);

        Assert.Equal(HostExit.Clean, HostExit.Resolve(control));
    }

    [Fact]
    public void ExplicitExitCode_WinsOverTheSessionOutcome()
    {
        var control = new HostControl();
        HostExit.ObserveStatus(control, RemotelyStopped());
        control.RequestExit(0, explicitCode: true);

        // "exit 0" is an instruction, not a default.
        // It must not be rewritten to 3.
        Assert.Equal(0, HostExit.Resolve(control));

        var other = new HostControl();
        HostExit.ObserveStatus(other, RemotelyStopped());
        other.RequestExit(7, explicitCode: true);
        Assert.Equal(7, HostExit.Resolve(other));
    }

    [Fact]
    public void AReconnectedSession_ClearsTheLoss()
    {
        var control = new HostControl();
        HostExit.ObserveStatus(control, RemotelyStopped());
        HostExit.ObserveStatus(control, Playing());
        control.RequestExit(0);

        // reco/connect (or the auto-reconnect supervisor) put us back on a server; quitting from there is 0.
        Assert.Equal(HostExit.Clean, HostExit.Resolve(control));
    }

    [Fact]
    public void NoExitCommand_FallsBackToTheCallersCode()
    {
        // The initial-connect-failure branch passes 3 as its fallback and must keep getting it.
        Assert.Equal(HostExit.ConnectionLost, HostExit.Resolve(new HostControl(), HostExit.ConnectionLost));
        Assert.Equal(HostExit.Clean, HostExit.Resolve(new HostControl()));
    }

    // ---------------------------------------------------------------------------------------------------
    // 3. A write that fails because the transport is gone
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public void BrokenPipe_IsClassifiedAsALostConnection()
    {
        // The live observation, verbatim: an IOException wrapping the socket error.
        var brokenPipe = new IOException(
            "Unable to write data to the transport connection: Broken pipe.",
            new System.Net.Sockets.SocketException(32));

        Assert.True(Client.IsTransportFault(brokenPipe));
        Assert.True(Client.IsTransportFault(new ConnectionClosedException(CloseReason.SocketEof, "closed")));

        // Not everything that fails a send is a lost connection: a refused action is still just a refusal, and a locally torn-down session is a local stop.
        Assert.False(Client.IsTransportFault(new InvalidOperationException("not in a session")));
        Assert.False(Client.IsTransportFault(new ObjectDisposedException("client")));
        Assert.False(Client.IsTransportFault(null));
    }

    // ---------------------------------------------------------------------------------------------------
    // 4. End to end: a real login, then a silent-but-open connection
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task ASilentServer_EndsTheSession_RefusesStateReads_AndExits3()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

        // The window now also covers the login handshake (including the RSA key exchange), because UMPK's own ReadIdleTimeout is armed from connection.Start() rather than only once play begins; 500 ms used to be plenty of margin past just the silent-server wait, but is tight against the login leg too, so this is widened to keep the margin.
        await using LiveSession session = await LiveSession
            .StartAsync("b1dead", TimeSpan.FromSeconds(2), cts.Token);

        // The session is genuinely live: the login completed on both legs and the client says so.
        Assert.Equal(ClientStatus.Playing, session.Client.Status);
        Assert.Equal(HostExit.Clean, HostExit.Resolve(session.Control));

        // The server now sends nothing at all, and the pipe stays OPEN.
        // No socket event will ever fire.
        await session.Stopped.WaitAsync(TimeSpan.FromSeconds(30), cts.Token);

        Assert.Equal(ClientStatus.Disconnected, session.Client.Status);
        Assert.NotNull(session.Client.LastDisconnect);
        Assert.Equal(CloseReason.IdleTimeout, session.Client.LastDisconnect!.Reason);
        Assert.Equal(DisconnectKind.ConnectionLost, session.Client.LastDisconnect.Kind);

        // Stale state: health no longer answers 20/20 from a snapshot the connection can never refresh.
        CmdResult health = await session.Client.Commands.DispatchAsync("health");
        Assert.Equal(CmdStatus.Fail, health.Status);
        Assert.Equal(CommandText.NotConnected(noPrefix: false, prefix: '/'), health.Message);

        // Exit code: a bare quit from here is 3.
        session.Control.RequestExit(0);
        Assert.Equal(HostExit.ConnectionLost, HostExit.Resolve(session.Control));
    }

    [Fact]
    public async Task ATalkingServer_KeepsTheSessionAlive_PastTheWindow()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var window = TimeSpan.FromSeconds(2);
        await using LiveSession session = await LiveSession.StartAsync("b1alive", window, cts.Token);

        // Frames for longer than the window, so a backstop that was not actually reading them would have fired.
        // An unmapped wire id on purpose: UMPK's read-idle timer resets on every successful frame read regardless of whether any codec can decode it (UnknownPacketPolicy.Preserve), so this exercises that without asking any codec to decode a fabricated payload.
        for (int i = 0; i < 30; i++)
        {
            await session.Server.SendFrameAsync(0x7FFF, new byte[] { 1, 2, 3 }, cts.Token);
            await Task.Delay(100, cts.Token);
        }

        Assert.Equal(ClientStatus.Playing, session.Client.Status);

        // The direct pin on the frame tap itself (independent of whether the backstop got round to firing) moved to UMPK's own Umpk.Client.Tests.ReadIdleTimeoutTests.ATalkingServerNeverTimesOut_PastTheWindow, now that the backstop is UMPK's transport-level ReadIdleTimeout rather than an MCC-owned watchdog fed by an MCC-side frame subscription.
    }

    [Fact]
    public async Task ABrokenPipeOnSend_EndsTheSession_InsteadOfReportingARawTransportError()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));

        // A window far longer than the test, so nothing here can be credited to the idle watchdog: the only thing that can end this session is the failed write.
        await using LiveSession session = await LiveSession
            .StartAsync("b1pipe", TimeSpan.FromMinutes(10), cts.Token);
        Assert.Equal(ClientStatus.Playing, session.Client.Status);

        // Half-open the way a real failure is: reads still block, writes fail.
        // The loss stays hidden until a chat send tries to go out, which is what exposes it.
        session.BreakWrites();

        InputRouting routing = await session.Client.Commands.HandleInputAsync("T5DEADCHECK", cts.Token);

        Assert.NotNull(routing.Result);
        Assert.Equal(CmdStatus.Fail, routing.Result!.Status);
        // Named as the loss it is.
        // The transport detail is still carried, through the same DisconnectDescription.Describe every host renders, so the cause is not thrown away either: what changed is that the raw transport string is no longer the WHOLE message.
        Assert.StartsWith("Connection lost:", routing.Result.Message, StringComparison.Ordinal);
        Assert.Contains(CommandStrings.DisconnectSocketEof, routing.Result.Message, StringComparison.Ordinal);

        await session.Stopped.WaitAsync(TimeSpan.FromSeconds(30), cts.Token);
        Assert.Equal(ClientStatus.Disconnected, session.Client.Status);

        // And the very next state read refuses instead of answering from the snapshot, which is the exact pair of adjacent lines the live transcript recorded.
        CmdResult health = await session.Client.Commands.DispatchAsync("health");
        Assert.Equal(CmdStatus.Fail, health.Status);
        Assert.Equal(CommandText.NotConnected(noPrefix: false, prefix: '/'), health.Message);
    }

    /// <summary>
    /// One real MCC session over an in-memory pipe pair: <see cref="JavaServerLogin"/> on one end, the whole <see cref="Client"/> stack on the other.
    /// After <see cref="StartAsync"/> returns, the server sends nothing unless the test tells it to, and the pipe is never closed, which is the silent-but-open state no socket event reports.
    /// </summary>
    private sealed class LiveSession : IAsyncDisposable
    {
        private readonly RSA _keyPair;
        private readonly HalfOpenPipe _clientPipe;

        private LiveSession(
            Client client, JavaConnection server, RSA keyPair, HostControl control, Task stopped, HalfOpenPipe clientPipe)
        {
            Client = client;
            Server = server;
            _keyPair = keyPair;
            Control = control;
            Stopped = stopped;
            _clientPipe = clientPipe;
        }

        /// <summary>Makes every further write fail the way a broken pipe does, leaving reads blocked.</summary>
        public void BreakWrites() => _clientPipe.Break();

        public Client Client { get; }

        public JavaConnection Server { get; }

        public HostControl Control { get; }

        public Task Stopped { get; }

        public static async Task<LiveSession> StartAsync(string username, TimeSpan window, CancellationToken ct)
        {
            Assert.True(JavaVersions.TryGetByName(TestVersion, out JavaVersion version));

            DuplexPipePair pipes = DuplexPipePair.Create();
            var serverConnection = new JavaConnection(pipes.Right, new JavaConnectionOptions
            {
                UnknownPacketPolicy = UnknownPacketPolicy.Preserve,
                ReadIdleTimeout = TimeSpan.Zero,
            });
            serverConnection.BindCodec(new DescriptorFrameCodecBinding(version.Protocol), PacketFlow.Serverbound);
            serverConnection.Start();

            RSA keyPair = RSA.Create(2048);
            Task<ServerLoginResult> serverLogin = JavaServerLogin.AcceptAsync(
                serverConnection,
                version,
                new JavaServerLoginOptions { KeyPair = keyPair, Verifier = null, CompressionThreshold = -1 },
                ct);

            var clientPipe = new HalfOpenPipe(pipes.Left);
            Client client = new ClientBuilder().UseCommands().UseBeacon()
                .UseServer("localhost", InMemoryPort)
                .UseUsername(username)
                .UseVersion(version)
                .UseProxy(new InMemoryConnectionFactory(clientPipe), forPing: false)
                .Build();

            // Vanilla's window is 30 s; a test cannot wait that long.
            // The window is the ONLY thing scaled.
            client.LivenessTimeout = window;

            var control = new HostControl();
            var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            client.StatusChanged += (_, e) =>
            {
                HostExit.ObserveStatus(control, e);
                if (e.Current == ClientStatus.Disconnected)
                    stopped.TrySetResult();
            };

            Task clientStart = client.StartAsync(ct);
            await serverLogin.WaitAsync(TimeSpan.FromSeconds(30), ct);

            // UMPK's connect-time readiness gate: the client reports no PLAY session until the first clientbound PLAY item crosses the wire (vanilla's JoinGame boundary).
            // A real server always sends it; this leg must too, or StartAsync never returns and there is no live session to go silent.
            // Silence is measured after this one frame.
            await SendPlayReadinessAsync(serverConnection, version, ct);
            await clientStart.WaitAsync(TimeSpan.FromSeconds(30), ct);
            return new LiveSession(client, serverConnection, keyPair, control, stopped.Task, clientPipe);
        }

        /// <summary>
        /// One valid, response-free clientbound PLAY item (a benign time update needing no world), mirroring UMPK's own ScriptedServer.SendPlayReadinessFrameAsync for a raw server leg.
        /// </summary>
        private static async Task SendPlayReadinessAsync(JavaConnection server, JavaVersion version, CancellationToken ct)
        {
            ProtocolDescriptor descriptor = version.Protocol;
            Assert.True(descriptor.TryGetRegistry(ProtocolPhase.Play, PacketFlow.Clientbound, out PhaseRegistry registry));
            var readiness = new ClientboundSetTimePacket(GameTime: 0, DayTime: 0, TickDayTime: false, ClockUpdates: []);
            foreach ((int wireId, PacketType type) in registry.Packets)
            {
                if (type.Id != readiness.Type.Id)
                    continue;

                Assert.True(registry.TryGetInbound(wireId, out BoundPacketCodec codec));
                var buffer = new ArrayBufferWriter<byte>();
                var writer = new PacketWriter(buffer);
                codec.Encode(ref writer, readiness, PacketCodecContext.Registryless);
                await server.SendFrameAsync(wireId, buffer.WrittenMemory, ct);
                return;
            }

            Assert.Fail($"{readiness.Type.Id} is not registered clientbound in {ProtocolPhase.Play}.");
        }

        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
            await Server.DisposeAsync();
            _keyPair.Dispose();
        }
    }

    private static ClientStatusChangedEventArgs Playing()
        => new(ClientStatus.Connecting, ClientStatus.Playing, null);

    private static ClientStatusChangedEventArgs RemotelyStopped()
        => new(
            ClientStatus.Playing,
            ClientStatus.Disconnected,
            new DisconnectInfo { Reason = CloseReason.IdleTimeout });

    private static ClientStatusChangedEventArgs LocallyStopped()
        => new(
            ClientStatus.Playing,
            ClientStatus.Disconnected,
            new DisconnectInfo { Reason = CloseReason.Local });

    /// <summary>Hands the client one end of an in-memory pipe pair instead of a socket.</summary>
    private sealed class InMemoryConnectionFactory(IDuplexPipe pipe) : IConnectionFactory
    {
        public ValueTask<IDuplexPipe> ConnectAsync(ServerEndpoint endpoint, CancellationToken ct)
            => ValueTask.FromResult(pipe);
    }

    /// <summary>
    /// The client's end of the link, with a switch that makes writes fail while reads stay blocked.
    /// That asymmetry IS the defect's setting: the peer is gone, the read never returns to say so, and the write is the only operation that finds out.
    /// </summary>
    private sealed class HalfOpenPipe(IDuplexPipe inner) : IDuplexPipe
    {
        private readonly BreakableWriter _output = new(inner.Output);

        public PipeReader Input { get; } = inner.Input;

        public PipeWriter Output => _output;

        public void Break() => _output.Break();

        private sealed class BreakableWriter(PipeWriter inner) : PipeWriter
        {
            private volatile bool _broken;

            public void Break() => _broken = true;

            public override void Advance(int bytes) => inner.Advance(bytes);

            public override void CancelPendingFlush() => inner.CancelPendingFlush();

            public override void Complete(Exception? exception = null) => inner.Complete(exception);

            public override Memory<byte> GetMemory(int sizeHint = 0) => inner.GetMemory(sizeHint);

            public override Span<byte> GetSpan(int sizeHint = 0) => inner.GetSpan(sizeHint);

            public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
            {
                // The live message, verbatim, over the exception type the socket stack actually raises.
                return _broken
                    ? ValueTask.FromException<FlushResult>(new IOException(
                        "Unable to write data to the transport connection: Broken pipe.",
                        new System.Net.Sockets.SocketException(32)))
                    : inner.FlushAsync(cancellationToken);
            }
        }
    }
}
