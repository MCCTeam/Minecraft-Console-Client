using Mcc.Cli.Startup;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Mcc.Cli.Diagnostics;

/// <summary>
/// The machine the client is running on, as a bug report needs it.
/// <para>
/// Written once at startup into its own file, because "works here, not there" is answered by comparing two of these and by nothing else.
/// Everything collected is about the MACHINE: no user name, no home directory, no environment dump.
/// A tester should be able to read this file and see nothing about themselves in it.
/// </para>
/// </summary>
internal static class EnvironmentReport
{
    /// <summary>Collects the system report.</summary>
    public static object Collect()
    {
        return new
        {
            Os = new
            {
                Description = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.OSArchitecture.ToString(),
                Platform = Environment.OSVersion.Platform.ToString(),
                Version = Environment.OSVersion.VersionString,
                Is64Bit = Environment.Is64BitOperatingSystem,
            },
            Cpu = new
            {
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                LogicalCores = Environment.ProcessorCount,
            },
            Memory = new
            {
                // The GC's view of installed memory.
                // Not the whole picture on every platform, but it is the number that predicts whether a big render distance will hold.
                TotalAvailableMegabytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024),
                WorkingSetMegabytes = Environment.WorkingSet / (1024 * 1024),
            },
            Runtime = new
            {
                Framework = RuntimeInformation.FrameworkDescription,
                RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
                ServerGc = System.Runtime.GCSettings.IsServerGC,

                // The unit of every timestamp in the packet capture, and the one number without which those timestamps are unreadable.
                // Stopwatch ticks are not TimeSpan ticks and the rate is the machine's, not the platform's: 1e9 here, 1e7 on Windows.
                // The capture header has no room for it, and a bundle is read on someone else's machine by definition, so it is recorded here and MANIFEST.md points at it.
                StopwatchTicksPerSecond = System.Diagnostics.Stopwatch.Frequency,
            },
            Locale = new
            {
                // The decimal separator has caused real defects here: a comma locale turned coordinate triples into what looked like six numbers, so the culture is worth recording.
                Culture = CultureInfo.CurrentCulture.Name,
                UiCulture = CultureInfo.CurrentUICulture.Name,
                DecimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,
                TimeZone = TimeZoneInfo.Local.Id,
            },
            Terminal = new
            {
                Term = Environment.GetEnvironmentVariable("TERM"),
                ColorTerm = Environment.GetEnvironmentVariable("COLORTERM"),
                OutputEncoding = SafeEncoding(),
                OutputRedirected = Console.IsOutputRedirected,
                InputRedirected = Console.IsInputRedirected,
                Width = SafeWidth(),
                Height = SafeHeight(),
            },
            Process = new
            {
                // The build id is what pins a report to a commit.
                // Without it, "which version" is a question the reporter has to answer and usually cannot.
                MccVersion = ClientVersion.Current,
                Started = DateTime.Now.ToString("O", CultureInfo.InvariantCulture),
                Is64Bit = Environment.Is64BitProcess,
                CommandLineArgumentCount = Environment.GetCommandLineArgs().Length,
            },
        };
    }

    private static string SafeEncoding()
    {
        try
        {
            return Console.OutputEncoding.WebName;
        }
        catch (IOException)
        {
            return "unknown";
        }
    }

    private static int SafeWidth()
    {
        try
        {
            return Console.WindowWidth;
        }
        catch (IOException)
        {
            return 0;
        }
    }

    private static int SafeHeight()
    {
        try
        {
            return Console.WindowHeight;
        }
        catch (IOException)
        {
            return 0;
        }
    }
}
