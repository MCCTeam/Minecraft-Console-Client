using Mcc.Cli.Presentation;
using Mcc.Cli;
using DMCBK.Core.Localization;
using Umpk.Data.Lang;
using Umpk.Text;
using Xunit;

namespace Mcc.Cli.Tests.Presentation;

/// <summary>
/// H6 guard: <see cref="HostTranslations"/> layers host overrides over UMPK's own per-protocol vanilla <c>en_us</c> tables (<see cref="VanillaTranslations"/>; the core owns no locale data of its own any more).
/// These tests prove a known key resolves with positional (<c>%s</c>) and indexed (<c>%1$s</c>) argument substitution through component rendering, that a missing key falls back to the key text, and that host overrides win.
/// </summary>
public sealed class HostTranslationsTests
{
    private static string Render(ITranslationSource translations, string key, params string[] args)
        => Component.Translatable(key, Array.ConvertAll(args, static a => Component.Text(a)))
            .ToPlainText(translations);

    [Fact]
    public void Render_ResolvesKnownKey_WithSingleArgument()
    {
        var translations = new HostTranslations();
        Component component = Component.Translatable("multiplayer.player.joined", Component.Text("Steve"));

        Assert.Equal("Steve joined the game", component.ToPlainText(translations));
    }

    [Fact]
    public void Render_ResolvesKnownKey_WithMultiplePositionalArguments()
    {
        var translations = new HostTranslations();
        Component component = Component.Translatable(
            "chat.type.text", Component.Text("Steve"), Component.Text("hello"));

        Assert.Equal("<Steve> hello", component.ToPlainText(translations));
    }

    [Fact]
    public void Render_ResolvesKnownKey_WithIndexedArgument()
    {
        var translations = new HostTranslations();
        // death.attack.explosion = "%1$s blew up"
        Component component = Component.Translatable("death.attack.explosion", Component.Text("Steve"));

        Assert.Equal("Steve blew up", component.ToPlainText(translations));
    }

    [Fact]
    public void TryResolve_ReturnsTrue_ForKnownKey()
    {
        var translations = new HostTranslations();

        Assert.True(translations.TryResolve("multiplayer.player.joined", out string? template));
        Assert.Equal("%s joined the game", template);
    }

    [Fact]
    public void Render_FallsBackToKey_ForMissingKey()
    {
        var translations = new HostTranslations();
        const string missing = "mcc.tests.no.such.key";

        Assert.False(translations.TryResolve(missing, out _));
        Assert.Equal(missing, Render(translations, missing));
    }

    [Fact]
    public void Overrides_WinOverBaseTable()
    {
        var overrides = new Dictionary<string, string> { ["multiplayer.player.joined"] = "%s appeared" };
        var translations = new HostTranslations(overrides);

        Assert.Equal(
            "Steve appeared",
            Component.Translatable("multiplayer.player.joined", Component.Text("Steve")).ToPlainText(translations));
    }
}

