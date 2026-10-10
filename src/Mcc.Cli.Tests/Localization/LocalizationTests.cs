using System.Globalization;
using DMCBK.Core.Localization;
using Mcc.Cli.Localization;
using Xunit;

namespace Mcc.Cli.Tests.Localization;

public sealed class LocalizationTests
{
    [Fact]
    public void TranslatedHostPromptUsesSatelliteResourcesAndKeepsArguments()
    {
        using var scope = UiCulture.Enter(CultureInfo.GetCultureInfo("fr-FR"));
        Assert.Equal("Pour vous connecter, ouvrez https://example.com dans un navigateur et saisissez le code : ABC123", Strings.DeviceCode("ABC123", "https://example.com"));
        Assert.Equal("Mot de passe : ", Strings.YggdrasilPasswordPrompt);
    }

    [Fact]
    public void MissingHostTranslationFallsBackToEnglish()
    {
        using var scope = UiCulture.Enter(CultureInfo.GetCultureInfo("fr-FR"));
        Assert.Equal("Connection failed: refused", Strings.ConnectFailed("refused"));
    }

    [Fact]
    public void EnglishFormattedPromptsKeepTheirOriginalOutput()
    {
        using var scope = UiCulture.Enter(CultureInfo.GetCultureInfo("en-US"));
        Assert.Equal("To sign in, open https://example.com in a browser and enter the code: ABC123", Strings.DeviceCode("ABC123", "https://example.com"));
    }
}
