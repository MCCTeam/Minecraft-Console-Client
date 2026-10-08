using System.Reflection;

namespace Mcc.Cli.Startup;

/// <summary>
/// The assembly informational version, resolved once and shared by every place that reports the client's own version (the classic banner, the TUI banner, and the server-status panel), so they can never disagree about what "the client version" is.
/// </summary>
internal static class ClientVersion
{
    /// <summary>The informational version (falls back to the assembly version when unset).</summary>
    public static string Current { get; } = Resolve();

    private static string Resolve()
    {
        Assembly assembly = typeof(ClientVersion).Assembly;
        string? informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return string.IsNullOrEmpty(informational) ? assembly.GetName().Version?.ToString() ?? "0.0.0" : informational;
    }
}