/// <summary>
/// Guard for the pre-flattening (protocols 47..340) vanilla translation tables, now <see cref="VanillaTranslations"/>'s own per-protocol tables layered in by <see cref="HostTranslations.UseProtocol"/> rather than MCC's own compressed base-plus-overlay scheme.
/// </summary>
/// <remarks>
/// <para>
/// Two symptoms, one cause.
/// Vanilla 1.8.4 <c>afg.java</c> (BlockBed.onBlockActivated) and 1.12.1 <c>aou.java</c> refuse a daytime sleep with the translatable key <c>tile.bed.noSleep</c>, whose modern spelling is <c>block.minecraft.bed.no_sleep</c>; a modern-only table resolves nothing and the console showed the bare key.
/// Separately, 1.8.4 <c>ah.java</c> (CommandGameMode.a) sends <c>gameMode.changed</c> with ZERO arguments against 1.8's zero-placeholder template, while every table from 1.9 on (and the modern one) reads "Your game mode has been updated to %s", so the modern template rendered a dangling "to " with nothing after it on protocol 47 and only on protocol 47.
/// </para>
/// <para>
/// Every band assertion below is pinned to a key whose vanilla text genuinely differs from the neighbouring band's, so a wrong protocol-to-band mapping cannot pass: 1.8 by the zero-argument <c>commands.gamerule.success</c>, 1.9 by "the stat" in <c>commands.achievement.give.success.one</c>, 1.10 by the zero-argument <c>commands.generic.player.notFound</c>, 1.11 by <c>achievement.bakeCake</c> (dropped in 1.12, when advancements replaced achievements) and 1.12 by its absence.
/// </para>
/// </remarks>
public sealed class HostTranslationsProtocolTests
{
    private const int Protocol18 = 47;       // 1.8 - 1.8.9
    private const int Protocol19 = 110;      // 1.9.3 / 1.9.4
    private const int Protocol110 = 210;     // 1.10 - 1.10.2
    private const int Protocol111 = 316;     // 1.11.1 / 1.11.2
    private const int Protocol112 = 340;     // 1.12.2
    private const int ProtocolModern = 765;  // 1.20.4

    private static string Render(HostTranslations translations, string key, params string[] args)
        => Component.Translatable(
            key, Array.ConvertAll(args, static a => Component.Text(a))).ToPlainText(translations);

    #region The legacy key namespace the modern table cannot know

    [Fact]
    public void ModernTable_CannotResolveTheLegacyBedKeys_WhichIsTheDefect()
    {
        var translations = new HostTranslations();

        // Pinned behavior: no legacy key resolves, so component flattening falls back to the raw key and that is what reached the console.
        Assert.False(translations.TryResolve("tile.bed.noSleep", out _));
        Assert.Equal("tile.bed.noSleep", Render(translations, "tile.bed.noSleep"));

        // The same string does exist under its modern spelling, which is why a modern table looks complete.
        Assert.True(translations.TryResolve("block.minecraft.bed.no_sleep", out string? modern));
        Assert.Equal("You can sleep only at night or during thunderstorms", modern);
    }

    [Theory]
    [InlineData(Protocol18)]
    [InlineData(Protocol19)]
    [InlineData(Protocol110)]
    [InlineData(Protocol111)]
    [InlineData(Protocol112)]
    public void LegacyProtocols_RenderTheBedRefusals_AsText(int protocol)
    {
        var translations = new HostTranslations();
        translations.UseProtocol(protocol);

        // 1.12.1 aou.java:60-64 sends these the same way BlockBed does in 1.8.4 afg.java.
        Assert.Equal("You can only sleep at night", Render(translations, "tile.bed.noSleep"));
        Assert.Equal(
            "You may not rest now, there are monsters nearby", Render(translations, "tile.bed.notSafe"));
        Assert.Equal("This bed is occupied", Render(translations, "tile.bed.occupied"));
    }

    [Fact]
    public void LegacyProtocols_ResolveTheWiderTileAndEntityFamilies()
    {
        var translations = new HostTranslations();
        translations.UseProtocol(Protocol112);

        Assert.Equal("Stone Bricks", Render(translations, "tile.stonebricksmooth.name"));
        Assert.Equal("Diamond Sword", Render(translations, "item.swordDiamond.name"));
        Assert.Equal("Zombie Pigman", Render(translations, "entity.PigZombie.name"));
        Assert.Equal("Creeper", Render(translations, "entity.Creeper.name"));
        Assert.Equal("Sharpness", Render(translations, "enchantment.damage.all"));
    }

    #endregion
    #region The arity disagreement, which a purely additive fallback could not have fixed

