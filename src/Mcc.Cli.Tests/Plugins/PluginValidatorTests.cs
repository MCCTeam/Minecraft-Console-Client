using DMCBK.Core.Plugins;
using Mcc.Cli.Tests.Fakes;
using DMCBK.PluginSdk;
using Xunit;

namespace Mcc.Cli.Tests.Plugins;

/// <summary>
/// <c>plugins validate</c>.
/// Every finding here is a failure the running client cannot report, because a string key that resolves to nothing legitimately prints as itself: an untranslated plugin and a broken one look identical on screen.
/// </summary>
public sealed class PluginValidatorTests
{
    private const string Source = """
        using System.Threading.Tasks;
        using DMCBK.PluginSdk;
        using Tomlet.Attributes;

        public sealed class ProbeSettings
        {
            [TomlInlineComment("$settings.enabled$")]
            public bool Enabled { get; set; } = true;
        }

        public sealed class ProbePlugin : IPlugin
        {
            public void Configure(PluginDescriptor descriptor) => descriptor.Id = "probe";

            public Task ActivateAsync(PluginContext context)
            {
                context.Logger.LogInformation("{Message}", context.Strings.Get("armed"));
                return Task.CompletedTask;
            }
        }
        """;

    private const string DollarProse = """
        using System.Threading.Tasks;
        using DMCBK.PluginSdk;
        using Tomlet.Attributes;

        public sealed class ProbeSettings
        {
            [TomlInlineComment("$settings.enabled$")]
            public bool Enabled { get; set; } = true;
        }

        public static class SampleFile
        {
            public const string Content = "# regex rules substitute the captures ($1..$n) into the action";
        }

        public sealed class ProbePlugin : IPlugin
        {
            public void Configure(PluginDescriptor descriptor) => descriptor.Id = "probe";

            public Task ActivateAsync(PluginContext context)
            {
                context.Logger.LogInformation("{Message}", context.Strings.Get("armed"));
                return Task.CompletedTask;
            }
        }
        """;

    [Fact]
    public void ReportsAMessageKeyThatFellIntoATableWithItsLine()
    {
        using var fixture = new PluginRootFixture();
        string folder = fixture.WritePlugin("probe", "Probe.cs", Source);
        PluginRootFixture.WriteLang(folder, "en", """
            [settings]
            enabled = "Master switch."
            armed = "Armed."
            """);

        PluginValidationReport report = PluginValidator.Validate(folder);

        PluginValidationProblem nested = Assert.Single(
            report.Problems, p => p.Message.Contains("'armed'", StringComparison.Ordinal));
        Assert.Equal(PluginValidationSeverity.Error, nested.Severity);
        Assert.Equal(Path.Combine("lang", "en.toml"), nested.File);
        Assert.Equal(3, nested.Line);
        Assert.False(report.IsValid);
    }

    /// <summary>
    /// The shape AutoEat actually shipped: the table is held in a nullable field, read through a null test, and asked for a key that had fallen under <c>[settings]</c>.
    /// Following only assignments from <c>.Strings</c> saw no key at all here, so the plugin printed "eating" for a year and validated clean.
    /// </summary>
    [Fact]
    public void ReadingThroughANullTestOnAFieldIsStillAKeyItReads()
    {
        const string Nullable = """
            using System.Threading.Tasks;
            using DMCBK.PluginSdk;

            public sealed class ProbePlugin : IPlugin
            {
                private IPluginLocalization? _strings;

                public void Configure(PluginDescriptor descriptor) => descriptor.Id = "probe";

                public Task ActivateAsync(PluginContext context)
                {
                    _strings = context.Strings;
                    return Task.CompletedTask;
                }

                private void Announce()
                {
                    if (_strings is { } strings)
                    {
                        System.Console.WriteLine(strings.Format("armed", 1));
                    }
                }
            }
            """;

        using var fixture = new PluginRootFixture();
        string folder = fixture.WritePlugin("probe", "Probe.cs", Nullable);
        PluginRootFixture.WriteLang(folder, "en", """
            [settings]
            enabled = "Master switch."
            armed = "Armed."
            """);

        PluginValidationReport report = PluginValidator.Validate(folder);

        PluginValidationProblem nested = Assert.Single(
            report.Problems, p => p.Message.Contains("'armed'", StringComparison.Ordinal));
        Assert.Equal(PluginValidationSeverity.Error, nested.Severity);
        Assert.Equal(3, nested.Line);
    }

    [Fact]
    public void AKeyNowhereInTheFileIsOnlyAWarning()
    {
        using var fixture = new PluginRootFixture();
        string folder = fixture.WritePlugin("probe", "Probe.cs", Source);
        PluginRootFixture.WriteLang(folder, "en", """
            [settings]
            enabled = "Master switch."
            """);

        PluginValidationReport report = PluginValidator.Validate(folder);

        PluginValidationProblem missing = Assert.Single(
            report.Problems, p => p.Message.Contains("'armed'", StringComparison.Ordinal));
        Assert.Equal(PluginValidationSeverity.Warning, missing.Severity);
        Assert.True(report.IsValid);
    }

