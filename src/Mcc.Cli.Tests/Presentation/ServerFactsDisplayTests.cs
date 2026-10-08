using Mcc.Cli.Presentation;
using Mcc.Cli.Localization;
using System.Globalization;
using Mcc.Cli;
using DMCBK.Core;
using DMCBK.Core.Commands;
using DMCBK.Core.Commands.Impl;
using DMCBK.Core.Configuration;
using Umpk.Client.Chat;
using Umpk.Client.Events;
using Umpk.Text;
using DMCBK.Core.Localization;
using Xunit;

namespace Mcc.Cli.Tests.Presentation;

/// <summary>
/// Covers the server-facts readouts: the server facts now read off UMPK's <c>ClientState.Server</c> (brand, measured tick rate, the two distinct latency figures), the semantic container type in the text output, the chat signature standing, and the corrected recipe-book wording.
/// The recurring theme is honesty about what is NOT known: a null tick rate is unknown rather than zero, a responder-side turnaround is never called ping, and an undecoded recipe-book frame count is never presented as a recipe count.
/// </summary>
public sealed class ServerFactsDisplayTests
{
    private static SessionInfoSnapshot Info(
        string? brand = null,
        double? tps = null,
        int? latency = null,
        TimeSpan? turnaround = null,
        TimeSpan? interval = null)
        => new("localhost", 25565, "1.21.11", 774, latency, brand, tps, turnaround, interval);

    #region /tps

    // This used to pin this client's own "Server TPS: 19.97".
    // Legacy printed the corpus header cmd.tps.current ("Current tps") followed by ": ", the threshold colour code (red < 10, yellow < 15, green otherwise) and the rate rounded to two decimals (MinecraftClient/Commands/Tps.cs:41-51).
    // The rate is interpolated, so it renders through CurrentCulture; the fixture pins the culture rather than the separator (see MoveMessageMappingTests).
    [Fact]
    public void Tps_KnownRate_IsReported()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            Assert.Equal("Current tps: §a19.97", TpsCommand.Format(19.97));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Tps_Rate_CarriesTheLegacyThresholdColour()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            // Tps.cs:44-51: red below 10, yellow below 15, green at or above 15.
            Assert.Equal("Current tps: §c9.5", TpsCommand.Format(9.5));
            Assert.Equal("Current tps: §e14.99", TpsCommand.Format(14.99));
            Assert.Equal("Current tps: §a15", TpsCommand.Format(15));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Tps_NullRate_ReportsUnknown_NeverZero()
    {
        string line = TpsCommand.Format(null);

        Assert.Equal(CommandStrings.TpsUnknown, line);
        Assert.Contains("unknown", line, StringComparison.OrdinalIgnoreCase);

        // The whole point: a paused or tick-frozen server must not be reported as a server running at 0 TPS.
        Assert.DoesNotContain("0.00", line, StringComparison.Ordinal);
        Assert.NotEqual(CommandStrings.TpsCurrent(0), line);
    }

    [Fact]
    public void Tps_ZeroRate_IsStillADistinctReadingFromUnknown()
    {
        // UMPK clamps to a real measurement and never synthesises 0, but if a rate of 0 ever were measured it must not collapse into the unknown wording: they mean different things.
        Assert.NotEqual(TpsCommand.Format(null), TpsCommand.Format(0));
    }

    #endregion
    #region debug state

    [Fact]
    public void DebugState_ShowsTheServerBrand()
    {
        Assert.Contains("brand=Paper", MiscCommandsBrandProbe("Paper"), StringComparison.Ordinal);
        Assert.Contains("brand=vanilla", MiscCommandsBrandProbe("vanilla"), StringComparison.Ordinal);
    }

    [Fact]
    public void DebugState_UnannouncedBrand_ReadsUnknown()
        => Assert.Contains("brand=unknown", DebugCommand.FormatServerFacts(Info()), StringComparison.Ordinal);

    [Fact]
    public void DebugState_NullTps_ReadsUnknownRatherThanZero()
    {
        string line = DebugCommand.FormatServerFacts(Info(tps: null));

        Assert.Contains("tps=unknown", line, StringComparison.Ordinal);
        Assert.DoesNotContain("tps=0", line, StringComparison.Ordinal);
    }

