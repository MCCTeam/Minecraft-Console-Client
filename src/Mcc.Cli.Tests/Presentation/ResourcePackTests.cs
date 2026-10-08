using DMCBK.Core;
using Mcc.Cli.Hosting.Classic;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Mcc.Cli;
using DMCBK.Core.Configuration;
using DMCBK.Core.Configuration.Toml;
using DMCBK.Core.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Umpk.Client;
using Xunit;

namespace Mcc.Cli.Tests.Presentation;

/// <summary>
/// Resource-pack policy, translation layering and pack extraction: the restored 1.x behavior where an accepted pack reports SuccessfullyLoaded on the wire and layers its language entries into chat until a pop clears it.
/// </summary>
public sealed class ResourcePackTests
{
    private static ConfigurationValidationResult Validate(ClientTomlFile? client = null)
        => ConfigurationValidation.Validate(client ?? new(), new(), new(), sourceFolder: null);

    [Fact]
    public void Policy_DefaultsToAccept()
    {
        Assert.Equal(ResourcePackPolicyMode.Accept, Validate().Config.Localization.ResourcePackPolicy);
    }

    [Theory]
    [InlineData("decline", ResourcePackPolicyMode.Decline)]
    [InlineData("DECLINE", ResourcePackPolicyMode.Decline)]
    [InlineData("prompt", ResourcePackPolicyMode.Prompt)]
    [InlineData("Prompt", ResourcePackPolicyMode.Prompt)]
    [InlineData("accept", ResourcePackPolicyMode.Accept)]
    public void Policy_ParsesCaseInsensitively(string input, ResourcePackPolicyMode expected)
    {
        var client = new ClientTomlFile();
        client.Localization.ResourcePackPolicy = input;
        ConfigurationValidationResult result = Validate(client);
        Assert.Equal(expected, result.Config.Localization.ResourcePackPolicy);
        Assert.DoesNotContain(result.Warnings, w => w.Message.Contains("ResourcePackPolicy"));
    }

    [Fact]
    public void Policy_UnknownValue_WarnsAndFallsBackToAccept()
    {
        var client = new ClientTomlFile();
        client.Localization.ResourcePackPolicy = "yes-please";
        ConfigurationValidationResult result = Validate(client);
        Assert.Equal(ResourcePackPolicyMode.Accept, result.Config.Localization.ResourcePackPolicy);
        Assert.Contains(result.Warnings, w => w.Message.Contains("ResourcePackPolicy"));
    }

    [Fact]
    public void PackIdentifier_PrefersUuidThenHashThenUrl()
    {
        Guid id = Guid.NewGuid();
        Assert.Equal(id.ToString("D"), ResourcePackTranslationLoader.PackIdentifier(id, "http://x/p.zip", new string('a', 40)));
        Assert.Equal(new string('A', 40).ToLowerInvariant(),
            ResourcePackTranslationLoader.PackIdentifier(Guid.Empty, "http://x/p.zip", new string('A', 40)));
        Assert.Equal("http://x/p.zip",
            ResourcePackTranslationLoader.PackIdentifier(Guid.Empty, "http://x/p.zip", string.Empty));
    }

