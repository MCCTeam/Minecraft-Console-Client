using DMCBK.Marketplace;
using Mcc.Cli.Plugins;
using Xunit;

namespace Mcc.Cli.Tests.Plugins;

public sealed class DefaultMarketplaceTests
{
    [Fact]
    public void FreshConfigurationIncludesOfficialPublisherWithAutomaticUpdatesOff()
    {
        string folder = Path.Combine(Path.GetTempPath(), "mcc-default-market-" + Guid.NewGuid().ToString("N"));
        try
        {
            DefaultMarketplace.EnsureRegistry(folder);
            MarketplaceRegistry registry = MarketplaceRegistry.Parse(File.ReadAllText(Path.Combine(folder, "marketplaces.toml")));
            MarketplaceBinding binding = Assert.Single(registry.Marketplaces);
            Assert.Equal("official", binding.Id);
            Assert.Equal(DefaultMarketplace.IndexUrl, binding.Source);
            Assert.Equal("off", binding.AutoUpdate);
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    [Theory]
    [InlineData("schema-version = 2\nmarketplaces = []\n")]
    [InlineData("schema-version = 2\n[[marketplaces]]\nid = \"official\"\nsource = \"https://custom.example/index.toml\"\nauto-update = \"check\"\n")]
    public void ExistingRegistryPreservesRemovalAndCustomBindings(string original)
    {
        string folder = Path.Combine(Path.GetTempPath(), "mcc-existing-market-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "marketplaces.toml");
        try
        {
            File.WriteAllText(path, original);
            DefaultMarketplace.EnsureRegistry(folder);
            Assert.Equal(original, File.ReadAllText(path));
            Assert.Single(Directory.GetFiles(folder));
        }
        finally { Directory.Delete(folder, recursive: true); }
    }
}