    [Fact]
    public void ReportsAPlaceholderNoLanguageFileAnswers()
    {
        using var fixture = new PluginRootFixture();
        string folder = fixture.WritePlugin("probe", "Probe.cs", Source);
        PluginRootFixture.WriteLang(folder, "en", """
            armed = "Armed."
            """);

        PluginValidationReport report = PluginValidator.Validate(folder);

        PluginValidationProblem placeholder = Assert.Single(
            report.Problems, p => p.Message.Contains("$settings.enabled$", StringComparison.Ordinal));
        Assert.Equal(PluginValidationSeverity.Error, placeholder.Severity);
    }

    /// <summary>
    /// AutoRespond ships a sample rules file whose text documents its substitutions as <c>$1..$n</c>.
    /// Read across a whole source file that is a placeholder nothing answers, and the plugin was reported broken for a string it never writes into <c>settings.toml</c>.
    /// </summary>
    [Fact]
    public void DollarTextInAnOrdinaryLiteralIsNotASettingsPlaceholder()
    {
        using var fixture = new PluginRootFixture();
        string folder = fixture.WritePlugin("probe", "Probe.cs", DollarProse);
        PluginRootFixture.WriteLang(folder, "en", """
            armed = "Armed."

            [settings]
            enabled = "Master switch."
            """);

        PluginValidationReport report = PluginValidator.Validate(folder);

        Assert.Empty(report.Problems);
    }

    /// <summary>The control: the same file's real attribute placeholder is still reported when nothing answers it.</summary>
    [Fact]
    public void APlaceholderInACommentAttributeIsStillReadBesideDollarProse()
    {
        using var fixture = new PluginRootFixture();
        string folder = fixture.WritePlugin("probe", "Probe.cs", DollarProse);
        PluginRootFixture.WriteLang(folder, "en", """
            armed = "Armed."
            """);

        PluginValidationProblem placeholder = Assert.Single(
            PluginValidator.Validate(folder).Problems,
            p => p.Message.Contains("$settings.enabled$", StringComparison.Ordinal));
        Assert.Equal(PluginValidationSeverity.Error, placeholder.Severity);
    }

    [Fact]
    public void ReportsOtherLanguagesShippedWithoutEnglish()
    {
        using var fixture = new PluginRootFixture();
        string folder = fixture.WritePlugin("probe", "Probe.cs", Source);
        PluginRootFixture.WriteLang(folder, "de", """
            armed = "Scharf."
            """);

        PluginValidationReport report = PluginValidator.Validate(folder);

        PluginValidationProblem english = Assert.Single(
            report.Problems, p => p.File == Path.Combine("lang", "en.toml"));
        Assert.Equal(PluginValidationSeverity.Error, english.Severity);
        Assert.Contains("de", english.Message);
    }

    [Fact]
    public void ReportsAManualTopicWithNoPage()
    {
        using var fixture = new PluginRootFixture();
        string folder = fixture.WritePlugin("probe", "Probe.cs", Source, extraKeys: "man = [\"probe\"]\n");
        PluginRootFixture.WriteLang(folder, "en", """
            armed = "Armed."

            [settings]
            enabled = "Master switch."
            """);

        PluginValidationReport report = PluginValidator.Validate(folder);

        PluginValidationProblem page = Assert.Single(report.Problems);
        Assert.Equal(PluginValidationSeverity.Warning, page.Severity);
        Assert.Contains("man/en/probe.md", page.Message);
    }

    [Fact]
    public void WithNoFolderItChecksEveryInstalledPlugin()
    {
        using var fixture = new PluginRootFixture();
        string good = fixture.WritePlugin("good", "Good.cs", Source);
        PluginRootFixture.WriteLang(good, "en", """
            armed = "Armed."

            [settings]
            enabled = "Master switch."
            """);
        string bad = fixture.WritePlugin("bad", "Bad.cs", Source);
        PluginRootFixture.WriteLang(bad, "en", """
            [settings]
            enabled = "Master switch."
            armed = "Armed."
            """);

        IReadOnlyList<PluginCheckResult> results = fixture.Host.Validate();

        Assert.Equal(2, results.Count);
        Assert.True(results.Single(r => r.Id == "good").IsValid);
        Assert.False(results.Single(r => r.Id == "bad").IsValid);
    }

    /// <summary>
    /// The one-shot a catalogue's CI runs per entry.
    /// It answers with an exit code, so it has to be right about which folders are sound; a check that always passed would be worse than no check.
    /// </summary>
    [Fact]
    public void TheOneShotAnswersWithAnExitCode()
    {
        using var fixture = new PluginRootFixture();
        string good = fixture.WritePlugin("good", "Good.cs", Source);
        PluginRootFixture.WriteLang(good, "en", """
            armed = "Armed."

            [settings]
            enabled = "Master switch."
            """);
        string bad = fixture.WritePlugin("bad", "Bad.cs", Source);
        PluginRootFixture.WriteLang(bad, "en", """
            [settings]
            enabled = "Master switch."
            armed = "Armed."
            """);

        Assert.True(Mcc.Cli.Plugins.PluginValidateOneShot.TryHandle(["--validate-plugin", good], out int ok));
        Assert.Equal(0, ok);

        Assert.True(Mcc.Cli.Plugins.PluginValidateOneShot.TryHandle(["--validate-plugin", bad], out int failed));
        Assert.NotEqual(0, failed);

        Assert.False(Mcc.Cli.Plugins.PluginValidateOneShot.TryHandle(["configurations"], out _));
    }
}
