namespace Mcc.Cli.Diagnostics;

/// <summary>
/// The resolved <c>client.toml [Diagnostics]</c> settings.
/// </summary>
/// <param name="Enabled">
/// Whether a bundle is written at all.
/// On by default: the whole point is that a tester who hits a problem already has the evidence, and a setting they had to turn on beforehand would be off exactly when it mattered.
/// </param>
/// <param name="CapturePackets">
/// Whether raw wire frames are recorded.
/// The largest file in the bundle and the most useful one: it is what turns "it disconnected" into a specific packet.
/// </param>
/// <param name="MaxCaptureMegabytes">
/// The cap on the packet capture, across all chunks.
/// It is limited to 20 MB so diagnostics remain small enough to attach to a bug report.
/// </param>
/// <param name="KeepSessions">
/// How many zipped bundles to keep.
/// Older ones are deleted on startup.
/// </param>
internal sealed record DiagnosticsSettings(
    bool Enabled,
    bool CapturePackets,
    int MaxCaptureMegabytes,
    int KeepSessions)
{
    /// <summary>The hard size ceiling for packet capture within one diagnostics session.</summary>
    public const int MaxCaptureMegabytesLimit = 20;

    /// <summary>The hard upper bound for packet recording within one diagnostics session.</summary>
    public static TimeSpan MaxCaptureDuration { get; } = TimeSpan.FromMinutes(20);

    /// <summary>The configured cap in bytes, limited to the 20 MB hard ceiling.</summary>
    public long MaxCaptureBytes => (long)Math.Clamp(MaxCaptureMegabytes, 1, MaxCaptureMegabytesLimit) * 1024 * 1024;

    /// <summary>What a config with no <c>[Diagnostics]</c> table gets.</summary>
    public static DiagnosticsSettings Default { get; } = new(
        Enabled: true,
        CapturePackets: true,
        MaxCaptureMegabytes: MaxCaptureMegabytesLimit,
        KeepSessions: 5);
}
