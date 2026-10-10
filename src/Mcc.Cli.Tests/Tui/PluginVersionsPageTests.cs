using Mcc.Cli.Tui.Plugins;
using Xunit;

namespace Mcc.Cli.Tests.Tui;

public sealed class PluginVersionsPageTests
{
    [Theory]
    [InlineData("2.0.0", false, true)]
    [InlineData("2.0.0-beta.1", false, false)]
    [InlineData("2.0.0-beta.1", true, true)]
    [InlineData("2.0.0+build-tag", false, true)]
    public void PrereleaseControlDoesNotHideStableBuildMetadata(string version, bool prerelease, bool expected)
        => Assert.Equal(expected, PluginVersionsPage.AllowsVersion(version, prerelease));
}
