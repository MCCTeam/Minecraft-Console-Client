using DMCBK.Marketplace;
using Tomlet;

namespace Mcc.Cli.Plugins;

internal static class DefaultMarketplace
{
    internal const string IndexUrl = "https://raw.githubusercontent.com/MCCTeam/Marketplace/master/marketplace/mcc-marketplace.toml";

    internal static void EnsureRegistry(string configurationFolder)
    {
        string path = Path.Combine(configurationFolder, "marketplaces.toml");
        if (File.Exists(path))
            return;

        var registry = new MarketplaceRegistry
        {
            Marketplaces = [new MarketplaceBinding { Id = "official", Source = IndexUrl, AutoUpdate = "off" }],
        };
        Directory.CreateDirectory(configurationFolder);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, TomletMain.TomlStringFrom(registry));
            try
            {
                File.Move(temporary, path);
            }
            catch (IOException) when (File.Exists(path))
            {
                // Preserve a registry created by another process while this one was preparing defaults.
            }
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}
