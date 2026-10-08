using Mcc.Cli.Hosting;
using Mcc.Cli.Presentation;
using Mcc.Cli.Localization;
using DMCBK.Marketplace;

namespace Mcc.Cli.Plugins;

/// <summary>Validates schema-v2 index and release metadata without loading packages or application configuration.</summary>
internal static class MarketplaceValidateOneShot
{
    internal static async Task<int?> HandleAsync(string[] args)
    {
        if (args.Length == 0 || args[0] != "--validate-marketplace") return null;
        if (args.Length != 2)
        {
            HostConsole.WriteErrorLine(MccStrings.Get("marketplace.validation.usage"));
            return HostExit.Usage;
        }
        string cache = Path.Combine(Path.GetTempPath(), "mcc-marketplace-validation-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var http = new HttpClient();
            var reader = new MarketplaceCatalogueClient(http, cache);
            MarketplaceIndex index = await reader.ReadIndexAsync(args[1]).ConfigureAwait(false);
            MarketplaceSnapshot snapshot = await reader.RefreshAsync(new() { Id = index.Id, Source = args[1] }).ConfigureAwait(false);
            HostConsole.WriteLine(MccStrings.Format("marketplace.validation.success", index.Id, snapshot.Index.Plugins.Count));
            return HostExit.Clean;
        }
        catch (Exception exception) when (exception is MarketplaceException or IOException or UnauthorizedAccessException
                                             or HttpRequestException or FormatException or ArgumentException or Tomlet.Exceptions.TomlException)
        {
            string message = exception is MarketplaceException failure
                ? MarketplaceDiagnostics.Format(failure, System.Globalization.CultureInfo.CurrentUICulture)
                : exception.Message;
            HostConsole.WriteErrorLine(MccStrings.Format("marketplace.validation.failure", message));
            return HostExit.Usage;
        }
        finally { if (Directory.Exists(cache)) Directory.Delete(cache, recursive: true); }
    }
}