    [Fact]
    public void Protocol47_GameModeChanged_RendersTheCompleteSentence_WithNoDanglingArgument()
    {
        var translations = new HostTranslations();
        translations.UseProtocol(Protocol18);

        // 1.8.4 ah.java:30 -> new fb("gameMode.changed") with no arguments at all.
        string rendered = Render(translations, "gameMode.changed");

        // Exact equality against a non-empty sentence; an empty or truncated render cannot satisfy it.
        Assert.Equal("Your game mode has been updated", rendered);
        Assert.False(rendered.EndsWith(' '), "the 1.8 message must not end in whitespace");
        Assert.DoesNotContain(" to ", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void Protocol47_GameModeChanged_AgainstTheModernTable_IsTheReportedSymptom()
    {
        var translations = new HostTranslations();

        // No UseProtocol: the modern one-argument template plus the server's zero arguments is exactly the "Your game mode has been updated to " with nothing after it.
        Assert.Equal("Your game mode has been updated to ", Render(translations, "gameMode.changed"));
    }

    [Theory]
    [InlineData(Protocol19)]
    [InlineData(Protocol110)]
    [InlineData(Protocol111)]
    [InlineData(Protocol112)]
    public void Protocols110AndUp_GameModeChanged_StillCarriesTheModeName(int protocol)
    {
        var translations = new HostTranslations();
        translations.UseProtocol(protocol);

        // 1.12.1 ck.java:32 -> new hp("gameMode.changed", <the gameMode.creative component>).
        Component component = Component.Translatable(
            "gameMode.changed", Component.Translatable("gameMode.creative"));

        Assert.Equal("Your game mode has been updated to Creative Mode", component.ToPlainText(translations));
    }

    [Fact]
    public void LegacyProtocols_UseTheLegacyTemplate_WhenBothErasKnowTheKey()
    {
        // commands.setblock.success is "Block placed" (no arguments) on the whole legacy band and "Changed the block at %s, %s, %s" in the modern table, so the modern table rendered a legacy server's /setblock feedback as "Changed the block at , , ".
        var modern = new HostTranslations();
        Assert.Equal("Changed the block at , , ", Render(modern, "commands.setblock.success"));

        var translations = new HostTranslations();
        translations.UseProtocol(Protocol112);
        Assert.Equal("Block placed", Render(translations, "commands.setblock.success"));
    }

    #endregion
    #region band selection, each arm pinned to a key that actually differs between the bands

    [Fact]
    public void Protocol47_SelectsThe18Band()
    {
        var translations = new HostTranslations();
        translations.UseProtocol(Protocol18);

        // 1.8: no arguments.
        // 1.9 onwards: "Game rule %s has been updated to %s".
        Assert.Equal("Game rule has been updated", Render(translations, "commands.gamerule.success"));
    }

    [Theory]
    [InlineData(107)]
    [InlineData(108)]
    [InlineData(109)]
    [InlineData(110)]
    public void Protocols107To110_SelectThe19Band(int protocol)
    {
        // UMPK ships one full table per protocol rather than a shared band overlay, but 107/108/109 are identical content (2855 keys each) and 110 only adds 5 keys on top (2860); none of the extra keys are the ones this test touches, so all four protocols still agree on both assertions below.
        var translations = new HostTranslations();
        translations.UseProtocol(protocol);

        Assert.Equal(
            "Game rule doFireTick has been updated to false",
            Render(translations, "commands.gamerule.success", "doFireTick", "false"));

        // 1.9 says "the stat"; 1.10 changed it to "the achievement".
        Assert.Equal(
            "Successfully given Steve the stat Taking Inventory",
            Render(translations, "commands.achievement.give.success.one", "Steve", "Taking Inventory"));
    }

    [Fact]
    public void Protocol210_SelectsThe110Band()
    {
        var translations = new HostTranslations();
        translations.UseProtocol(Protocol110);

        Assert.Equal(
            "Successfully given Steve the achievement Taking Inventory",
            Render(translations, "commands.achievement.give.success.one", "Steve", "Taking Inventory"));

        // 1.10: no arguments.
        // 1.11 changed it to "Player '%s' cannot be found".
        Assert.Equal("That player cannot be found", Render(translations, "commands.generic.player.notFound"));
    }

    [Theory]
    [InlineData(315)]
    [InlineData(316)]
    public void Protocols315And316_SelectThe111Band(int protocol)
    {
        var translations = new HostTranslations();
        translations.UseProtocol(protocol);

        Assert.Equal(
            "Player 'Steve' cannot be found",
            Render(translations, "commands.generic.player.notFound", "Steve"));

        // achievement.* survives through 1.11 and is gone in 1.12, where advancements replaced it.
        Assert.Equal("The Lie", Render(translations, "achievement.bakeCake"));
    }

    [Theory]
    [InlineData(335)]
    [InlineData(338)]
    [InlineData(340)]
    public void Protocols335To340_SelectThe112Band(int protocol)
    {
        var translations = new HostTranslations();
        translations.UseProtocol(protocol);

        // 1.12 dropped the achievement family, so this key resolves nowhere and falls back to itself.
        Assert.False(translations.TryResolve("achievement.bakeCake", out _));

        // ... but the 1.12-only bed key that 1.8 never had does resolve.
        Assert.Equal(
            "You may not rest now, the bed is too far away", Render(translations, "tile.bed.tooFarAway"));
    }

    [Fact]
    public void Protocol47_UsesItsOwn1_8_9Text_ForArityAndWording()
    {
        // The contract that matters: wherever 1.8's own vanilla table defines a key, the 1.8 band renders 1.8's text, never a newer band's.
        // These are spot checks in the families a server actually sends.
        var translations = new HostTranslations();
        translations.UseProtocol(Protocol18);

        // 1.12 replaced tile.bed.name with per-color keys, so only 1.8's own table can answer this.
        Assert.Equal("Bed", Render(translations, "tile.bed.name"));

        // Arity, the case an additive fallback cannot fix: both are zero-argument in 1.8 and take arguments from 1.9 (gameMode.changed) and 1.9 (commands.gamerule.success) onwards.
        Assert.Equal("Your game mode has been updated", Render(translations, "gameMode.changed"));
        Assert.Equal("Game rule has been updated", Render(translations, "commands.gamerule.success"));

        // Text-only drift inside the legacy era: 1.8 says "Build", 1.9 onwards say "Eat".
        Assert.Equal("Build a Notch apple", Render(translations, "achievement.overpowered.desc"));

        var later = new HostTranslations();
        later.UseProtocol(Protocol19);
        Assert.Equal("Eat a Notch apple", Render(later, "achievement.overpowered.desc"));
    }

    [Fact]
    public void Protocol47_ResolvesOnly1_8_9sOwnNamespace()
    {
        // tile.bed.tooFarAway was added in 1.12. The old MCC-owned table compressed the legacy bands as a 1.12.2 base plus per-band overlays, so a key a band never shipped fell through to the shared 1.12.2 base rather than printing raw - an artifact of that compression, unreachable from a real vanilla 1.8.9 server (it has no such key to send).
        // UMPK ships one full, independent vanilla table per protocol instead, straight from the dataset, so 1.8.9's own table simply does not have this key: NOT resolving it is what vanilla 1.8.9 itself does.
        var translations = new HostTranslations();
        translations.UseProtocol(Protocol18);

        Assert.False(translations.TryResolve("tile.bed.tooFarAway", out _));
        Assert.Equal("tile.bed.tooFarAway", Render(translations, "tile.bed.tooFarAway"));
    }

    #endregion
    #region isolation: the legacy layers must not leak into a modern session

    [Fact]
    public void ModernProtocol_KeepsTheModernTableAlone()
    {
        var translations = new HostTranslations();
        translations.UseProtocol(ProtocolModern);

        Assert.False(translations.TryResolve("tile.bed.noSleep", out _));
        Assert.Equal(
            "Changed the block at 1, 2, 3", Render(translations, "commands.setblock.success", "1", "2", "3"));
    }

    [Fact]
    public void UseProtocol_Modern_ClearsLayersAPreviousLegacySessionInstalled()
    {
        // A single HostTranslations outlives a reconnect to a different server, so the switch has to work in both directions, not just legacy-ward.
        var translations = new HostTranslations();

        translations.UseProtocol(Protocol18);
        Assert.Equal("You can only sleep at night", Render(translations, "tile.bed.noSleep"));
        Assert.Equal("Your game mode has been updated", Render(translations, "gameMode.changed"));

        translations.UseProtocol(ProtocolModern);
        Assert.Equal("tile.bed.noSleep", Render(translations, "tile.bed.noSleep"));
        Assert.Equal("Your game mode has been updated to ", Render(translations, "gameMode.changed"));

        translations.UseProtocol(Protocol18);
        Assert.Equal("You can only sleep at night", Render(translations, "tile.bed.noSleep"));
    }

    [Fact]
    public void Overrides_WinOverTheLegacyLayers()
    {
        var overrides = new Dictionary<string, string> { ["tile.bed.noSleep"] = "nope, it is daytime" };
        var translations = new HostTranslations(overrides);
        translations.UseProtocol(Protocol18);

        Assert.Equal("nope, it is daytime", Render(translations, "tile.bed.noSleep"));
    }

    [Fact]
    public void LegacyProtocol_StillFallsThroughToTheModernTable_ForKeysOnlyItKnows()
    {
        var translations = new HostTranslations();
        translations.UseProtocol(Protocol18);

        // Nothing in 1.8.9's own table defines this, and a modded or plugin server may still send it; the last layer is always VanillaTranslations.Latest, so it still resolves rather than printing raw.
        Assert.Equal(
            "You can sleep only at night or during thunderstorms",
            Render(translations, "block.minecraft.bed.no_sleep"));
    }

    [Fact]
    public void Count_ReportsTheEraTableSize()
    {
        // HostTranslations exposes no aggregate Count: unlike the old MCC-owned base-plus-overlay scheme (where installing the legacy layers could only ever ADD keys on top of the modern table), each UseProtocol layer is UMPK's own complete, independent table, so this asserts directly against Umpk.Data.Lang.VanillaTranslations, which is what HostTranslations.UseProtocol layers in.
        // 1.8.9's table is roughly a third the size of the newest table's, the opposite direction from the old scheme.
        int legacyCount = VanillaTranslations.CountFor(Protocol18);
        int modernCount = VanillaTranslations.CountFor(VanillaTranslations.Protocols[^1]);

        Assert.True(
            legacyCount < modernCount,
            $"expected 1.8.9's table ({legacyCount} keys) to be smaller than the newest table ({modernCount} keys)");
    }

    #endregion
    #region the effect, not the variable: the exact text the console host emits

    [Fact]
    public void TheConsoleRenderer_EmitsTheResolvedText_NotTheRawKey()
    {
        // AnsiComponentRenderer is the classic host's Component -> console path, so this is the string a user actually sees, not just what TryResolve returns.
        // Colors off so the assertion is the text.
        var translations = new HostTranslations();
        var renderer = new AnsiComponentRenderer(translations, depth: ConsoleColorDepth.Disable);
        Component noSleep = Component.Translatable("tile.bed.noSleep");

        // Before the era is known, exactly the reported symptom.
        Assert.Equal("tile.bed.noSleep", renderer.Render(noSleep));

        translations.UseProtocol(Protocol18);
        Assert.Equal("You can only sleep at night", renderer.Render(noSleep));

        Component gameMode = Component.Translatable("gameMode.changed");
        Assert.Equal("Your game mode has been updated", renderer.Render(gameMode));
    }

    [Fact]
    public void ProtocolVersion_ReportsTheEraInUse()
    {
        var translations = new HostTranslations();
        Assert.Null(translations.ProtocolVersion);

        translations.UseProtocol(Protocol18);
        Assert.Equal(Protocol18, translations.ProtocolVersion);

        translations.UseProtocol(ProtocolModern);
        Assert.Equal(ProtocolModern, translations.ProtocolVersion);
    }
    #endregion
}
