using DMCBK.Core;

namespace Mcc.Cli.Diagnostics;

/// <summary>
/// Everything the client learned about the server it is on.
/// <para>
/// This is the file that answers "which server was this, and what kind of server is it" without anyone having to ask the reporter.
/// Brand and protocol together identify the software; the tick rate, the keep-alive cadence and the latency say whether it was healthy; the dimension bounds and world border are what a world or movement bug has to be read against.
/// </para>
/// <para>
/// Collected AFTER the join rather than at startup, because almost none of it exists before then: the brand arrives on a plugin channel, the tick rate is measured over several seconds, and the dimension is only known once the server has sent a login/respawn.
/// Every field is nullable for the same reason, and a missing one means "the server never told us", never zero.
/// </para>
/// </summary>
internal static class ServerReport
{
    /// <summary>
    /// Collects the server report, or returns null when there is no session to describe.
    /// </summary>
    /// <remarks>
    /// Null rather than a report of nulls, and the distinction is load-bearing.
    /// This is called once shortly after joining and again at shutdown, and by shutdown the session is usually already gone: a caller that wrote whatever came back would overwrite a good report from a healthy session with an empty one every single time, which is exactly the file a disconnect investigation needs and exactly the one it would lose.
    /// Every section below is still gathered independently, so one unavailable subsystem costs its own section and not the file.
    /// </remarks>
    public static async Task<object?> CollectAsync(Client client, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        SessionInfoSnapshot? session = await TryGet(() => client.Game.Session.GetInfoAsync(ct)).ConfigureAwait(false);
        if (session is null)
            return null;

        DimensionInfo? dimension = await TryGet(() => client.Game.World.GetDimensionAsync(ct)).ConfigureAwait(false);
        WorldBorderInfo? border = await TryGet(() => client.Game.World.GetWorldBorderAsync(ct)).ConfigureAwait(false);
        WorldTimeInfo? time = await TryGet(() => client.Game.World.GetTimeAsync(ct)).ConfigureAwait(false);
        TabListSnapshot? tabList = await TryGet(() => client.Game.Player.GetTabListAsync(ct)).ConfigureAwait(false);
        PlayerStatus? status = await TryGet(() => client.Game.Player.GetStatusAsync(ct)).ConfigureAwait(false);

        return new
        {
            Collected = DateTime.Now.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            Endpoint = new
            {
                session.Host,
                session.Port,
            },
            Version = new
            {
                session.VersionName,
                session.Protocol,
            },
            Software = new
            {
                // Null brand is a real signal, not a gap: proxies that strip the plugin channel and heavily modified servers both show up this way.
                session.Brand,
            },
            Health = new
            {
                // Null tick rate is UNKNOWN, never zero.
                // A server paused by pause-when-empty or held by /tick freeze stops broadcasting game time and the estimate expires back to null.
                session.TpsEstimate,

                // Server-measured round trip, from the tab list.
                // The only real latency figure a Java client has.
                session.ObservedLatencyMs,

                // Our own responder-side turnaround, NOT a round trip.
                // A large value here is a stalled session loop, which is the cause behind most keep-alive timeout disconnects.
                KeepAliveTurnaroundMs = session.KeepAliveTurnaround?.TotalMilliseconds,

                // Vanilla aims for 15 seconds.
                // Materially longer means the server is behind on its own network tick.
                KeepAliveIntervalSeconds = session.KeepAliveInterval?.TotalSeconds,
            },
            Dimension = dimension is null ? null : new
            {
                dimension.DimensionName,
                dimension.DimensionType,
                dimension.MinY,
                dimension.Height,
                dimension.MaxY,
                dimension.HasSkylight,
            },
            WorldBorder = border is null ? null : new
            {
                border.CenterX,
                border.CenterZ,
                border.Size,
                border.TargetSize,
                border.IsLerping,
                border.WarningBlocks,
                border.WarningTimeSeconds,
            },
            Time = time is null ? null : new
            {
                time.WorldAge,
                time.TimeOfDay,
                time.Day,
                time.IsDaytime,
            },
            Players = tabList is null ? null : new
            {
                Count = tabList.Entries.Count,

                // Names and game modes, not UUIDs.
                // A player's UUID is their account identity, and the population of a server tells the reader what they need (was it busy, was there an operator on) without it.
                Online = tabList.Entries.Select(e => new { e.Name, e.GameMode, e.Latency }).ToArray(),
            },
            LocalPlayer = status is null ? null : new
            {
                status.GameMode,
                status.IsSpectator,
                Position = new { status.Position.X, status.Position.Y, status.Position.Z },
                status.OnGround,
            },
        };
    }

    /// <summary>
    /// Runs one collector, returning null if it fails.
    /// Nothing here is worth taking the bundle down for: a report missing its world border is still a report.
    /// </summary>
    private static async Task<T?> TryGet<T>(Func<Task<T>> collector)
        where T : class
    {
        try
        {
            return await collector().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }
}