    [Theory]
    [InlineData("http://example.com/p.zip", true)]
    [InlineData("https://example.com/p.zip", true)]
    [InlineData("ftp://example.com/p.zip", false)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    public void IsDownloadableUrl_OnlyHttpAndHttps(string url, bool expected)
        => Assert.Equal(expected, ResourcePackTranslationLoader.IsDownloadableUrl(url));

    [Theory]
    [InlineData("auto", "en_us")]
    [InlineData("", "en_us")]
    [InlineData("en", "en_us")]
    [InlineData("en_us", "en_us")]
    [InlineData("pt-BR", "pt_br")]
    [InlineData("DE_de", "de_de")]
    public void NormalizeLanguage_MatchesLegacyExpectations(string input, string expected)
        => Assert.Equal(expected, ResourcePackTranslationLoader.NormalizeLanguage(input));

    [Theory]
    [InlineData("assets/minecraft/lang/en_us.json", "en_us")]
    [InlineData("assets/mymod/lang/de_de.json", "de_de")]
    [InlineData("assets/minecraft/lang/en_us.lang", null)]
    [InlineData("assets/minecraft/textures/x.png", null)]
    [InlineData("lang/en_us.json", null)]
    public void TryGetEntryLanguage_OnlyLangJsonUnderAssets(string path, string? expected)
    {
        bool ok = ResourcePackTranslationLoader.TryGetEntryLanguage(path, out string? language);
        Assert.Equal(expected is not null, ok);
        Assert.Equal(expected, language);
    }

    [Fact]
    public void ExtractTranslations_LayersEnUsWithSelectedLanguageOnTop()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "assets/minecraft/lang/en_us.json",
                """{"greeting":"Hello %s","only.english":"English"}""");
            WriteEntry(archive, "assets/minecraft/lang/de_de.json",
                """{"greeting":"Hallo %s"}""");
            WriteEntry(archive, "assets/minecraft/textures/ignore.png", "nope");
        }

        stream.Position = 0;
        Dictionary<string, string> entries = ResourcePackTranslationLoader.ExtractTranslations(stream, "de_de");
        Assert.Equal("Hallo %s", entries["greeting"]);
        Assert.Equal("English", entries["only.english"]);
    }

    [Fact]
    public void HostTranslations_PackLayerLosesToOverrides()
    {
        var translations = new HostTranslations(new Dictionary<string, string> { ["k"] = "override" });
        translations.SetPackTranslations("pack", new Dictionary<string, string> { ["k"] = "pack" });
        Assert.True(translations.TryResolve("k", out string? template));
        Assert.Equal("override", template);
    }

    [Fact]
    public void HostTranslations_PackLayerBeatsVanillaAndStacksLastWins()
    {
        var translations = new HostTranslations();
        translations.SetPackTranslations("first", new Dictionary<string, string> { ["k"] = "first" });
        translations.SetPackTranslations("second", new Dictionary<string, string> { ["k"] = "second" });
        Assert.True(translations.TryResolve("k", out string? template));
        Assert.Equal("second", template);

        translations.RemovePackTranslations("second");
        Assert.True(translations.TryResolve("k", out template));
        Assert.Equal("first", template);

        translations.ClearPackTranslations();
        Assert.False(translations.TryResolve("k", out _));
    }

    [Fact]
    public void HostTranslations_UseProtocolStillWorksWithPackLayerPresent()
    {
        var translations = new HostTranslations();
        translations.SetPackTranslations("pack", new Dictionary<string, string> { ["custom.key"] = "pack value" });
        translations.UseProtocol(767);
        Assert.True(translations.TryResolve("custom.key", out string? template));
        Assert.Equal("pack value", template);
        // A vanilla key still resolves through the protocol layer underneath the pack layer.
        Assert.True(translations.TryResolve("chat.type.text", out _));
    }

    [Fact]
    public async Task LoadAsync_CacheHit_LayersWithoutNetwork()
    {
        string cacheRoot = Path.Combine(Path.GetTempPath(), $"mcc-pack-test-{Guid.NewGuid():N}");
        try
        {
            var translations = new HostTranslations();
            var loader = new ResourcePackTranslationLoader(translations, cacheRoot, "en_us", NullLogger.Instance);
            string url = "http://127.0.0.1:1/pack.zip";
            string cachePath = loader.CacheFilePath(new Uri(url), string.Empty);
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
            var cached = new
            {
                CacheVersion = ResourcePackTranslationLoader.CacheVersion,
                Language = "en_us",
                SourceUrl = new Uri(url).AbsoluteUri,
                SourceHash = string.Empty,
                Translations = new Dictionary<string, string> { ["cached.key"] = "cached value" },
            };
            await File.WriteAllTextAsync(cachePath, JsonSerializer.Serialize(cached), Encoding.UTF8);

            await loader.LoadAsync(Guid.NewGuid(), url, string.Empty, CancellationToken.None);

            Assert.True(translations.TryResolve("cached.key", out string? template));
            Assert.Equal("cached value", template);
        }
        finally
        {
            try
            {
                Directory.Delete(cacheRoot, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    [Fact]
    public void Policy_DeclineMode_AnswersDeclined()
    {
        DmcbkConfiguration config = Validate(TomlWithPolicy("decline")).Config;
        ResourcePackPolicy policy = ResourcePackPolicyFactory.Build(config, new NullHostInterface(), new HostTranslations());
        Assert.Equal(ResourcePackResponse.Declined,
            policy.Decide(new ResourcePackRequest(Guid.NewGuid(), "http://127.0.0.1:1/p.zip", string.Empty, Required: false)));
    }

    [Fact]
    public void Policy_AcceptMode_AnswersSuccessfullyLoaded()
    {
        DmcbkConfiguration config = Validate(TomlWithPolicy("accept")).Config;
        ResourcePackPolicy policy = ResourcePackPolicyFactory.Build(config, new NullHostInterface(), new HostTranslations());
        Assert.Equal(ResourcePackResponse.SuccessfullyLoaded,
            policy.Decide(new ResourcePackRequest(Guid.NewGuid(), "http://127.0.0.1:1/p.zip", string.Empty, Required: false)));
    }

    [Fact]
    public void Policy_InvalidUrl_AnswersInvalidUrl()
    {
        DmcbkConfiguration config = Validate().Config;
        ResourcePackPolicy policy = ResourcePackPolicyFactory.Build(config, new NullHostInterface(), new HostTranslations());
        Assert.Equal(ResourcePackResponse.InvalidUrl,
            policy.Decide(new ResourcePackRequest(Guid.NewGuid(), "ftp://example.com/p.zip", string.Empty, Required: false)));
    }

    [Fact]
    public void Policy_PromptMode_AsksHost()
    {
        var client = new ClientTomlFile();
        client.Localization.ResourcePackPolicy = "prompt";
        DmcbkConfiguration config = Validate(client).Config;

        ResourcePackPolicy yes = ResourcePackPolicyFactory.Build(config, new StubHost(true), new HostTranslations());
        Assert.Equal(ResourcePackResponse.SuccessfullyLoaded,
            yes.Decide(new ResourcePackRequest(Guid.NewGuid(), "http://127.0.0.1:1/p.zip", string.Empty, Required: false)));

        ResourcePackPolicy no = ResourcePackPolicyFactory.Build(config, new StubHost(false), new HostTranslations());
        Assert.Equal(ResourcePackResponse.Declined,
            no.Decide(new ResourcePackRequest(Guid.NewGuid(), "http://127.0.0.1:1/p.zip", string.Empty, Required: false)));

        ResourcePackPolicy headless = ResourcePackPolicyFactory.Build(config, new NullHostInterface(), new HostTranslations());
        Assert.Equal(ResourcePackResponse.Declined,
            headless.Decide(new ResourcePackRequest(Guid.NewGuid(), "http://127.0.0.1:1/p.zip", string.Empty, Required: false)));
    }

    [Fact]
    public void ClearForPop_RemovesOnePackOrAll()
    {
        var translations = new HostTranslations();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        translations.SetPackTranslations(first.ToString("D"), new Dictionary<string, string> { ["a"] = "1" });
        translations.SetPackTranslations(second.ToString("D"), new Dictionary<string, string> { ["b"] = "2" });

        Assert.True(ResourcePackPolicyFactory.ClearForPop(
            new Umpk.Protocol.Java.Packets.ClientboundResourcePackPopPacket(first), translations));
        Assert.False(translations.TryResolve("a", out _));
        Assert.True(translations.TryResolve("b", out _));

        Assert.True(ResourcePackPolicyFactory.ClearForPop(
            new Umpk.Protocol.Java.Packets.ClientboundConfigResourcePackPopPacket(null), translations));
        Assert.False(translations.TryResolve("b", out _));

        Assert.False(ResourcePackPolicyFactory.ClearForPop(new object(), translations));
    }

    [Theory]
    [InlineData("y", true)]
    [InlineData("YES", true)]
    [InlineData("  yes  ", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("no", false)]
    [InlineData("oui", false)]
    public void ConsolePrompt_OnlyYesAccepts(string? line, bool expected)
        => Assert.Equal(expected, ConsoleResourcePackPrompt.IsYes(line));

    private static ClientTomlFile TomlWithPolicy(string policy)
    {
        var client = new ClientTomlFile();
        client.Localization.ResourcePackPolicy = policy;
        return client;
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name);
        using StreamWriter writer = new(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private sealed class StubHost(bool answer) : IHostInterface
    {
        public IUserPrompt? Prompt => null;

        public Umpk.Auth.IAuthInteraction? AuthInteraction => null;

        public IResourcePackPrompt? ResourcePackPrompt => new StubPrompt(answer);

        private sealed class StubPrompt(bool answer) : IResourcePackPrompt
        {
            public ValueTask<bool> PromptAsync(ResourcePackPromptRequest request, CancellationToken ct)
                => ValueTask.FromResult(answer);
        }
    }
}
