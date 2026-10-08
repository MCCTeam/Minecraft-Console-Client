using System.Resources;
namespace Mcc.Cli.Localization;

internal static class TextResources
{
    private static readonly ResourceManager Resources = new("Mcc.Cli.Resources.TextResources", typeof(TextResources).Assembly);
    internal static string Format(string key, params object?[] args)
        => string.Format(System.Globalization.CultureInfo.CurrentCulture, Get(key), args);

    internal static string Get(string key) => Resources.GetString(key, DMCBK.Core.Localization.McStrings.Culture) ?? key;
}
