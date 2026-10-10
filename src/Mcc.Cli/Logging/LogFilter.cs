using System.Text.RegularExpressions;
using DMCBK.Core.Configuration;

namespace Mcc.Cli.Logging;

/// <summary>
/// A configurable line filter re-homing the legacy <c>FilteredLogger</c> behavior: a regex plus a <see cref="LogFilterMode"/>.
/// In <see cref="LogFilterMode.Disable"/> mode every line shows; in <see cref="LogFilterMode.Whitelist"/> mode only matching lines show; in <see cref="LogFilterMode.Blacklist"/> mode matching lines are dropped.
/// An invalid pattern is treated as no filter (fail open) so a bad regex never silences all output.
/// </summary>
internal sealed class LogFilter
{
    private readonly Regex? _regex;
    private readonly LogFilterMode _mode;

    /// <summary>Builds a filter from a pattern and mode. The pattern is ignored in <see cref="LogFilterMode.Disable"/>.</summary>
    public LogFilter(string pattern, LogFilterMode mode)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        _mode = mode;
        if (mode != LogFilterMode.Disable)
        {
            try
            {
                _regex = new Regex(pattern, RegexOptions.CultureInvariant);
            }
            catch (ArgumentException)
            {
                _regex = null;
            }
        }
    }

    /// <summary>Whether a line (matched against its plain, un-colored text) should be shown.</summary>
    public bool ShouldShow(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);
        if (_mode == LogFilterMode.Disable || _regex is null)
            return true;

        bool match = _regex.IsMatch(plainText);
        return _mode == LogFilterMode.Whitelist ? match : !match;
    }
}