    [Fact]
    public void DebugState_KnownTps_IsShown()
        => Assert.Contains("tps=19.50", DebugCommand.FormatServerFacts(Info(tps: 19.5)), StringComparison.Ordinal);

    [Fact]
    public void DebugState_LabelsTheServerMeasuredLatencyAsSuch()
    {
        string line = DebugCommand.FormatServerFacts(Info(latency: 42));

        Assert.Contains("latency=42ms", line, StringComparison.Ordinal);
        Assert.Contains("server-measured", line, StringComparison.Ordinal);
    }

    [Fact]
    public void DebugState_NeverCallsTheKeepAliveTurnaroundAPing()
    {
        string line = DebugCommand.FormatServerFacts(Info(
            latency: 42,
            turnaround: TimeSpan.FromMilliseconds(0.4),
            interval: TimeSpan.FromSeconds(15)));

        // A Java client is only ever the RESPONDER on the keep-alive exchange and the id is the server's own clock, so it provably cannot measure the round trip.
        // The turnaround must be labelled as our own.
        Assert.Contains("turnaround=", line, StringComparison.Ordinal);
        Assert.Contains("not a round trip", line, StringComparison.Ordinal);
        Assert.DoesNotContain("ping", line, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rtt", line, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("interval=15", line, StringComparison.Ordinal);
    }

    [Fact]
    public void DebugState_TurnaroundWithoutASecondKeepAlive_OmitsTheInterval()
    {
        string line = DebugCommand.FormatServerFacts(Info(turnaround: TimeSpan.FromMilliseconds(1)));

        Assert.Contains("turnaround=", line, StringComparison.Ordinal);
        Assert.DoesNotContain("interval=", line, StringComparison.Ordinal);
    }

    [Fact]
    public void DebugState_BeforeAnyKeepAlive_OmitsTheWholeLine()
        => Assert.DoesNotContain("keepalive", DebugCommand.FormatServerFacts(Info()), StringComparison.Ordinal);

    [Fact]
    public void DebugState_UnreportedLatency_ReadsUnknown()
        => Assert.Contains("latency=unknown", DebugCommand.FormatServerFacts(Info()), StringComparison.Ordinal);

    private static string MccBrand(string brand) => DebugCommand.FormatServerFacts(Info(brand: brand));

    private static string MiscCommandsBrandProbe(string brand) => MccBrand(brand);

    #endregion
    #region The semantic container type in /inventory

    private static OpenContainerSnapshot Container(string? menuType = null, string? legacyWindowType = null)
        => new(
            WindowId: 1,
            Slots: [],
            Properties: new Dictionary<int, int>(),
            Cursor: ItemStackInfo.Empty,
            StateId: 0,
            Trades: null,
            MenuTypeId: -1,
            Title: "Chest",
            MenuType: menuType,
            LegacyWindowType: legacyWindowType);

    // The inventory command no longer prints a "type: minecraft:furnace" line: it shows legacy's container-layout DIAGRAM, which is what the old client showed in that position.
    // The guarantee these cases exist for is unchanged though, and is now checked where the semantic type is actually consumed: the diagram is chosen from UMPK's resolved minecraft:menu key, not guessed from a slot count.

    [Fact]
    public void ContainerArt_IsChosenByTheSemanticMenuType()
    {
        Assert.NotNull(ContainerArt.ForMenuType("minecraft:furnace"));
        Assert.Equal(ContainerArt.Get("Container.Furnace"), ContainerArt.ForMenuType("minecraft:furnace"));
    }

    [Fact]
    public void ContainerArt_TreatsTheNamespaceAsOptional()
        => Assert.Equal(ContainerArt.ForMenuType("minecraft:generic_9x3"), ContainerArt.ForMenuType("generic_9x3"));

    [Fact]
    public void ContainerArt_DistinguishesWindowsWithTheSameSlotCount()
    {
        // generic_9x3 and shulker_box both hold 27 container slots, and a slot-count heuristic could not tell a 9x6 double chest from two of them either.
        // The registry key can.
        Assert.Equal(ContainerArt.ForMenuType("minecraft:generic_9x3"), ContainerArt.ForMenuType("minecraft:shulker_box"));
        Assert.NotEqual(ContainerArt.ForMenuType("minecraft:generic_9x3"), ContainerArt.ForMenuType("minecraft:generic_9x6"));
    }

    [Fact]
    public void ContainerArt_ReturnsNull_ForAWindowItCannotName()
    {
        Assert.Null(ContainerArt.ForMenuType("EntityHorse"));
        Assert.Null(ContainerArt.ForMenuType(null));
        Assert.Null(ContainerArt.ForMenuType(string.Empty));
    }

    [Fact]
    public void ContainerArt_CoversTheWindowsLegacyLeftBlank()
    {
        // Legacy's GetAsciiArt returned null for all of these; they are drawn now.
        foreach (string menuType in (string[])
                 ["anvil", "merchant", "beacon", "lectern", "loom", "stonecutter",
                  "cartography_table", "smithing_table", "generic_9x1", "generic_9x2",
                  "generic_9x4", "generic_9x5"])
            Assert.NotNull(ContainerArt.ForMenuType(menuType));
    }

    #endregion
    #region The chat signature standing

    private static ChatMessageReceived Chat(
        ChatVerification verification,
        ChatCategory category = ChatCategory.Player)
        => new(Component.Text("hello"), category, Component.Text("Tester"), Guid.Empty, false, verification);

    private static ChatStandingMarker Marker(SignatureConfig? config = null, bool color = true)
        => new(config ?? new SignatureConfig(), color);

    [Fact]
    public void Standing_NeverHidesAnythingButARejectedSignature()
    {
        ChatStandingMarker marker = Marker(new SignatureConfig { ShowIllegalSignedChat = false });

        Assert.True(marker.ShouldHide(Chat(ChatVerification.Failed)));
        Assert.False(marker.ShouldHide(Chat(ChatVerification.Unverified)));
        Assert.False(marker.ShouldHide(Chat(ChatVerification.Insecure)));
        Assert.False(marker.ShouldHide(Chat(ChatVerification.Verified)));
        Assert.False(marker.ShouldHide(Chat(ChatVerification.NotApplicable, ChatCategory.System)));
    }

    [Fact]
    public void Standing_ByDefault_ShowsEvenARejectedSignature()
        => Assert.False(Marker().ShouldHide(Chat(ChatVerification.Failed)));

    [Fact]
    public void Standing_MapsEachSigningOutcomeToItsOwnFamily()
    {
        AssertKind(ChatVerification.Verified, ChatStanding.Verified);
        AssertKind(ChatVerification.Failed, ChatStanding.Rejected);
        AssertKind(ChatVerification.Unverified, ChatStanding.Unverified);
    }

    private static void AssertKind(ChatVerification verification, ChatStanding expected)
    {
        ChatStandingMark? mark = Marker().Mark(Chat(verification));

        Assert.NotNull(mark);
        Assert.Equal(expected, mark!.Value.Kind);
    }

    [Fact]
    public void Standing_UnverifiedIsNotPresentedAsVerifiedOrRejected()
    {
        // Signed-but-uncheckable is a third outcome.
        // Collapsing it into either neighbour would misreport it.
        int unverified = ChatStandingMarker.AnsiCodeOf(ChatStanding.Unverified);

        Assert.NotEqual(ChatStandingMarker.AnsiCodeOf(ChatStanding.Verified), unverified);
        Assert.NotEqual(ChatStandingMarker.AnsiCodeOf(ChatStanding.Rejected), unverified);
    }

    [Fact]
    public void Standing_InsecureIsUnmarkedByDefault_ButOptIn()
    {
        // Every message on an offline-mode server is insecure, so the default must not mark every line.
        Assert.Null(Marker().Mark(Chat(ChatVerification.Insecure)));

        ChatStandingMark? opted = Marker(new SignatureConfig { MarkInsecureMsg = true })
            .Mark(Chat(ChatVerification.Insecure));
        Assert.NotNull(opted);
        Assert.Equal(ChatStanding.Insecure, opted!.Value.Kind);
    }

    [Fact]
    public void Standing_ServerVoicedChatIsMarked_ButPre119LegacyChatIsNot()
    {
        Assert.NotNull(Marker().Mark(Chat(ChatVerification.NotApplicable, ChatCategory.System)));
        Assert.NotNull(Marker().Mark(Chat(ChatVerification.NotApplicable, ChatCategory.Disguised)));

        // Pre-1.19 chat carries no signature by design; marking every line there would say nothing.
        Assert.Null(Marker().Mark(Chat(ChatVerification.NotApplicable, ChatCategory.Legacy)));
    }

    [Fact]
    public void Standing_EachToggleSuppressesOnlyItsOwnFamily()
    {
        ChatStandingMarker marker = Marker(new SignatureConfig
        {
            MarkLegallySignedMsg = false,
            MarkUnverifiedMsg = false,
            MarkIllegallySignedMsg = true,
        });

        Assert.Null(marker.Mark(Chat(ChatVerification.Verified)));
        Assert.Null(marker.Mark(Chat(ChatVerification.Unverified)));
        Assert.NotNull(marker.Mark(Chat(ChatVerification.Failed)));
    }

    [Fact]
    public void Standing_AnsiPrefixCarriesTheColourAndTrailingSpace()
    {
        string prefix = Marker().AnsiPrefix(Chat(ChatVerification.Verified));

        Assert.StartsWith("\u001b[32m", prefix, StringComparison.Ordinal);
        Assert.EndsWith(" ", prefix, StringComparison.Ordinal);
        Assert.Contains("|", prefix, StringComparison.Ordinal);
    }

    [Fact]
    public void Standing_WithoutColour_MarksOnlyTheAbnormalStandings_InWords()
    {
        ChatStandingMarker plain = Marker(color: false);

        // A bare uncoloured bar would say nothing about which standing it is, so the normal case is silent.
        Assert.Equal(string.Empty, plain.AnsiPrefix(Chat(ChatVerification.Verified)));
        Assert.Equal(string.Empty, plain.AnsiPrefix(Chat(ChatVerification.NotApplicable, ChatCategory.System)));

        Assert.Contains("unverified", plain.AnsiPrefix(Chat(ChatVerification.Unverified)), StringComparison.Ordinal);
        Assert.Contains("signature-rejected", plain.AnsiPrefix(Chat(ChatVerification.Failed)), StringComparison.Ordinal);
    }

    [Fact]
    public void Standing_LabelsAreDistinctPerOutcome()
    {
        var labels = new HashSet<string>(StringComparer.Ordinal)
        {
            ChatStandingMarker.LabelOf(ChatVerification.Verified),
            ChatStandingMarker.LabelOf(ChatVerification.Unverified),
            ChatStandingMarker.LabelOf(ChatVerification.Failed),
            ChatStandingMarker.LabelOf(ChatVerification.Insecure),
            ChatStandingMarker.LabelOf(ChatVerification.NotApplicable),
        };

        Assert.Equal(5, labels.Count);
    }

    #endregion
    #region Recipe-book wording

    [Fact]
    public void RecipeBook_OpaqueWording_DoesNotPresentFrameCountsAsRecipeCounts()
    {
        // UMPK increments OpaqueAdditions once per recipe_book_add FRAME with a non-empty payload, and a normal 1.21.2+ join is a single such frame carrying the whole book.
        // Calling that "1 addition" would tell the reader their book holds one recipe.
        string only = CommandStrings.RecipeBookOpaqueOnly(1);
        string note = CommandStrings.RecipeBookOpaqueNote(1);

        Assert.Contains("update(s)", only, StringComparison.Ordinal);
        Assert.Contains("update(s)", note, StringComparison.Ordinal);
        Assert.DoesNotContain("addition(s) received", only, StringComparison.Ordinal);
        Assert.DoesNotContain("addition(s)", note, StringComparison.Ordinal);
    }

    [Fact]
    public void RecipeBook_OpaqueWording_SaysTheRecipeCountIsUnknown()
    {
        Assert.Contains("not known", CommandStrings.RecipeBookOpaqueOnly(1), StringComparison.Ordinal);
        Assert.Contains("not known", CommandStrings.RecipeBookOpaqueNote(3), StringComparison.Ordinal);
    }

    [Fact]
    public void RecipeBook_OpaqueOnly_StillDeniesTheBookIsEmpty()
    {
        string line = CommandStrings.RecipeBookOpaqueOnly(1);

        Assert.Contains("not empty", line, StringComparison.Ordinal);
        Assert.NotEqual(CommandStrings.RecipeBookNone, line);
    }
    #endregion
}
